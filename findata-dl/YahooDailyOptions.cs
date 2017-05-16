using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace findata_dl
{
    [Verb("yahoo", HelpText = "Download Yahoo daily historical data.")]
    public class YahooDailyOptions : CommonSubOptions
    {
        [Option('d', "dir", Required = false,Default = "YahooDailyData", HelpText = "Folder to put downloads into. (Will be created if does not exist.)")]
        public string DataFolder { get; set; }

        [Option('t', "threads", Required = false, HelpText = "Number of threads for downloading.", Default = 5)]
        public int Threads { get; set; }

        [Option('a', "adjust", Required = false, HelpText = "Back adjust all data based on Adjusted Close.", Default = false)]
        public bool BackAdjust { get; set; }

        [Option('k', "removeadjust", Required = false, HelpText = "Remove the adjusted close from the data.", Default = true)]
        public bool RemoveAdjust { get; set; }

        [Option('n', "round", Required = false, HelpText = "Round decimal places.", Default = true)]
        public bool RoundDecimal { get; set; }

        [Option('c', "digits", Required = false, HelpText = "Round round decimal places digits.", Default = 2)]
        public int RoundDigits { get; set; }

        public YahooDailyOptions()
        {
            EndDate = DateTime.Now.Date;
            BeginDate = EndDate.AddYears(-1);
        }
    }
}
