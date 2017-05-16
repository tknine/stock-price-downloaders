using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace findata_dl
{
    /// <summary>
    /// Converts a string to a stream for use in the CSV library.
    /// </summary>
    public static class Utility
    {
        public static StreamReader ToStreamReader(this string str)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(str);
            //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
            MemoryStream stream = new MemoryStream(byteArray);


            // convert stream to string
            return new StreamReader(stream);
        }
    }

    public static class DateUtility
    {
        public static string ConvertDateStringFormat(string oldDate, string expectedFormat, string newFormat)
        {
            //yyyy-MM-dd, MM/dd/yyyy

            DateTime newDate;

            bool result = DateTime.TryParseExact(
                oldDate,
                expectedFormat,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out newDate);

            if (result)
            {
                return newDate.ToString(newFormat);
            }
            else
            {
                return oldDate;
            }
        }
    }
    


    /// <summary>
    /// Provide date comparer for the data from files or from server.  Assumes date is in first index.
    /// </summary>
    public class CsvDateComparer : IEqualityComparer<string[]>
    {
        private string dateFormat;

        public CsvDateComparer(string dateFormat)
        {
            this.dateFormat = dateFormat;
        }

        public CsvDateComparer()
        {
            this.dateFormat = "MM/dd/yyyy";
        }

        public bool Equals(string[] x, string[] y)
        {
            try
            {
                DateTime xd = DateTime.Parse(x[0]);
                DateTime yd = DateTime.Parse(y[0]);

                return xd == yd;
            }
            catch
            {
                return false;
            }
        }

        public int GetHashCode(string[] obj)
        {
            return obj[0].GetHashCode();
        }
    }
}
