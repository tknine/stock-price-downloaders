using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace findata_dl
{
    public abstract class CommonSubOptions
    {
        [Option('s', "symbols", Required = false, Default = "GOOG", HelpText = "Input symbols to download. (Quoted and space separated if more than one symbol)")]
        public string Symbol { get; set; }

        [Option('f', "file", Required = false, HelpText = "Input symbol file list to download.  (One symbol per line)")]
        public string SymbolFile { get; set; }

        [Option('e', "enddate", Required = false, HelpText = "End date of data download. (Default current date)")]
        public DateTime EndDate { get; set; }

        [Option('b', "begindate", Required = false, HelpText = "End date of data download. (Default 1 year before end date)")]
        public DateTime BeginDate { get; set; }

        [Option('r', "sort", Required = false, HelpText = "Sort data by date.", Default = true)]
        public bool DoSort { get; set; }

        [Option('m', "merge", Required = false, HelpText = "Merge new data with existing data if data file already exists.  New data overrides existing data.", Default = true)]
        public bool Merge { get; set; }

        [Option('g', "log", Required = false, Default = "-- download.log", HelpText = "Log actions and errors to specified file.)")]
        public string LogFile { get; set; }

        [Option('x', "dateformat", Required = false, Default = "MM/dd/yyyy", HelpText = "Date format. (MM=months, dd=days, yyyy|yy=years)")]
        public string DateFormat { get; set; }

        [Option('p', "sep", Required = false, Default = ",", HelpText = "Field separator.")]
        public string Separator { get; set; }


    }
}
