using System.Collections.Generic;
using System.Diagnostics;

namespace CoreLogic.Checker
{
    class ProcessNameChecker : ListChecker
    {
        public ProcessNameChecker(ICollection<string> set) : base(set) { }

        public ProcessNameChecker(string filename) : base(filename) { }

        public override bool IsGame(Process process)
        {
            return set.Contains(process.ProcessName);
        }
    }
}
