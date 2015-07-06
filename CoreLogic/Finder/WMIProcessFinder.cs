using System.Diagnostics;
using System.Management;

namespace CoreLogic
{
    public class WMIProcessFinder : IProcessFinder
    {
        private ManagementEventWatcher processStartWatcher;
        private ManagementEventWatcher processStopWatcher;

        public WMIProcessFinder()
        {
            processStartWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            processStartWatcher.EventArrived += ProcessStart_EventArrived;
            processStartWatcher.Start();

            processStopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
            processStopWatcher.EventArrived += ProcessStop_EventArrived;
            processStopWatcher.Start();
        }

        ~WMIProcessFinder()
        {
            Dispose();
        }

        public override void Dispose()
        {
            processStartWatcher.Stop();
            processStopWatcher.Stop();
        }

        private void ProcessStart_EventArrived(object sender, EventArrivedEventArgs e)
        {
            RunProcessStarted(Process.GetProcessById((int)e.NewEvent.Properties["ProcessID"].Value));
        }

        private void ProcessStop_EventArrived(object sender, EventArrivedEventArgs e)
        {
            RunProcessStopped((int)e.NewEvent.Properties["ProcessID"].Value);
        }
    }
}
