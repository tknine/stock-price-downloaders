using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GooglePriceDownloader
{
    class Options
    {
        /*
         *  -e "4/1/2017" -b "1/1/1970" -s "AAPL NUGT DUST CSCO IBM C NFLX FSLR GE SPY A AA" -t 1
         */


        //-s "AAPL NUGT DUST CSCO IBM C NFLX FSLR GE SPY A AA"

        [Option('s', "symbols", Required = false, Default = "GOOG", HelpText = "Input symbols to download. (Quoted and space separated if more than one symbol)")]
        public string Symbol { get; set; }

        //-f symbols.txt 

        [Option('f', "file", Required = false, HelpText = "Input symbol file list to download.  (One symbol per line)")]
        public string SymbolFile { get; set; }


        [Option('d', "dir", Required = false, Default = "Data", HelpText = "Folder to put donwloads into. (Will be created if does not exist.)")]
        public string DataFolder { get; set; }

        [Option('e', "enddate", Required = false, HelpText = "End date of data download. (Default current date)")]
        public DateTime EndDate { get; set; }

        [Option('b', "begindate", Required = false, HelpText = "End date of data download. (Default 1 year before end date)")]
        public DateTime BeginDate { get; set; }

        [Option('r', "sort", Required = false, HelpText = "Sort data by date.", Default = true)]
        public bool DoSort { get; set; }

        [Option('t', "threads", Required = false, HelpText = "Number of threads for downloading.", Default = 1)]
        public int Threads { get; set; }


        public Options()
        {
            EndDate = DateTime.Now.Date;
            BeginDate = EndDate.AddYears(-1);
        }
    }
}
