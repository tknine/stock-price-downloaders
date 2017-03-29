using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YahooPriceDownloader
{
    public class Options
    {
        [Option('s', "symbol", Required = false, HelpText = "Input symbol to download.")]
        public string Symbol { get; set; }

        [Option('l', "list", Required = false, HelpText = "Input symbol file list to download.  (One symbol per line.)")]
        public string SymbolFile { get; set; }

        [Option('e', "enddate", Required = false, HelpText = "End date of data download. (Default current date)")]
        public DateTime EndDate { get; set; }

        [Option('b', "begindate", Required = false, HelpText = "End date of data download. (Default 1 year before end date)")]
        public DateTime BeginDate { get; set; }

        [Option('s', "symbol", Required = false, HelpText = "Input symbol to download.")]
        public string Symbol { get; set; }
    }
}
