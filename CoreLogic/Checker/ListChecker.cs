using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace CoreLogic.Checker
{
    abstract class ListChecker : IGameChecker
    {
        protected HashSet<string> set;

        public ListChecker(ICollection<string> set)
        {
            this.set = new HashSet<string>(set);
        }

        public ListChecker(string filename)
            : this(File.ReadAllLines(filename))
        {
            
        }

        public abstract bool IsGame(Process process);
    }
}
