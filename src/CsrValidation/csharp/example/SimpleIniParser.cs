using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Example
{
    public class SimpleIniParser
    {
        /// <summary>
        /// Initialize an INI file
        /// Load it if it exists
        /// </summary>
        /// <param name="file">Full path where the INI file has to be read from or written to</param>
        public static Dictionary<string, string> Parse(string file)
        {
            if (!File.Exists(file))
                return null;

            var txt = File.ReadAllText(file);

            Dictionary<string, string> properties = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var l in txt.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = l.Trim();

                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var idx = line.IndexOf("=");
                if (idx != -1)
                {
                    properties[line.Substring(0, idx)] = line.Substring(idx + 1);
                }
            }

            return properties;
        }
    }
}