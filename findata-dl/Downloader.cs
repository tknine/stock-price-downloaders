using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using System.Diagnostics;

namespace findata_dl
{
    public static class Downloader
    {
        public static int Main(string[] args)
        {
            //map the CommandLine to the options object
            var optionsResult = CommandLine.Parser.Default.ParseArguments<GoogleDailyOptions, YahooDailyOptions>(args);

            return optionsResult.MapResult(
                (GoogleDailyOptions googleOptions) =>
                {

                    GoogleDailyData downloader = new GoogleDailyData(googleOptions);
                    downloader.Start();

                    //done
                    Console.WriteLine("Download Complete!");

                    if (Debugger.IsAttached)
                        Console.ReadKey();

                    return 0;
                },
                (YahooDailyOptions yahooOptions) =>
                {
                    if (yahooOptions.BackAdjust)
                    {
                        Console.Write("Yahoo back adjusting does not provide data that matches other sources.  Continue (Y/N)");

                        if (!string.Equals(Console.ReadLine(), "Y", StringComparison.CurrentCultureIgnoreCase))
                        {
                            return 0;
                        }
                    }

                    YahooDailyData downloader = new YahooDailyData(yahooOptions);
                    downloader.Start();

                    //done
                    Console.WriteLine("Download Complete!");

                    if (Debugger.IsAttached)
                        Console.ReadKey();

                    return 0;
                },
                errors =>
                {
                    //if the option error is not request of the help info, then display errors
                    if (!errors.Any(e => e.Tag == ErrorType.HelpRequestedError || e.Tag == ErrorType.HelpVerbRequestedError))
                    {
                        //the options had an of error
                        foreach (var error in errors)
                        {
                            Console.WriteLine(error);
                        }

                        Console.WriteLine("Invalid options specified.  Process terminated.");
                        Console.ReadKey();
                    }

                    if (Debugger.IsAttached)
                        Console.ReadKey();

                    return 0;
                }
            );
        }
    }
}
