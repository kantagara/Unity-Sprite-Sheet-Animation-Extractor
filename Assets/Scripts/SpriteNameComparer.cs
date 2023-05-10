using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DefaultNamespace
{
    public class SpriteNameComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return ExtractNumber(x).CompareTo(ExtractNumber(y));
        }

        private int ExtractNumber(string input)
        {
            Match match = Regex.Match(input, @"_(\d+)(?!.*_\d+)");

            if (match.Success)
                return int.Parse(match.Groups[1].Value);
            return -1;
        }
    }
}