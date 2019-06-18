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
        public static Dictionary<int, HashSet<string>> GetLengthMonthsDictionary(List<string> months)
        {
            Dictionary<int, HashSet<string>> monthsDictionary = new Dictionary<int, HashSet<string>>();
            foreach (string month in months)
            {
                if (monthsDictionary.ContainsKey(month.Trim().Length))
                {
                    HashSet<string> setOfMonths;
                    monthsDictionary.TryGetValue(month.Trim().Length, out setOfMonths);
                    setOfMonths.Add(month.Trim());
                }
                else
                {
                    monthsDictionary.Add(month.Trim().Length, new HashSet<string>(new string[] { month.Trim() }));
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
        public static (int, string) GetYearDistance(List<char> dayArray, List<char> monthArray, List<char> yearArray, int cumulativeDistance)
        {
            int distance = 0;
            List<char> yearWithDash = new List<char>();
            if (yearArray.Count > 0)
            {
                if (yearArray.Count == 2)
                {
                    yearWithDash.Add(yearArray[0]);
                    if (!DIGITS.Contains(yearArray[0].ToString()))
                    {
                        distance++;
                        yearWithDash.Add('_');
                    }
                    yearWithDash.Add(yearArray[1]);
                    if (!DIGITS.Contains(yearArray[1].ToString()))
                    {
                        distance++;
                        yearWithDash.Add('_');
                    }
                }
                else
                {
                    yearWithDash.Add(yearArray[0]);
                    if (!DIGITS.Contains(yearArray[0].ToString()) || yearArray[0] == '0')
                    {
                        distance++;
                        yearWithDash.Add('_');
                    }
                    for (int i = 1; i < yearArray.Count; i++)
                    {
                        yearWithDash.Add(yearArray[i]);
                        if (!DIGITS.Contains(yearArray[i].ToString()))
                        {
                            distance++;
                            yearWithDash.Add('_');
                        }
                    }
                }
                if (distance + cumulativeDistance == 0)
                {
                    string date = BuildDate(dayArray, monthArray, yearArray);
                    bool isDateValid = ValidateDate(date, "dd.mm.yyyy");
                    if (!isDateValid) distance++;
                }
            }
            return (distance, new string(yearWithDash.ToArray()));
        }

        /// <summary>
        /// Calculates the month string distance.
        /// </summary>
        /// <returns>The month distance.</returns>
        /// <param name="dayArray">Day array.</param>
        /// <param name="monthArray">Month array.</param>
        public static (int, string) GetMonthDistance(List<char> dayArray, List<char> monthArray, int cumulativeDistance)
        {
            int distance = 0;
            List<char> monthWithDash = new List<char>();
            switch (monthArray[0])
            {
                case '0':
                    monthWithDash.Add(monthArray[0]);
                    monthWithDash.Add(monthArray[1]);
                    if (!DIGITS.Contains(monthArray[1].ToString()) || monthArray[1] == '0')
                    {
                        distance++;
                        monthWithDash.Add('_');
                    }
                    break;
                case '1':
                    monthWithDash.Add(monthArray[0]);
                    monthWithDash.Add(monthArray[1]);
                    if (monthArray[1] != '0' && monthArray[1] != '1' && monthArray[1] != '2')
                    {
                        distance++;
                        monthWithDash.Add('_');
                    }
                    break;
                default:
                    monthWithDash.Add(monthArray[0]);
                    monthWithDash.Add('_');
                    distance++;
                    monthWithDash.Add(monthArray[1]);
                    if (!DIGITS.Contains(monthArray[1].ToString()))
                    {
                        distance++;
                        monthWithDash.Add('_');
                    }
                    break;
            }

            if (distance + cumulativeDistance == 0)
            {
                string date = DateHelper.BuildDate(dayArray, monthArray);
                bool isDateValid = DateHelper.ValidateDate(date + ".2000", "dd.mm.yyyy");
                if (!isDateValid) distance++;
            }
            return (distance, new string(monthWithDash.ToArray()));
        }

        /// <summary>
        /// Calculates the day string distance.
        /// </summary>
        /// <returns>The day distance.</returns>
        /// <param name="dayArray">Day array.</param>
        public static (int, string) GetDayDistance(List<char> dayArray)
        {
            int distance = 0;
            List<char> dayWithDash = new List<char>();
            switch (dayArray[0])
            {
                case '0':
                    dayWithDash.Add(dayArray[0]);
                    dayWithDash.Add(dayArray[1]);
                    if (!DIGITS.Contains(dayArray[1].ToString()) || dayArray[1] == '0')
                    {
                        dayWithDash.Add('_');
                        distance++;
                    }
                    break;
                case '1':
                case '2':
                    dayWithDash.Add(dayArray[0]);
                    dayWithDash.Add(dayArray[1]);
                    if (!DIGITS.Contains(dayArray[1].ToString()))
                    {
                        dayWithDash.Add('_');
                        distance++;
                    }
                    break;
                case '3':
                    dayWithDash.Add(dayArray[0]);
                    dayWithDash.Add(dayArray[1]);
                    if (dayArray[1] != '0' && dayArray[1] != '1')
                    {
                        dayWithDash.Add('_');
                        distance++;
                    }
                    break;
                default:
                    dayWithDash.Add(dayArray[0]);
                    dayWithDash.Add('_');
                    distance++;
                    dayWithDash.Add(dayArray[1]);
                    if (!DIGITS.Contains(dayArray[1].ToString()))
                    {
                        dayWithDash.Add('_');
                        distance++;
                    }
                    break;
            }
            return (distance, new string(dayWithDash.ToArray()));
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
            return DateTime.TryParseExact(date, "dd MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                DateTime.TryParseExact(date, "dd MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                DateTime.TryParseExact(date, "d MMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                DateTime.TryParseExact(date, "d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
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