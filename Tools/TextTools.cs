using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PluginLogReader.Tools
{
    public static class TextTools
    {
        /// <summary>
        /// Replace Xchat TimeStamp
        /// </summary>
        public static string XchatTimeStamp(string s)
        {
            return Regex.Replace(s, @"^T (\d+)", delegate(Match match)
            {
                var v = match.Groups[1].Value;
                // Calculate elapsed time since epoch
                return new DateTime(1970, 1, 1).AddSeconds(Convert.ToDouble(v)).ToString();
            });
        }
    }
}
