using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace DateDetectorV2.Dater
{
    public class DateFinder
    {
        private string _stringToScan;
        private string _pathToFormats;
        private int _distance;
        private Dictionary<int, HashSet<string>> _monthsDictionary;

        public DateFinder(string stringToScan, string pathForFormats, string pathForMonths, int distance)
        {
            this._stringToScan = stringToScan;
            this._pathToFormats = pathForFormats;
            this._distance = distance;
            this._monthsDictionary = DateHelper.GetLengthMonthsDictionary(pathForMonths);
        }

        /// <summary>
        /// Scans given file to find any date representation that corresponds to
        /// given formats.
        /// </summary>
        /// <returns>a JSON string that represents DateResult objects.</returns>
        public string DetectDate()
        {
            List<string> formats = ScanFormats(_pathToFormats);
            Dictionary<int, HashSet<string>> lengthWithCorrespondingFormats = GetLengthFormatsDictionary(formats);
            Dictionary<string, HashSet<string>> wordRegexes = GetWordRegexes(lengthWithCorrespondingFormats);
            List<DateResult> dateResults = GetDateResultListFromWordRegEx(wordRegexes);
            return JsonConvert.SerializeObject(dateResults);
        }

        private List<DateResult> GetDateResultListFromWordRegEx(Dictionary<string, HashSet<string>> wordRegexes)
        {
            List<DateResult> dateResults = new List<DateResult>();
            foreach( KeyValuePair<string, HashSet<string>> wordRegexFormats in wordRegexes){
                dateResults.AddRange(GetDateResults(wordRegexFormats.Key, wordRegexFormats.Value));
            }
            return dateResults;
        }

        private IEnumerable<DateResult> GetDateResults(string stringRegEx, IEnumerable<string> formats)
        {
            List<DateResult> dateResults = new List<DateResult>();
            Regex regex = new Regex(@stringRegEx);
            MatchCollection matchCollection = regex.Matches(_stringToScan);
            foreach (Match match in matchCollection)
            {
                foreach(string format in formats) 
                {
                    DateResult dateResult = GetDateResultFromWordFormats(match, format);
                    if(dateResult != null) 
                    {
                        dateResults = ExtendDateResults(dateResults, dateResult); 
                    }
                }
            }
            return dateResults;
        }

        private DateResult GetDateResultFromWordFormats(Match match, string format)
        {
            int distance = GetDistance(match.Value, format);
            if (distance <= _distance)
            {
                return CreateDateResult(match, format, distance);
            }
            else return null;
        }

        private int GetDistance(string value, string format)
        {
            value = value.Substring(1, (value.Length - 2));
            int distance = 0;
            char[] valueArray = value.ToCharArray();
            char[] formatArray = format.ToCharArray();
            // filter out formats with alphabetic months representation
            if (format.ToLower().Contains("mmm"))
            {
                if (format.ToLower().Contains("d") && !format.ToLower().Contains("dd"))
                {
                    distance = GetDistanceFromFormatDMMM(value, format);
                }
                else {
                    distance = GetDistanceFromFormatDDMMM(value, format);
                }
            }
            // filter out formats with numeric months representation
            else
            {
                // formats with 1 d -> d.m.yy
                if (format.ToLower().Contains("d") && !format.ToLower().Contains("dd"))
                {
                    if(format.ToLower().Contains("m") && !format.ToLower().Contains("mm")) {
                        distance = GetDistanceFromFormatDM(value, format);
                    }
                    else {
                        distance = GetDistanceFromFormatDMM(value, format);
                    }
                }
                // formats with 2 d -> dd.mm.yy
                else
                {
                    if (format.ToLower().Contains("m") && !format.ToLower().Contains("mm")) {
                        distance = GetDistanceFromFormatDDM(value, format);
                    }
                    else {
                        distance = GetDistanceFromFormatDDMM(value, format);
                    }
                }
            }
            return distance;
        }

        private int GetDistanceFromFormatDDMM(string value, string format)
        {
            int distance = 0;
            char[] valueArray = value.ToCharArray();
            char[] formatArray = format.ToCharArray();

            List<char> monthArray = new List<char>();
            List<char> dayArray = new List<char>();
            List<char> yearArray = new List<char>();
            (distance, dayArray, monthArray, yearArray) = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray); 
            distance += DateHelper.GetDayDistance(dayArray);
            distance += DateHelper.GetMonthDistance(dayArray, monthArray);
            distance += DateHelper.GetYearDistance(dayArray, monthArray, yearArray);

            return distance;
        }

        private int GetDistanceFromFormatDDM(string value, string format)
        {
            int distance = 0;
            char[] valueArray = value.ToCharArray();
            char[] formatArray = format.ToCharArray();
            if (value.Length == format.Length + 1)
            {
                Regex mRegex = new Regex("[mM]");
                Match match = mRegex.Match(format);
                string newFormat = format.Insert(match.Index + 1, "m");
                distance = GetDistanceFromFormatDDMM(value, newFormat);
            }
            else
            {
                List<char> monthArray = new List<char>();
                List<char> dayArray = new List<char>();
                List<char> yearArray = new List<char>();
                (distance, dayArray, monthArray, yearArray) = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                distance += DateHelper.GetDayDistance(dayArray);
                distance += !DateHelper.DIGITS.Contains(monthArray[0].ToString()) || monthArray[0] == '0' ? 1 : 0;
                if (distance == 0)
                    distance += DateHelper.GetDayMonthDateValidationDistance(dayArray, monthArray);
                distance += DateHelper.GetYearDistance(dayArray, monthArray, yearArray);
            }

            return distance;
        }

        private int GetDistanceFromFormatDMM(string value, string format)
        {
            int distance = 0;
            char[] valueArray = value.ToCharArray();
            char[] formatArray = format.ToCharArray();
            if (value.Length == format.Length + 1)
            {
                Regex mRegex = new Regex("d");
                Match match = mRegex.Match(format);
                string newFormat = format.Insert(match.Index + 1, "d");
                distance = GetDistanceFromFormatDDMM(value, newFormat);
            }
            else
            {
                List<char> monthArray = new List<char>();
                List<char> dayArray = new List<char>();
                List<char> yearArray = new List<char>();
                (distance, dayArray, monthArray, yearArray) = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                distance += !DateHelper.DIGITS.Contains(dayArray[0].ToString()) || dayArray[0] == '0' ? 1 : 0;
                distance += DateHelper.GetMonthDistance(dayArray, monthArray);
                distance += DateHelper.GetYearDistance(dayArray, monthArray, yearArray);
            }
            return distance;
        }

        private int GetDistanceFromFormatDM(string value, string format)
        {
            int distance = 0;
            char[] valueArray = value.ToCharArray();
            char[] formatArray = format.ToCharArray();

            if (value.Length == format.Length + 2)
            {
                Regex mRegex = new Regex("d");
                Match match = mRegex.Match(format);
                string newFormat = format.Insert(match.Index + 1, "d");
                mRegex = new Regex("[mM]");
                match = mRegex.Match(format);
                newFormat = newFormat.Insert(match.Index + 1, "m");
                distance = GetDistanceFromFormatDDMM(value, newFormat);
            }
            else
            if (value.Length == format.Length + 1)
            {
                Regex mRegex = new Regex("d");
                Match match = mRegex.Match(format);
                string newFormat = format.Insert(match.Index + 1, "d");
                int distance1 = GetDistanceFromFormatDDM(value, newFormat);

                mRegex = new Regex("[mM]");
                match = mRegex.Match(format);
                newFormat = format.Insert(match.Index + 1, "m");
                int distance2 = GetDistanceFromFormatDMM(value, newFormat);

                distance = distance1 < distance2 ? distance1 : distance2;
            }
            else
            {
                List<char> monthArray = new List<char>();
                List<char> dayArray = new List<char>();
                List<char> yearArray = new List<char>();
                (distance, dayArray, monthArray, yearArray) = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                distance += !DateHelper.DIGITS.Contains(dayArray[0].ToString()) || dayArray[0] == '0' ? 1 : 0; 
                distance += !DateHelper.DIGITS.Contains(monthArray[0].ToString()) || monthArray[0] == '0' ? 1 : 0;
                if (distance == 0)
                    distance += DateHelper.GetDayMonthDateValidationDistance(dayArray, monthArray);
                distance += DateHelper.GetYearDistance(dayArray, monthArray, yearArray);
            }

            return distance;
        }

        private int GetDistanceFromFormatDDMMM(string value, string format)
        {
            int distance = 0;
            string newFormat = format;
            int monthLength = value.Length - format.Length + 3;
            if (_monthsDictionary.ContainsKey(monthLength))
            {
                Regex mRegex = new Regex("[mM]");
                Match match = mRegex.Match(format);
                int monthFormatPadding = monthLength - 3;
                for (int i = 0; i < monthFormatPadding; i++)
                {
                    newFormat = newFormat.Insert(match.Index + i + 1, "m");
                }

                char[] valueArray = value.ToCharArray();
                char[] formatArray = newFormat.ToCharArray();
                List<char> monthArray = new List<char>();
                List<char> dayArray = new List<char>();
                List<char> yearArray = new List<char>();
                (distance, dayArray, monthArray, yearArray) = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                distance += DateHelper.GetDayDistance(dayArray);

                HashSet<string> months = new HashSet<string>();
                _monthsDictionary.TryGetValue(monthLength, out months);
                int monthDistance = Int16.MaxValue;
                foreach (string month in months)
                {
                    int currentMonthDistance = 0;
                    char[] monthDictArray = month.ToCharArray();
                    for (int i = 0; i < monthDictArray.Length; i++)
                    {
                        if (monthDictArray[i] != monthArray[i])
                            currentMonthDistance++;
                    }
                    monthDistance = monthDistance < currentMonthDistance ? monthDistance : currentMonthDistance;
                }

                distance += monthDistance;
                if (distance == 0)
                    distance += DateHelper.GetDayLongMonthDateValidationDistance(dayArray, monthArray);
                distance += DateHelper.GetYearDistance(dayArray, monthArray, yearArray);
            }
            else distance = Int16.MaxValue;
            return distance;
        }

        private int GetDistanceFromFormatDMMM(string value, string format)
        {
            int distanceDMMM = 0;
            string newFormat = format;
            int monthLength = value.Length - format.Length + 3;
            if (_monthsDictionary.ContainsKey(monthLength))
            {
                Regex mRegex = new Regex("[mM]");
                Match match = mRegex.Match(format);
                int monthFormatPadding = monthLength - 3;
                for (int i = 0; i < monthFormatPadding; i++)
                {
                    newFormat = newFormat.Insert(match.Index + i + 1, "m");
                }
                char[] valueArray = value.ToCharArray();
                char[] formatArray = newFormat.ToCharArray();

                List<char> monthArray = new List<char>();
                List<char> dayArray = new List<char>();
                List<char> yearArray = new List<char>();
                (distanceDMMM, dayArray, monthArray, yearArray) = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                distanceDMMM += !DateHelper.DIGITS.Contains(dayArray[0].ToString()) || dayArray[0] == '0' ? 1 : 0;
                HashSet<string> months = new HashSet<string>();
                _monthsDictionary.TryGetValue(monthLength, out months);
                int monthDistance = Int16.MaxValue;
                foreach (string month in months)
                {
                    int currentMonthDistance = 0;
                    char[] monthDictArray = month.ToCharArray();
                    for (int i = 0; i < monthDictArray.Length; i++)
                    {
                        if (monthDictArray[i] != monthArray[i])
                            currentMonthDistance++;
                    }
                    monthDistance = monthDistance < currentMonthDistance ? monthDistance : currentMonthDistance;
                }
                distanceDMMM = distanceDMMM + monthDistance;

                if (distanceDMMM == 0)
                    distanceDMMM += DateHelper.GetDayLongMonthDateValidationDistance(dayArray, monthArray);
                distanceDMMM += DateHelper.GetYearDistance(dayArray, monthArray, yearArray);
            }

            else distanceDMMM = Int16.MaxValue;
            if(distanceDMMM > 0) {
                Regex dRegex = new Regex("d");
                Match dMatch = dRegex.Match(format);
                string newFormatDDMMM = format.Insert(dMatch.Index + 1, "d");
                int distanceDDMMM = GetDistanceFromFormatDDMMM(value, newFormatDDMMM);
                return distanceDMMM < distanceDDMMM ? distanceDMMM : distanceDDMMM;
            }
            return distanceDMMM;
        }

        private DateResult CreateDateResult(Match match, string format, int distance)
        {
            DateResult dateResult = new DateResult();
            dateResult.Status = distance > 0 ? Status.Error : Status.OK;
            dateResult.Value = match.Value.Substring(1, (match.Value.Length - 2)); ;
            dateResult.StartIndex = match.Index + 1;
            dateResult.EndIndex = match.Index + match.Length - 2;
            dateResult.Format.Add(format, distance);

            return dateResult;
        }

        private List<DateResult> ExtendDateResults(List<DateResult> dateResults, DateResult dateResult)
        {
            if (dateResults.Contains(dateResult))
            {
                DateResult primaryDt = dateResults.Find(obj => obj.Equals(dateResult));
                foreach(KeyValuePair<string, int> kvp in dateResult.Format) 
                {
                    if (kvp.Value == 0)
                        primaryDt.Status = Status.OK;
                    primaryDt.Format.Add(kvp.Key, kvp.Value); 
                }
            }
            else
            {
                dateResults.Add(dateResult);
            }
            return dateResults;
        }

        private Dictionary<string, HashSet<string>> GetWordRegexes(Dictionary<int, HashSet<string>> lengthWithCorrespondingFormats)
        {
            Dictionary<string, HashSet<string>> regexFormats = new Dictionary<string, HashSet<string>>();
            foreach(int length in lengthWithCorrespondingFormats.Keys) {
                string regex = "[\\s\\[\\{\\n\\/\\\"](.{" + length + "})[\\s\\]\\}\\n\\/\\\"]";
                HashSet<string> formats;
                lengthWithCorrespondingFormats.TryGetValue(length, out formats);
                regexFormats.Add(regex, formats);
            }
            return regexFormats;
        }

        private List<string> ScanFormats(string pathToFile)
        {
            List<string> formats = new List<string>();
            using (StreamReader reader = new StreamReader(pathToFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    formats.Add(line);
                }
            }
            return formats;
        }

        private Dictionary<int, HashSet<string>> GetLengthFormatsDictionary(List<string> formats)
        {
            Dictionary<string, HashSet<int>> formatLengths = new Dictionary<string, HashSet<int>>();
            foreach (string format in formats)
            {
                int length = format.Length;
                formatLengths.Add(format, new HashSet<int>(new int[] { length }));

                if (format.Contains("d") && !format.Contains("dd"))
                {
                    HashSet<int> lengths;
                    HashSet<int> newLengths = new HashSet<int>();
                    formatLengths.TryGetValue(format, out lengths);
                    foreach (int formatLength in lengths)
                    {
                        newLengths.Add(formatLength + 1);
                    }
                    lengths.UnionWith(newLengths);
                }

                if (format.ToLower().Contains("m") && !format.ToLower().Contains("mm"))
                {
                    HashSet<int> lengths;
                    HashSet<int> newLengths = new HashSet<int>();
                    formatLengths.TryGetValue(format, out lengths);
                    foreach (int formatLength in lengths)
                    {
                        newLengths.Add(formatLength + 1);
                    }
                    lengths.UnionWith(newLengths);
                }

                if (format.ToLower().Contains("mmm"))
                {
                    HashSet<int> lengths;
                    HashSet<int> newLengths = new HashSet<int>();
                    formatLengths.TryGetValue(format, out lengths);
                    foreach (int formatLength in lengths)
                    {
                        foreach (int monthLength in _monthsDictionary.Keys)
                            newLengths.Add(formatLength - 3 + monthLength);
                    }
                    lengths.Clear();
                    lengths.UnionWith(newLengths);

                }
            }

            Dictionary<int, HashSet<string>> lengthsFormats = new Dictionary<int, HashSet<string>>();
            foreach (KeyValuePair<string, HashSet<int>> entry in formatLengths)
            {
                HashSet<int> lengths = entry.Value;
                foreach (int length in lengths)
                {
                    if (lengthsFormats.ContainsKey(length))
                    {
                        HashSet<string> newFormats;
                        lengthsFormats.TryGetValue(length, out newFormats);
                        newFormats.Add(entry.Key);
                    }
                    else
                    {
                        lengthsFormats.Add(length, new HashSet<string>(new string[] { entry.Key }));
                    }
                }
            }
            return lengthsFormats;
        }
    }
}
