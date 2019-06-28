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
            List<string> formats = new List<string>(new string[] { "dd MMM yyyy", "dd MMM yy", "dd MM yy",
                        "d MMM yyyy", "d MMM yy", "dd/MM/yyyy", "dd-MMM-yyyy", "yyyy-MM-dd", 
                        "dd/MM/yy", "dd MMM", "ddMMM"});
            List<string> months = new List<string>(new string[] { "January", "February", "March", "April",
                        "May", "Jun", "July", "August", "September", "October", "November", "December", 
                        "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", 
                        "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" });
            var pathToTestFile = Path.Combine(currentDirectory, @"Resources/A_tale_of_two_cities.txt");
            Dictionary<int, HashSet<string>> monthsDictionary = DateHelper.GetLengthMonthsDictionary(formats);

            using (StreamReader reader = new StreamReader(pathToTestFile))
            {
                string line;
                StringBuilder sb = new StringBuilder();
                while ((line = reader.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
                long start = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                DateFinder df = new DateFinder(sb.ToString(), formats, months, 2, "dd/MMM/yyyy");
                // Main method
                Console.WriteLine(df.DetectJSONDates());
                long end = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                Console.WriteLine((end - start) / 1000);
            }
        }
    }
}
