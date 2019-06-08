using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace DateDetectorV2.Dater
{
    public class DateIdentificator
    {
        public const string DIGITS_LETTERS = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        internal static (string, float) GetSupposedValue(List<string> valueToDetec)
        {
            if (string.IsNullOrEmpty(valueToDetec[2]))
            {
                string CurrentYear = DateTime.Now.Year.ToString();
                valueToDetec[2] = CurrentYear;
            }

            string currentDate = string.Join(" ", valueToDetec);
            if (!currentDate.Contains("_")) {
                return (currentDate, 1f);
            }

            float accuracy = 0;
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pathToLeet = Path.Combine(currentDirectory, @"Resources/Leet.txt");
            Dictionary<string, HashSet<string>> leetDict = Leet.GetLengthMonthsDictionary(pathToLeet);

            List<string> months = valueToDetec[1].Split(',').ToList();
            foreach (string month in months)
            {
                valueToDetec[1] = month;
                string reparedDate = string.Join(" ", valueToDetec);
                int leetUnderscores = reparedDate.Count(x => x == '_');
                int simpleUnderscores = 0;
                List<ISet<string>> possibleLetters = new List<ISet<string>>();
                for (int i = 0; i < valueToDetec.Count; i++)
                {
                    Regex regex = new Regex(@"._");
                    MatchCollection matchCollection = regex.Matches(valueToDetec[i]);
                    foreach (Match match in matchCollection)
                    {
                        string letter = match.Value.TrimEnd('_');
                        var exist = leetDict.TryGetValue(letter, out HashSet<string> dictLetters);
                        if (exist)
                        {
                            possibleLetters.Add(dictLetters);
                        }
                        else
                        {
                            possibleLetters.Add(new HashSet<string>(DIGITS_LETTERS.Select(x => x.ToString()).ToList()));
                            leetUnderscores--;
                            simpleUnderscores++;
                        }
                    }
                }

                List<string> combos = GetAllCombinations(possibleLetters);
                Regex repareRegex = new Regex(@"._");
                string[] matchesPerReparedDate = repareRegex.Matches(reparedDate).Cast<Match>()
                    .Select(m => m.Value).ToArray();
                foreach (string combo in combos)
                {
                    currentDate = string.Join(" ", valueToDetec);
                    foreach (var letterMatch in combo.ToCharArray().Zip(matchesPerReparedDate, Tuple.Create))
                    {
                        currentDate = ReplaceFirstOccurrence(currentDate, letterMatch.Item2, letterMatch.Item1.ToString());
                    }
                    if (DateIsValis(currentDate))
                    {
                        accuracy = CalculateAccuracy(currentDate, leetUnderscores, simpleUnderscores);
                        return (PrepareDate(currentDate), accuracy);
                    }
                }
            }

            (currentDate, accuracy) = GetSupposedValueWithoutLeet(valueToDetec);
            return (currentDate, accuracy);
        }

        private static (string currentDate, float accuracy) GetSupposedValueWithoutLeet(List<string> valueToDetec)
        {
            float accuracy = 0;
            List<string> months = valueToDetec[1].Split(',').ToList();
            foreach (string month in months)
            {
                valueToDetec[1] = month;
                string reparedDate = string.Join(" ", valueToDetec);
                int simpleUnderscores = reparedDate.Count(x => x == '_');
                List<ISet<string>> possibleLetters = new List<ISet<string>>();
                for (int i = 0; i < valueToDetec.Count; i++)
                {
                    Regex regex = new Regex(@"._");
                    MatchCollection matchCollection = regex.Matches(valueToDetec[i]);
                    foreach (Match match in matchCollection)
                    {
                        possibleLetters.Add(new HashSet<string>(DIGITS_LETTERS.Select(x => x.ToString()).ToList()));
                    }
                }

                List<string> combos = GetAllCombinations(possibleLetters);
                Regex repareRegex = new Regex(@"._");
                string[] matchesPerReparedDate = repareRegex.Matches(reparedDate).Cast<Match>()
                    .Select(m => m.Value).ToArray();

                foreach (string combo in combos)
                {
                    string currentDate = string.Join(" ", valueToDetec);
                    foreach (var letterMatch in combo.ToCharArray().Zip(matchesPerReparedDate, Tuple.Create))
                    {
                        currentDate = ReplaceFirstOccurrence(currentDate, letterMatch.Item2, letterMatch.Item1.ToString());
                    }
                    if (DateIsValis(currentDate))
                    {
                        accuracy = CalculateAccuracy(currentDate, 0, simpleUnderscores);
                        return (PrepareDate(currentDate), accuracy);
                    }
                }
            }
            return (DateTime.Now.ToString("dd MM yyyy"), 0f);
        }

        private static List<string> GetAllCombinations(List<ISet<string>> possibleLetters, List<string> combos = null)
        {
            if(combos == null) { 
                if(possibleLetters == null || possibleLetters.Count == 0) {
                    throw new ArgumentException("There are no combinations identified. You didn't provide any wrongly detected letters.");
                }
                else 
                {
                    combos = new List<string>(); 
                    combos = combos.Concat(possibleLetters[0]).ToList();
                    possibleLetters.Remove(possibleLetters[0]);
                }
            }

            ISet<string> letters = possibleLetters.Count != 0 ? possibleLetters[0] : null;
            if (letters == null)
                return combos;
            else 
            {
                List<string> newCombos = new List<string>();
                foreach(string combo in combos) 
                { 
                    foreach(string let in possibleLetters[0]) 
                    {
                        newCombos.Add(combo + let);
                    }
                }
                possibleLetters.Remove(possibleLetters[0]);
                return GetAllCombinations(possibleLetters, newCombos);
            }
        }

        public static string ReplaceFirstOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.IndexOf(Find);
            string result = Source.Remove(Place, Find.Length).Insert(Place, Replace);
            return result;
        }

        private static float CalculateAccuracy(string currentDate, int leetUnderscores, int simpleUnderscores)
        { 
            return (float)(currentDate.Count() - 2 - leetUnderscores * 0.1 - simpleUnderscores * 0.95) / (currentDate.Count() - 2);
        }

        private static bool DateIsValis(string date) 
        {
            return DateHelper.ValidateDate(date, "dd MM yy") || 
                DateHelper.ValidateDate(date, "dd MM yyyy") || 
                DateHelper.ValidateLongDate(date);
        }

        private static string PrepareDate(string currentDate) 
        {
            if (currentDate.Split(' ')[1].Count() == 3)
            {
                string[] detectedDate = currentDate.Split(' ');
                detectedDate[1] = currentDate.Split(' ')[1].ToUpper();
                currentDate = string.Join(" ", detectedDate);
            }
            else if (currentDate.Split(' ')[1].Count() > 3)
            {
                string[] detectedDate = currentDate.Split(' ');
                detectedDate[1] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(currentDate.Split(' ')[1].ToLower());
                currentDate = string.Join(" ", detectedDate);
            }
            return currentDate;
        }
    }
}
