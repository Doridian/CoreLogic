using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace CoreLogic
{
    public class SimpleProcessFinder : IProcessFinder
    {
        private Thread finderThread;
        private bool running;

        private HashSet<int> runningProcesses = new HashSet<int>();

        public SimpleProcessFinder()
        {
            running = true;
            finderThread = new Thread(new ThreadStart(ProcessStart_Thread));
            finderThread.Start();
        }

        public override void FindAll()
        {
            lock(runningProcesses)
            {
                HashSet<int> noLongerRunningProcesses = new HashSet<int>(runningProcesses);

                foreach(Process p in Process.GetProcesses())
                {
                    if(runningProcesses.Add(p.Id))
                    {
                        RunProcessStarted(p);
                    }
                    noLongerRunningProcesses.Remove(p.Id);
                }

                foreach (int pid in noLongerRunningProcesses)
                {
                    runningProcesses.Remove(pid);
                    RunProcessStopped(pid);
                }
            }
        }

        ~SimpleProcessFinder()
        {
            Dispose();
        }

        public override void Dispose()
        {
            running = false;
        }

        private void ProcessStart_Thread()
        {
            while(running)
            {
                FindAll();
                Thread.Sleep(5000);
            }
        }
    }
}
