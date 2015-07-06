using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoreLogic
{
    class ProcessHandler : IDisposable
    {
        private Dictionary<int, Process> processCache = new Dictionary<int, Process>();
        private HashSet<int> gameModeProcesses = new HashSet<int>();
        private bool gameModeOn = false;
        private bool running;
        IProcessFinder finder;

        private bool initialSweep = false;

        private List<IGameChecker> gameCheckers;

        public ProcessHandler(IEnumerable<IGameChecker> gameCheckers)
        {
            running = true;

            this.gameCheckers = new List<IGameChecker>(gameCheckers);

            finder = new SimpleProcessFinder();
            finder.ProcessStarted += finder_ProcessStarted;
            finder.ProcessStopped += finder_ProcessStopped;
            Console.WriteLine("EVT | Initial sweep");
            initialSweep = true;
            finder.FindAll();
            initialSweep = false;
            Console.WriteLine("EVT | Initial sweep complete");
            Console.WriteLine("EVT | Initial affinity assignment");
            AssignGameMode();
            Console.WriteLine("EVT | Initial affinity assignment complete");
        }

        public void Dispose()
        {
            running = false;
            finder.Dispose();
            foreach (Process proc in Process.GetProcesses())
            {
                try
                {
                    proc.ProcessorAffinity = Program.PROCESSOR_ALL;
                }
                catch { }
            }
        }

        private string PrettyProcessName(Process proc)
        {
            return PrettyProcessName(proc, -1);
        }

        private string PrettyProcessName(Process proc, long fallbackID)
        {
            if(proc == null)
            {
                return "N/A (" + fallbackID + ")";
            }

            return proc.ProcessName + " (" + proc.Id + ")";
        }

        ~ProcessHandler()
        {
            Dispose();
        }

        private void AssignGameMode(Process proc)
        {
            if (initialSweep || !running)
            {
                return;
            }

            lock (gameModeProcesses)
            {
                string aName;
                IntPtr aValue;
                if (gameModeOn)
                {
                    if(gameModeProcesses.Contains(proc.Id)) //IsGame
                    {
                        aName = "Game";
                        aValue = Program.PROCESSOR_GAME;
                    }
                    else
                    {
                        aName = "OS  ";
                        aValue = Program.PROCESSOR_OS;
                    }
                }
                else
                {
                    aName = "All  ";
                    aValue = Program.PROCESSOR_ALL;
                }

                try
                {
                    proc.ProcessorAffinity = aValue;
                    Console.Out.WriteLine("OK  | " + aName + " | " + PrettyProcessName(proc));
                }
                catch
                {
                    Console.Out.WriteLine("ERR | " + aName + " | " + PrettyProcessName(proc));
                }
            }
        }

        private void AssignGameMode()
        {
            foreach(Process p in Process.GetProcesses())
            {
                AssignGameMode(p);
            }
        }

        private void RefreshGameMode(Process related)
        {
            lock (gameModeProcesses)
            {
                if (gameModeProcesses.Count < 1)
                {
                    if (gameModeOn)
                    {
                        gameModeOn = false;
                        AssignGameMode();
                    }
                    else
                    {
                        if (related != null)
                        {
                            AssignGameMode(related);
                        }
                    }
                    return;
                }

                if (gameModeOn)
                {
                    if (related != null)
                    {
                        AssignGameMode(related);
                    }
                }
                else
                {
                    gameModeOn = true;
                    AssignGameMode();
                }
            }
        }

        private Process GetProcess(int pid)
        {
            Process p;
            lock (processCache)
            {
                p = processCache[pid];
                if (p == null)
                {
                    p = Process.GetProcessById(pid);
                    processCache[pid] = p;
                }
            }
            return p;
        }

        void finder_ProcessStopped(int pid)
        {
            Console.WriteLine("EVT | Stop detected " + PrettyProcessName(processCache[pid], pid));

            lock (processCache)
            {
                processCache.Remove(pid);
            }

            lock(gameModeProcesses)
            {
                if(gameModeProcesses.Remove(pid))
                {
                    RefreshGameMode(null);
                }
            }
        }

        void finder_ProcessStarted(Process process)
        {
            if (!initialSweep)
            {
                Console.WriteLine("EVT | Start detected " + PrettyProcessName(process));
            }

            lock (processCache)
            {
                processCache[process.Id] = process;
            }

            if (IsGame(process))
            {
                lock (gameModeProcesses)
                {
                    gameModeProcesses.Add(process.Id);
                }
            }

            RefreshGameMode(process);
        }

        private bool IsGame(Process p)
        {
            foreach(IGameChecker checker in gameCheckers)
            {
                if(checker.IsGame(p))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
