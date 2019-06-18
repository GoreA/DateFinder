using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace DateDetectorV2.Dater
{
    public class DateFinder
    {
        private string _stringToScan;
        private List<string> _formats;
        private List<string> _months;
        private int _distance;
        private string _outputFormat;
        private Dictionary<int, HashSet<string>> _monthsDictionary;

        public DateFinder(string stringToScan, List<string> formats, List<string> months, int distance, string outputFormat)
        {
            if (string.IsNullOrEmpty(stringToScan))
            {
                throw new System.ArgumentException("StringToScan parameter cannot be null or empty", stringToScan);
            }
            else
                _stringToScan = stringToScan;

            if ((formats != null) && (!formats.Any()))
            {
                throw new System.ArgumentException("Formats list parameter cannot be null or empty");
            }
            else
                _formats = formats;

            if ((months != null) && (!months.Any()))
            {
                throw new System.ArgumentException("Months list parameter cannot be null or empty");
            }
            else
            {
                _monthsDictionary = DateHelper.GetLengthMonthsDictionary(months);
                _months = months;
            }

            if (distance < 0)
            {
                throw new System.ArgumentException("Distance must be equal or greater than 0");
            }
            else
                _distance = distance;

            if (string.IsNullOrEmpty(outputFormat))
            {
                throw new System.ArgumentException("OutputFormat parameter cannot be null or empty");
            }
            else
                _outputFormat = outputFormat;
        }

        /// <summary>
        /// Scans given file to find any date representation that corresponds to
        /// given formats.
        /// </summary>
        /// <returns>a JSON string that represents DateResult objects.</returns>
        public List<DateResult> DetectDates()
        {
            Dictionary<int, HashSet<string>> lengthWithCorrespondingFormats = GetLengthFormatsDictionary(_formats);
            Dictionary<string, HashSet<string>> wordRegexes = GetWordRegexes(lengthWithCorrespondingFormats);
            List<DateResult> dateResults = GetDateResultListFromWordRegEx(wordRegexes);
            return dateResults;
        }

        public string DetectJSONDates()
        {
            List<DateResult> dateResults = DetectDates();
            return JsonConvert.SerializeObject(dateResults);
        }

    private List<DateResult> GetDateResultListFromWordRegEx(Dictionary<string, HashSet<string>> wordRegexes)
        {
            List<DateResult> dateResults = new List<DateResult>();
            foreach (KeyValuePair<string, HashSet<string>> wordRegexFormats in wordRegexes)
            {
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
                foreach (string format in formats)
                {
                    DateResult dateResult = GetDateResultFromWordFormats(match, format);
                    if (dateResult != null)
                    {
                        dateResults = ExtendDateResults(dateResults, dateResult);
                    }
                }
            }
            return dateResults;
        }

        private DateResult GetDateResultFromWordFormats(Match match, string format)
        {
            Tuple<int, List<string>> distance_valueToDetec;
            distance_valueToDetec = GetDistanceSupposedValue(match.Value, format);
            if (distance_valueToDetec.Item1 <= _distance)
            {
                return CreateDateResult(match, format, distance_valueToDetec.Item1, distance_valueToDetec.Item2);
            }
            else return null;
        }

        private Tuple<int, List<string>> GetDistanceSupposedValue(string value, string format)
        {
            value = value.Substring(1, (value.Length - 2));

            List<string> supposedValue = new List<string>();
            char[] valueArray = value.ToCharArray();
            char[] formatArray = format.ToCharArray();
            // filter out formats with alphabetic months representation
            if (format.ToLower().Contains("mmm"))
            {
                if (format.ToLower().Contains("d") && !format.ToLower().Contains("dd"))
                {
                    return GetDistanceSupposedValueFromFormatDMMM(value, format);
                }
                else
                {
                    return GetDistanceSupposedValueFromFormatDDMMM(value, format);
                }
            }
            else
            {
                // formats with 1 d -> d.m.yy
                if (format.ToLower().Contains("d") && !format.ToLower().Contains("dd"))
                {
                    if (format.ToLower().Contains("m") && !format.ToLower().Contains("mm"))
                    {
                        return GetDistanceSupposedValueFromFormatDM(value, format);
                    }
                    else
                    {
                        return GetDistanceSupposedValueFromFormatDMM(value, format);
                    }
                }
                // formats with 2 d -> dd.mm.yy
                else
                {
                    if (format.ToLower().Contains("m") && !format.ToLower().Contains("mm"))
                    {
                        return GetDistanceSupposedValueFromFormatDDM(value, format);
                    }
                    else
                    {
                        return GetDistanceSupposedValueFromFormatDDMM(value, format);
                    }
                }
            }
        }

        private Tuple<int, List<string>> GetDistanceSupposedValueFromFormatDDMM(string value, string format)
        {
            List<string> supposedValue = new List<string>();
            char[] valueArray = value.ToCharArray();
            char[] formatArray = format.ToCharArray();

            Tuple<int, List<char>, List<char>, List<char>> distance_dayArray_monthArray_yearArray = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
            int distance = distance_dayArray_monthArray_yearArray.Item1;
            List<char> dayArray = distance_dayArray_monthArray_yearArray.Item2;
            List<char> monthArray = distance_dayArray_monthArray_yearArray.Item3;
            List<char> yearArray = distance_dayArray_monthArray_yearArray.Item4;

            Tuple<int, string> dayDistance_dayWithDash = DateHelper.GetDayDistance(dayArray);
            distance += dayDistance_dayWithDash.Item1;

            Tuple<int, string> monthDistance_monthWithDash = DateHelper.GetMonthDistance(dayArray, monthArray, distance);
            distance += monthDistance_monthWithDash.Item1;

            Tuple<int, string>  yearDistance_yearWithDash = DateHelper.GetYearDistance(dayArray, monthArray, yearArray, distance);
            distance += yearDistance_yearWithDash.Item1;

            supposedValue.Add(dayDistance_dayWithDash.Item2);
            supposedValue.Add(monthDistance_monthWithDash.Item2);
            supposedValue.Add(yearDistance_yearWithDash.Item2);
            return Tuple.Create(distance, supposedValue);
        }

        private Tuple<int, List<string>> GetDistanceSupposedValueFromFormatDDM(string value, string format)
        {
            List<string> supposedValue = new List<string>();
            char[] valueArray = value.ToCharArray();
            char[] formatArray = format.ToCharArray();
            if (value.Length == format.Length + 1)
            {
                Regex mRegex = new Regex("[mM]");
                Match match = mRegex.Match(format);
                string newFormat = format.Insert(match.Index + 1, "m");
                return GetDistanceSupposedValueFromFormatDDMM(value, newFormat);
            }
            else
            {
                Tuple<int, List<char>, List<char>, List<char>> distance_dayArray_monthArray_yearArray = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                int distance = distance_dayArray_monthArray_yearArray.Item1;
                List<char> dayArray = distance_dayArray_monthArray_yearArray.Item2;
                List<char> monthArray = distance_dayArray_monthArray_yearArray.Item3;
                List<char> yearArray = distance_dayArray_monthArray_yearArray.Item4;
                Tuple<int, string> dayDistance_dayWithDash = DateHelper.GetDayDistance(dayArray);
                distance += dayDistance_dayWithDash.Item1;

                string monthWithDash = monthArray[0].ToString();
                if (!DateHelper.DIGITS.Contains(monthArray[0].ToString()) || monthArray[0] == '0')
                {
                    monthWithDash += "_";
                }
                if (distance == 0)
                    distance += DateHelper.GetDayMonthDateValidationDistance(dayArray, monthArray);

                Tuple<int, string> yearDistance_yearWithDash = DateHelper.GetYearDistance(dayArray, monthArray, yearArray, distance);
                distance += yearDistance_yearWithDash.Item1;
                supposedValue.Add(dayDistance_dayWithDash.Item2);
                supposedValue.Add(monthWithDash);
                supposedValue.Add(yearDistance_yearWithDash.Item2);
                return Tuple.Create(distance, supposedValue);
            }
        }

        private Tuple<int, List<string>> GetDistanceSupposedValueFromFormatDMM(string value, string format)
        {
            List<string> supposedValue = new List<string>();

            char[] valueArray = value.ToCharArray();
            char[] formatArray = format.ToCharArray();
            if (value.Length == format.Length + 1)
            {
                Regex mRegex = new Regex("d");
                Match match = mRegex.Match(format);
                string newFormat = format.Insert(match.Index + 1, "d");
                return GetDistanceSupposedValueFromFormatDDMM(value, newFormat);
            }
            else
            {
                Tuple<int, List<char>, List<char>, List<char>> distance_dayArray_monthArray_yearArray = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                int distance = distance_dayArray_monthArray_yearArray.Item1;
                List<char> dayArray = distance_dayArray_monthArray_yearArray.Item2;
                List<char> monthArray = distance_dayArray_monthArray_yearArray.Item3;
                List<char> yearArray = distance_dayArray_monthArray_yearArray.Item4;

                string dayWithDash = dayArray[0].ToString();
                if (!DateHelper.DIGITS.Contains(dayArray[0].ToString()) || dayArray[0] == '0')
                {
                    dayWithDash += "_";
                    distance++;
                }

                Tuple<int, string> monthDistance_monthWithDash = DateHelper.GetMonthDistance(dayArray, monthArray, distance);
                distance += monthDistance_monthWithDash.Item1;

                Tuple<int, string> yearDistance_yearWithDash = DateHelper.GetYearDistance(dayArray, monthArray, yearArray, distance);
                distance += yearDistance_yearWithDash.Item1;
                supposedValue.Add(dayWithDash);
                supposedValue.Add(monthDistance_monthWithDash.Item2);
                supposedValue.Add(yearDistance_yearWithDash.Item2);
                return Tuple.Create(distance, supposedValue);
            }
        }

        private Tuple<int, List<string>> GetDistanceSupposedValueFromFormatDM(string value, string format)
        {
            int distance = 0;
            List<string> supposedValue = new List<string>();

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
                return GetDistanceSupposedValueFromFormatDDMM(value, newFormat);
            }
            else
            if (value.Length == format.Length + 1)
            {
                Regex mRegex = new Regex("d");
                Match match = mRegex.Match(format);
                string newFormat = format.Insert(match.Index + 1, "d");
                Tuple<int, List<string>> distance1_supposedValue1 = GetDistanceSupposedValueFromFormatDDM(value, newFormat);
                int distance1 = distance1_supposedValue1.Item1;
                List<string> supposedValue1 = distance1_supposedValue1.Item2;

                mRegex = new Regex("[mM]");
                match = mRegex.Match(format);
                newFormat = format.Insert(match.Index + 1, "m");
                Tuple<int, List<string>> distance2_supposedValue2 = GetDistanceSupposedValueFromFormatDMM(value, newFormat);
                int distance2 = distance2_supposedValue2.Item1;
                List<string> supposedValue2 = distance2_supposedValue2.Item2;

                distance = distance1 < distance2 ? distance1 : distance2;
                supposedValue = distance1 < distance2 ? supposedValue1 : supposedValue2;
                return Tuple.Create(distance, supposedValue);
            }
            else
            {
                Tuple<int, List<char>, List<char>, List<char>> distance_dayArray_monthArray_yearArray = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                distance = distance_dayArray_monthArray_yearArray.Item1;
                List<char> dayArray = distance_dayArray_monthArray_yearArray.Item2;
                List<char> monthArray = distance_dayArray_monthArray_yearArray.Item3;
                List<char> yearArray = distance_dayArray_monthArray_yearArray.Item4;

                string dayWithDash = dayArray[0].ToString();
                if (!DateHelper.DIGITS.Contains(dayArray[0].ToString()) || dayArray[0] == '0')
                {
                    dayWithDash += "_";
                    distance++;
                }

                string monthWithDash = monthArray[0].ToString();
                if (!DateHelper.DIGITS.Contains(monthArray[0].ToString()) || monthArray[0] == '0')
                {
                    monthWithDash += "_";
                    distance++;
                }

                if (distance == 0)
                    distance += DateHelper.GetDayMonthDateValidationDistance(dayArray, monthArray);

                Tuple<int, string> yearDistance_yearWithDash = DateHelper.GetYearDistance(dayArray, monthArray, yearArray, distance);
                distance += yearDistance_yearWithDash.Item1;
                supposedValue.Add(dayWithDash);
                supposedValue.Add(monthWithDash);
                supposedValue.Add(yearDistance_yearWithDash.Item2);
                return Tuple.Create(distance, supposedValue);
            }
        }

        private Tuple<int, List<string>> GetDistanceSupposedValueFromFormatDDMMM(string value, string format)
        {
            int distance = 0;
            List<string> supposedValue = new List<string>();

            string newFormat = format;
            int monthLength = value.Length - format.Length + 3;
            if (_monthsDictionary.ContainsKey(monthLength))
            {
                string monthWithDash = string.Empty;
                Regex mRegex = new Regex("[mM]");
                Match match = mRegex.Match(format);
                int monthFormatPadding = monthLength - 3;
                for (int i = 0; i < monthFormatPadding; i++)
                {
                    newFormat = newFormat.Insert(match.Index + i + 1, "m");
                }

                char[] valueArray = value.ToCharArray();
                char[] formatArray = newFormat.ToCharArray();
                Tuple<int, List<char>, List<char>, List<char>> distance_dayArray_monthArray_yearArray = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                distance = distance_dayArray_monthArray_yearArray.Item1;
                List<char> dayArray = distance_dayArray_monthArray_yearArray.Item2;
                List<char> monthArray = distance_dayArray_monthArray_yearArray.Item3;
                List<char> yearArray = distance_dayArray_monthArray_yearArray.Item4;

                Tuple<int, string> dayDistance_dayWithDash = DateHelper.GetDayDistance(dayArray);
                distance += dayDistance_dayWithDash.Item1;

                monthWithDash = new string(monthArray.ToArray());
                HashSet<string> months = new HashSet<string>();
                _monthsDictionary.TryGetValue(monthLength, out months);
                int monthDistance = Int16.MaxValue;
                foreach (string month in months)
                {
                    int currentMonthDistance = 0;
                    char[] monthDictArray = month.ToCharArray();
                    List<char> monthWithDashArray = new List<char>();
                    for (int i = 0; i < monthDictArray.Length; i++)
                    {
                        monthWithDashArray.Add(monthArray[i]);
                        if (monthDictArray[i] != monthArray[i])
                        {
                            monthWithDashArray.Add('_');
                            currentMonthDistance++;
                        }
                    }
                    string calculatedMonthWithDash = new string(monthWithDashArray.ToArray());
                    if (currentMonthDistance < monthDistance)
                    {
                        monthDistance = currentMonthDistance;
                        monthWithDash = calculatedMonthWithDash;
                    }
                    else if (currentMonthDistance == monthDistance && 
                        !monthWithDash.Split(',').ToList().Contains(calculatedMonthWithDash))
                    {
                        monthWithDash = monthWithDash + "," + calculatedMonthWithDash;
                    }
                }

                distance += monthDistance;
                if (distance == 0)
                    distance += DateHelper.GetDayLongMonthDateValidationDistance(dayArray, monthArray);

                Tuple<int, string> yearDistance_yearWithDash = DateHelper.GetYearDistance(dayArray, monthArray, yearArray, distance);
                distance += yearDistance_yearWithDash.Item1;
                supposedValue.Add(dayDistance_dayWithDash.Item2);
                supposedValue.Add(monthWithDash);
                supposedValue.Add(yearDistance_yearWithDash.Item2);
            }
            else distance = Int16.MaxValue;

            return Tuple.Create(distance, supposedValue);
        }

        private Tuple<int, List<string>> GetDistanceSupposedValueFromFormatDMMM(string value, string format)
        {
            int distanceDMMM = 0;
            List<string> supposedValue = new List<string>();
            string newFormat = format;
            int monthLength = value.Length - format.Length + 3;
            if (_monthsDictionary.ContainsKey(monthLength))
            {
                string dayWithDash = string.Empty;
                string monthWithDash = string.Empty;
                string yearWithDash = string.Empty;
                Regex mRegex = new Regex("[mM]");
                Match match = mRegex.Match(format);
                int monthFormatPadding = monthLength - 3;
                for (int i = 0; i < monthFormatPadding; i++)
                {
                    newFormat = newFormat.Insert(match.Index + i + 1, "m");
                }
                char[] valueArray = value.ToCharArray();
                char[] formatArray = newFormat.ToCharArray();

                Tuple<int, List<char>, List<char>, List<char>> distance_dayArray_monthArray_yearArray = DateHelper.GetSymbolDistanceDayMonthYearArrays(formatArray, valueArray);
                distanceDMMM = distance_dayArray_monthArray_yearArray.Item1;
                List<char> monthArray = distance_dayArray_monthArray_yearArray.Item2;
                List<char> dayArray = distance_dayArray_monthArray_yearArray.Item3;
                List<char> yearArray = distance_dayArray_monthArray_yearArray.Item4;

                dayWithDash = new string(dayArray.ToArray());
                if (!DateHelper.DIGITS.Contains(dayArray[0].ToString()) || dayArray[0] == '0') {
                    dayWithDash += "_";
                    distanceDMMM += 1;
                }

                monthWithDash = new string(monthArray.ToArray());
                HashSet<string> months = new HashSet<string>();
                _monthsDictionary.TryGetValue(monthLength, out months);
                int monthDistance = Int16.MaxValue;
                foreach (string month in months)
                {
                    int currentMonthDistance = 0;
                    char[] monthDictArray = month.ToCharArray();
                    List<char> monthWithDashArray = new List<char>();
                    for (int i = 0; i < monthDictArray.Length; i++)
                    {
                        monthWithDashArray.Add(monthArray[i]);
                        if (monthDictArray[i] != monthArray[i])
                        {
                            monthWithDashArray.Add('_');
                            currentMonthDistance++;
                        }
                    }
                    string calculatedMonthWithDash = new string(monthWithDashArray.ToArray());
                    if (currentMonthDistance < monthDistance) {
                        monthDistance = currentMonthDistance;
                        monthWithDash = calculatedMonthWithDash;
                    }
                    else if (currentMonthDistance == monthDistance)
                    {
                        monthWithDash = monthWithDash + "," + calculatedMonthWithDash;
                    }
                }
                distanceDMMM = distanceDMMM + monthDistance;

                if (distanceDMMM == 0)
                    distanceDMMM += DateHelper.GetDayLongMonthDateValidationDistance(dayArray, monthArray);

                Tuple<int, string> yearDistance_yearWithDash = DateHelper.GetYearDistance(dayArray, monthArray, yearArray, distanceDMMM);
                distanceDMMM += yearDistance_yearWithDash.Item1;
                supposedValue.Add(dayWithDash);
                supposedValue.Add(monthWithDash);
                supposedValue.Add(yearDistance_yearWithDash.Item2);
            }

            else distanceDMMM = Int16.MaxValue;
            if (distanceDMMM > 0)
            {
                Regex dRegex = new Regex("d");
                Match dMatch = dRegex.Match(format);
                string newFormatDDMMM = format.Insert(dMatch.Index + 1, "d");
                Tuple<int, List<string>> distanceDDMMM_supposedValueDDMMM = GetDistanceSupposedValueFromFormatDDMMM(value, newFormatDDMMM);
                int distanceDDMMM = distanceDDMMM_supposedValueDDMMM.Item1;
                List<string> supposedValueDDMMM = distanceDDMMM_supposedValueDDMMM.Item2;
                return distanceDMMM < distanceDDMMM ? Tuple.Create(distanceDMMM, supposedValue) : Tuple.Create(distanceDDMMM, supposedValueDDMMM);
            }
            return Tuple.Create(distanceDMMM, supposedValue);
        }

        private DateResult CreateDateResult(Match match, string format, int distance, List<string> valueToDetec)
        {
            DateResult dateResult = new DateResult();
            dateResult.Status = distance > 0 ? Status.Error : Status.OK;
            dateResult.OriginalValue = match.Value.Substring(1, (match.Value.Length - 2));
            DateIdentificator dateIdentificator = new DateIdentificator(_months, _outputFormat);
            Tuple<string, float> supposedValue_accuracy = dateIdentificator.GetSupposedValue(valueToDetec);
            dateResult.SupposedValue = supposedValue_accuracy.Item1;
            dateResult.Accuracy = supposedValue_accuracy.Item2;
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
                foreach (KeyValuePair<string, int> kvp in dateResult.Format)
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
            foreach (int length in lengthWithCorrespondingFormats.Keys)
            {
                HashSet<string> formats;
                lengthWithCorrespondingFormats.TryGetValue(length, out formats);
                regexFormats.Add($"[\\s](.{{{length}}})[\\s\\]}}\\n\\/\\\"]", formats);
                regexFormats.Add($"[\\[](.{{{length}}})[\\s\\]}}\\n\\/\\\"]", formats);
                regexFormats.Add($"[{{](.{{{length}}})[\\s\\]}}\\n\\/\\\"]", formats);
                regexFormats.Add($"[\\n](.{{{length}}})[\\s\\]}}\\n\\/\\\"]", formats);
                regexFormats.Add($"[\\/](.{{{length}}})[\\s\\]}}\\n\\/\\\"]", formats);
                regexFormats.Add($"[\\\\](.{{{length}}})[\\s\\]}}\\n\\/\\\"]", formats);
                regexFormats.Add($"[\"](.{{{length}}})[\\s\\]}}\\n\\/\\\"]", formats);
            }
            return regexFormats;
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