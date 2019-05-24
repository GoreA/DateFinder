using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace DateDetectorV2.Dater
{
    public class DateHelper
    {
        public const string DIGITS = "0123456789";

        /// <summary>
        /// Gets a dictionary which key is an int number and value is a list of months names.
        /// The key represents the length of months from list. 
        /// The months names are scaned in file which path is given as method parameter.
        /// </summary>
        /// <returns>The length months dictionary.</returns>
        /// <param name="path">Path.</param>
        public static Dictionary<int, HashSet<string>> GetLengthMonthsDictionary(string path)
        {
            Dictionary<int, HashSet<string>> monthsDictionary = new Dictionary<int, HashSet<string>>();
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (monthsDictionary.ContainsKey(line.Trim().Length))
                    {
                        HashSet<string> setOfMonths;
                        monthsDictionary.TryGetValue(line.Trim().Length, out setOfMonths);
                        setOfMonths.Add(line.Trim());
                    }
                    else
                    {
                        monthsDictionary.Add(line.Trim().Length, new HashSet<string>(new string[] { line.Trim() }));
                    }
                }
            }
            return monthsDictionary;
        }

        /// <summary>
        /// Calculates the year string distance.
        /// </summary>
        /// <returns>The year distance.</returns>
        /// <param name="dayArray">Day array.</param>
        /// <param name="monthArray">Month array.</param>
        /// <param name="yearArray">Year array.</param>
        public static int GetYearDistance(List<char> dayArray, List<char> monthArray, List<char> yearArray)
        {
            int distance = 0;
            if (yearArray.Count > 0)
            {
                if (yearArray.Count == 2)
                {
                    if (!DIGITS.Contains(yearArray[0].ToString())) distance++;
                    if (!DIGITS.Contains(yearArray[1].ToString())) distance++;
                }
                else
                {
                    if (!DIGITS.Contains(yearArray[0].ToString()) || yearArray[0] == '0') distance++;
                    for (int i = 1; i < yearArray.Count; i++)
                    {
                        if (!DIGITS.Contains(yearArray[i].ToString())) distance++;
                    }
                }
                if (distance == 0)
                {
                    string date = BuildDate(dayArray, monthArray, yearArray);
                    bool isDateValid = ValidateDate(date, "dd.mm.yyyy");
                    if (!isDateValid) distance++;
                }
            }
            return distance;

        }

        /// <summary>
        /// Calculates the month string distance.
        /// </summary>
        /// <returns>The month distance.</returns>
        /// <param name="dayArray">Day array.</param>
        /// <param name="monthArray">Month array.</param>
        public static int GetMonthDistance(List<char> dayArray, List<char> monthArray)
        {
            int distance = 0;
            switch (monthArray[0])
            {
                case '0':
                    if (!DIGITS.Contains(monthArray[1].ToString()) || monthArray[1] == '0')
                    {
                        distance++;
                    }
                    break;
                case '1':
                    if (monthArray[1] != '0' && monthArray[1] != '1' && monthArray[1] != '2')
                    {
                        distance++;
                    }
                    break;
                default:
                    distance++;
                    distance += !DIGITS.Contains(monthArray[1].ToString()) ? 1 : 0;
                    break;
            }

            if (distance == 0)
            {
                string date = DateHelper.BuildDate(dayArray, monthArray);
                bool isDateValid = DateHelper.ValidateDate(date + ".2000", "dd.mm.yyyy");
                if (!isDateValid) distance++;
            }
            return distance;
        }

        /// <summary>
        /// Calculates the day string distance.
        /// </summary>
        /// <returns>The day distance.</returns>
        /// <param name="dayArray">Day array.</param>
        public static int GetDayDistance(List<char> dayArray)
        {
            int distance = 0;
            switch (dayArray[0])
            {
                case '0':
                    if (!DIGITS.Contains(dayArray[1].ToString()) || dayArray[1] == '0')
                    {
                        distance++;
                    }
                    break;
                case '1':
                case '2':
                    if (!DIGITS.Contains(dayArray[1].ToString()))
                    {
                        distance++;
                    }
                    break;
                case '3':
                    if (dayArray[1] != '0' && dayArray[1] != '1')
                    {
                        distance++;
                    }
                    break;
                default:
                    distance++;
                    distance += !DIGITS.Contains(dayArray[1].ToString()) ? 1 : 0;
                    break;
            }
            return distance;
        }

        /// <summary>
        /// Calculates the distance based only on symbols if they correspond 
        /// with delimiter symbols from given formats. 
        /// </summary>
        /// <returns>a tupple with 4 parameters: distance calculated from 
        /// delimiter symbols, array with digits from day representation, 
        /// delimiter symbols, array with digits from month representation and
        /// delimiter symbols, array with digits from year representation</returns>
        /// <param name="formatArray">Format array.</param>
        /// <param name="valueArray">Value array.</param>
        public static (int, List<char>, List<char>, List<char>) GetSymbolDistanceDayMonthYearArrays(char[] formatArray, char[] valueArray)
        {
            List<char> monthArray = new List<char>();
            List<char> dayArray = new List<char>();
            List<char> yearArray = new List<char>();
            int distance = 0;

            for (int i = 0; i < formatArray.Length; i++)
            {
                switch (formatArray[i])
                {
                    case 'd':
                        dayArray.Add(valueArray[i]); break;
                    case 'm':
                    case 'M':
                        monthArray.Add(valueArray[i]); break;
                    case 'y':
                        yearArray.Add(valueArray[i]); break;
                    default:
                        if (formatArray[i] != valueArray[i])
                        {
                            distance++;
                        }
                        break;
                }
            }
            return (distance, dayArray, monthArray, yearArray);
        }

        /// <summary>
        /// Validates if a day and a month name form a valid date together. E.g. 20 Apr
        /// For this scope we add a year 2000 to make sure that 29th of February 
        /// will also be found as a valid date.
        /// </summary>
        /// <returns>0 if date is valid, 1 otherwise.</returns>
        /// <param name="dayArray">Day array.</param>
        /// <param name="monthArray">Month array.</param>
        public static int GetDayLongMonthDateValidationDistance(List<char> dayArray, List<char> monthArray)
        {
            string date = DateHelper.BuildLongDate(dayArray, monthArray);
            bool isDateValid = DateHelper.ValidateLongDate(date + " 2000");
            if (!isDateValid) return 1;
            else return 0;
        }

        /// <summary>
        /// Validates if a day and a month name form a valid date together. E.g. 20.04
        /// For this scope we add a year 2000 to make sure that 29th of February
        ///  will also be found as a valid date.
        /// </summary>
        /// <returns>0 if date is valid, 1 otherwise.</returns>
        /// <param name="dayArray">Day array.</param>
        /// <param name="monthArray">Month array.</param>
        public static int GetDayMonthDateValidationDistance(List<char> dayArray, List<char> monthArray)
        {
            string date = DateHelper.BuildDate(dayArray, monthArray);
            bool isDateValid = DateHelper.ValidateDate(date + ".2000", "dd.mm.yyyy");
            if (!isDateValid) return 1;
            else return 0;
        }

        /// <summary>
        /// Validates the date which contains the month in numeric format.
        /// </summary>
        /// <returns><c>true</c>, if date was validated, <c>false</c> otherwise.</returns>
        /// <param name="date">Date.</param>
        /// <param name="format">Format.</param>
        public static bool ValidateDate(string date, string format)
        {
            return DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        /// <summary>
        /// Validates the date which contains the month in alphabetic format.
        /// </summary>
        /// <returns><c>true</c>, if long date was validated, <c>false</c> otherwise.</returns>
        /// <param name="date">Date.</param>
        public static bool ValidateLongDate(string date)
        {
            return DateTime.TryParse(date, out _);
        }

        /// <summary>
        /// Builds the date from day, month and year, where month has numeric format.
        /// </summary>
        /// <returns>The date.</returns>
        /// <param name="dayArray">Day array.</param>
        /// <param name="monthArray">Month array.</param>
        /// <param name="yearArray">Year array.</param>
        public static string BuildDate(List<char> dayArray, List<char> monthArray, List<char> yearArray)
        {
            string date = BuildDate(dayArray, monthArray);
            string year = string.Empty;
            foreach (char c in yearArray)
            {
                year = year + c;
            }
            year = year.PadLeft(4, '0');

            date = date + "." + year;
            return date;
        }

        /// <summary>
        /// Builds the date from day, month and year, where month has alphabetic format.
        /// </summary>
        /// <returns>The long date.</returns>
        /// <param name="dayArray">Day array.</param>
        /// <param name="monthArray">Month array.</param>
        /// <param name="yearArray">Year array.</param>
        public static string BuildLongDate(List<char> dayArray, List<char> monthArray, List<char> yearArray)
        {
            string date = BuildLongDate(dayArray, monthArray);
            string year = string.Empty;
            foreach (char c in yearArray)
            {
                year = year + c;
            }
            year = year.PadLeft(4, '0');

            date = date + " " + year;
            return date;
        }

        /// <summary>
        /// Builds the date from day and month, where month has numeric format.
        /// </summary>
        /// <returns>The date.</returns>
        /// <param name="dayArray">Day array.</param>
        /// <param name="monthArray">Month array.</param>
        public static string BuildDate(List<char> dayArray, List<char> monthArray)
        {
            string date = string.Empty;
            string day = string.Empty;
            foreach (char c in dayArray)
            {
                day = day + c;
            }
            day = day.PadLeft(2, '0');
            date = date + day + ".";
            string month = string.Empty;
            foreach (char c in monthArray)
            {
                month = month + c;
            }
            month = month.PadLeft(2, '0');
            date = date + month;
            return date;
        }

        /// <summary>
        /// Builds the date from day and month, where month has alphabetic format.
        /// </summary>
        /// <returns>The long date.</returns>
        /// <param name="dayArray">Day array.</param>
        /// <param name="monthArray">Month array.</param>
        public static string BuildLongDate(List<char> dayArray, List<char> monthArray)
        {
            string date = string.Empty;
            string day = string.Empty;
            foreach (char c in dayArray)
            {
                day = day + c;
            }
            day = day.PadLeft(2, '0');
            date = date + day + " ";
            string month = string.Empty;
            foreach (char c in monthArray)
            {
                month = month + c;
            }
            date = date + month;
            return date;
        }

    }
}
