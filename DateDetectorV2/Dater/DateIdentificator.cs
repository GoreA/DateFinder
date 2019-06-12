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
        public const string DIGITS = "0123456789";
        public string LETTERS = string.Empty;
        private string _outputFormat;

        public DateIdentificator (string pathForMonths, string outputFormat) 
        {
            if (string.IsNullOrEmpty(pathForMonths))
            {
                throw new System.ArgumentException("PathForMonths parameter cannot be null or empty", pathForMonths);
            }
            else
                LETTERS = GetLettersFromDeclaredMonths(pathForMonths);

            if (string.IsNullOrEmpty(outputFormat))
            {
                throw new System.ArgumentException("OutputFormat parameter cannot be null or empty");
            }
            else
                _outputFormat = outputFormat;
        }

        private string GetLettersFromDeclaredMonths(string path)
        {
            HashSet<char> chars = new HashSet<char>();
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    chars.UnionWith(line.ToCharArray());
                }
            }
            return string.Join("", chars);
        }

        internal (string, float) GetSupposedValue(List<string> valueToDetec)
        {
            if (string.IsNullOrEmpty(valueToDetec[2]))
            {
                string CurrentYear = DateTime.Now.Year.ToString();
                valueToDetec[2] = CurrentYear;
            }

            string currentDate = string.Join(" ", valueToDetec);
            if (!currentDate.Contains("_")) {
                return (PrepareDate(currentDate), 1f);
            }

            float accuracy = 0;
            valueToDetec = valueToDetec.Select(val => val.Replace(',', 'z')).ToList();

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
                            possibleLetters = CalculatePossibleLetters(possibleLetters, month, i);
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

        private (string currentDate, float accuracy) GetSupposedValueWithoutLeet(List<string> valueToDetec)
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
                        possibleLetters = CalculatePossibleLetters(possibleLetters, month, i);
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
            return (DateTime.Now.ToString(_outputFormat, CultureInfo.InvariantCulture), 0f);
        }

        private List<ISet<string>> CalculatePossibleLetters(List<ISet<string>> possibleLetters, string month, int i)
        {
            string monthWithoutDash = month.Replace("_", "");
            if (i == 1 && monthWithoutDash.Count() > 2) 
                // in this case we have a month representation in alphabetic format so in this case we do not need to includ digits too
                possibleLetters.Add(new HashSet<string>(LETTERS.Select(x => x.ToString()).ToList()));
            else
                possibleLetters.Add(new HashSet<string>(DIGITS.Select(x => x.ToString()).ToList()));
            return possibleLetters;
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

        private string PrepareDate(string currentDate) 
        {
            DateTime result = DateTime.Now;
            if (currentDate.Split(' ')[1].Count() >= 3)
            {
                if (currentDate.Split(' ')[2].Count() != 2)
                {
                    result = GetDateLongMonthLongYear(currentDate);
                }
                else
                {
                    result = GetDateLongMonthShortYear(currentDate);
                }
            }
            else
            {
                if (currentDate.Split(' ')[2].Count() != 2)
                {
                    result = GetDateShortMonthLongYear(currentDate);
                }
                else
                {
                    result = GetDateShortMothShortYear(currentDate);
                }
            }
            return result.ToString(_outputFormat, CultureInfo.InvariantCulture);
        }

        private static DateTime GetDateLongMonthLongYear(string currentDate)
        {
            DateTime result;
            if (!DateTime.TryParseExact(currentDate, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                if (DateTime.TryParseExact(currentDate, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    if (DateTime.TryParseExact(currentDate, "d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                        DateTime.TryParseExact(currentDate, "dd MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            return result;
        }

        private static DateTime GetDateLongMonthShortYear(string currentDate)
        {
            DateTime result;
            if (!DateTime.TryParseExact(currentDate, "d MMM yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                if (DateTime.TryParseExact(currentDate, "dd MMM yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    if (DateTime.TryParseExact(currentDate, "d MMMM yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                        DateTime.TryParseExact(currentDate, "dd MMMM yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            return result;
        }

        private static DateTime GetDateShortMonthLongYear(string currentDate)
        {
            DateTime result;
            if (DateTime.TryParseExact(currentDate, "d MM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                DateTime.TryParseExact(currentDate, "dd MM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            return result;
        }

        private static DateTime GetDateShortMothShortYear(string currentDate)
        {
            DateTime result;
            if (DateTime.TryParseExact(currentDate, "d MM yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                DateTime.TryParseExact(currentDate, "dd MM yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
            return result;
        }
    }
}
