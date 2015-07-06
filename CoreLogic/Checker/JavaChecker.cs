using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CoreLogic.Checker
{
    class JavaChecker : ListChecker
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        private static string[] SplitArgs(string commandLine)
        {
            int argc;
            IntPtr argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try
            {
                string[] args = new string[argc];
                for (int i = 0; i < args.Length; i++)
                {
                    IntPtr p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        private static string NormalizeFile(string file)
        {
            return file.Split(new char[] { '\\', '/' }).Last<string>();
        }

        public JavaChecker(ICollection<string> set) : base(set) { }

        public JavaChecker(string filename) : base(filename) { }

        public override bool IsGame(Process process)
        {
            if(process.ProcessName != "java" && process.ProcessName != "javaw")
            {
                return false;
            }

            string wmiQuery = string.Format("SELECT CommandLine FROM Win32_Process WHERE ProcessId = '{0}'", process.Id);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection retObjectCollection = searcher.Get();
            ManagementObject retObject = (ManagementObject)retObjectCollection.OfType<ManagementObject>().FirstOrDefault<ManagementObject>();
            if(retObject == null)
            {
                return false;
            }
            string[] commandLine = SplitArgs((string)retObject["CommandLine"]);
            if (commandLine == null)
            {
                return false;
            }

            string jarName = null;
            string[] classPath = null;
            string mainClass = null;
            for(int i = 1; i < commandLine.Length; i++)
            {
                string str = commandLine[i];
                if(str == "-jar")
                {
                    jarName = NormalizeFile(commandLine[++i]);
                    mainClass = null;
                }
                else if(str == "-cp")
                {
                    classPath = commandLine[++i].Split(new char[] { ':', ';' });
                }
                else if(str[0] != '-')
                {
                    mainClass = commandLine[i];
                    break;
                }
            }

            if((jarName != null && set.Contains("JAR=" + jarName)) ||
               (mainClass != null && set.Contains("MAIN=" + mainClass)))
            {
                return true;
            }

            if(classPath != null)
            {
                foreach(string cPathEntry in classPath)
                {
                    if (set.Contains("CPATH=" + NormalizeFile(cPathEntry)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
