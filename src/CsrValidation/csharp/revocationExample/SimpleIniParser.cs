// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portionas of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;

namespace RevocationExample
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