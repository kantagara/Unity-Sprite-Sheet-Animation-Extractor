using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityLab
{
    /// <summary>
    /// Small class that helps to compare sprites by the last number that sprite sheet extractor assigned to them
    /// </summary>
    public class ExtractedSpriteNumberComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return ExtractNumber(x).CompareTo(ExtractNumber(y));
        }

        private int ExtractNumber(string input)
        {
            //Get all the numbers that come after the underscore
            Match match = Regex.Match(input, @"_(\d+)(?!.*_\d+)");

            //But just compare the ones tat come after the last underscore,
            //since those are telling us the position in the array.
            if (match.Success)
                return int.Parse(match.Groups[^1].Value);
            return -1;
        }
    }
}