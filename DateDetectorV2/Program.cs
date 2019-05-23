using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using DateDetectorV2.Dater;

namespace DateDetectorV2
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var pathToFormats = Path.Combine(currentDirectory, @"Resources/Formats.txt");
            var pathToTestFile = Path.Combine(currentDirectory, @"Resources/A_tale_of_two_cities.txt");
            var pathForMonths = Path.Combine(currentDirectory, @"Resources/Months.txt");
            Dictionary<int, HashSet<string>> monthsDictionary = DateHelper.GetLengthMonthsDictionary(pathForMonths);

            using (StreamReader reader = new StreamReader(pathToTestFile))
            {
                string line;
                StringBuilder sb = new StringBuilder();
                while ((line = reader.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }

                long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                DateFinder df = new DateFinder(sb.ToString(), pathToFormats, pathForMonths, 1);
                Console.WriteLine(df.DetectDate());
                long end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                Console.WriteLine((end - start) / 1000);
            }
        }
    }
}
