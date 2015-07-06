using System;
using System.Collections.Generic;

namespace CoreLogic
{
    class Program
    {
        public static IntPtr PROCESSOR_ALL;
        public static IntPtr PROCESSOR_GAME;
        public static IntPtr PROCESSOR_OS = (IntPtr)0x0001;

        static string MakeMask(IntPtr intPtr, int len)
        {
            return Convert.ToString((long)intPtr, 2).PadLeft(len, '0');
        }

        static void Main(string[] args)
        {
            int coreCount = 0;
            int realCoreCount = 0;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfLogicalProcessors"].ToString());
                realCoreCount += int.Parse(item["NumberOfCores"].ToString());
            }

            long processorAll = 1;
            for (int i = 1; i < coreCount; i++)
            {
                processorAll = (processorAll << 1) + 1;
            }

            long processorGame = 0;
            for (int j = 0; j < (coreCount / realCoreCount); j++)
            {
                for (int i = 1; i < realCoreCount; i++)
                {
                    processorGame = (processorGame << 1) + 1;
                }
                processorGame = processorGame << 1;
            }

            PROCESSOR_GAME = (IntPtr)processorGame;
            PROCESSOR_OS = (IntPtr)(processorAll & ~processorGame);
            PROCESSOR_ALL = (IntPtr)processorAll;

            Console.Out.WriteLine("GAME processor mask: " + MakeMask(PROCESSOR_GAME, coreCount));
            Console.Out.WriteLine("OS   processor mask: " + MakeMask(PROCESSOR_OS, coreCount));
            Console.Out.WriteLine("ALL  processor mask: " + MakeMask(PROCESSOR_ALL, coreCount));

            Console.Out.WriteLine("-------------");
            Console.Out.WriteLine(" INITIALIZED ");
            Console.Out.WriteLine("-------------");

            List<IGameChecker> checkers = new List<IGameChecker>();
            checkers.Add(new Checker.JavaChecker("javagames.txt"));
            checkers.Add(new Checker.ProcessNameChecker("nativegames.txt"));
            ProcessHandler ph = new ProcessHandler(checkers);
        }
    }
}
