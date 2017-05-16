using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace findata_dl
{
    [Verb("google", HelpText = "Download Google daily historical data.")]
    public class GoogleDailyOptions : CommonSubOptions
    {
        [Option('d', "dir", Required = false, Default = "GoogleDailyData", HelpText = "Folder to put downloads into. (Will be created if does not exist.)")]
        public string DataFolder { get; set; }

        [Option('t', "threads", Required = false, HelpText = "Number of threads for downloading.", Default = 1)]
        public int Threads { get; set; }

        [Option('n', "round", Required = false, HelpText = "Round decimal places.", Default = false)]
        public bool RoundDecimal { get; set; }

        [Option('c', "digits", Required = false, HelpText = "Round round decimal places digits.", Default = 2)]
        public int Digits { get; set; }


        public YahooDailyOptions GetAsYahooDailyOptions()
        {
            YahooDailyOptions yahooOptions = null;

            var optionsResult = CommandLine.Parser.Default.ParseArguments< YahooDailyOptions>(new string[] { "yahoo" })
                .MapResult(
                    (YahooDailyOptions options) => {
                        yahooOptions = options;
                        return 0;
                        },
                    errs => 1
                );

            yahooOptions.BeginDate = this.BeginDate;
            yahooOptions.EndDate = this.EndDate;
            yahooOptions.DateFormat = this.DateFormat;
            yahooOptions.DoSort = this.DoSort;

            return yahooOptions;
        }

        public GoogleDailyOptions()
        {
            EndDate = DateTime.Now.Date;
            BeginDate = EndDate.AddYears(-1);
        }
    }
}
