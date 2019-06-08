﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DateDetectorV2.Dater
{
    public class Leet
    {
        public static Dictionary<string, HashSet<string>> GetLengthMonthsDictionary(string path)
        {
            Dictionary<string, HashSet<string>> leetDictionary = new Dictionary<string, HashSet<string>>();
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex kRegex = new Regex(".=");
                    Match kMatch = kRegex.Match(line);
                    string key = kMatch.Value.TrimEnd('=');
                    Regex valueRegex = new Regex("=.*");
                    Match valueMatch = valueRegex.Match(line);
                    string[] values = valueMatch.Value.TrimStart('=').Split(',');
                    if (leetDictionary.ContainsKey(key))
                    {
                        HashSet<string> leetValues;
                        leetDictionary.TryGetValue(key, out leetValues);
                        foreach(var value in values)
                        {
                            leetValues.Add(value);
                        }
                    }
                    else
                    {
                        leetDictionary.Add(key, new HashSet<string>(values));
                    }
                }
            }
            return leetDictionary;
        }
    }
}
