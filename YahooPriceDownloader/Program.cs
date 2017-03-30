using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using MoreLinq;
using System.Threading;
using CommandLine;
using LumenWorks.Framework.IO.Csv;

namespace YahooPriceDownloader
{
    class Program
    {
        private static SemaphoreSlim semaphore;
        private static Options options;

        private static Action<string> yahooDownloader = (string symbol) =>
        {

            Console.WriteLine($"Downloading: {symbol}");

            //if multi-threaded, then have task wait on the semphore for a slot to run.
            if (options.Threads > 1)
            {
                semaphore.Wait();
            }
            

            var wc = new WebClient();

            try
            {
                //download the data for the specified date range.
                var data = wc.DownloadString(new Uri($@"https://ichart.finance.yahoo.com/table.csv"
                    + $"?d={options.EndDate.Month - 1}"
                    + $"&e={options.EndDate.Day}"
                    + $"&f={options.EndDate.Year}"
                    + $"&g=d"
                    + $"&a={options.BeginDate.Month - 1}"
                    + $"&b={options.BeginDate.Day}"
                    + $"&c={options.BeginDate.Year}"
                    + $"&ignore=.csv&s={symbol}"));

                //parse the download and reverse the records to date ascending order
                //The csv reader expects the input to be a stream, so use a extension method
                //which will turn the download string into a StreamReader on a MemoryStream
                using (var csv = new CachedCsvReader(data.ToStreamReader(), true))
                {
                    csv.ReadToEnd();

                    csv.Records.Reverse();

                    //output all the lines of the download using a Join of the fields sub-array to write out each line.
                    File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.DataFolder, $"{symbol}.csv"), csv.Records.Select(r => string.Join(",", r)));
                }

                Console.WriteLine($"Completed: {symbol}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on: {symbol}{Environment.NewLine}{ex}");
            }
            finally
            {
                if (options.Threads > 1)
                {
                    //if multi-threaded, then release the semphore so that next task can run.
                    semaphore.Release();
                }
            }
        };

        static void Main(string[] args)
        {
            ParserResult<Options> result;
            bool isValidOptions = true;

            //map the CommandLine to the options object
            result = CommandLine.Parser.Default.ParseArguments<Options>(args);

            ParserResult<Options>  errorResult = result.WithNotParsed<Options>((IEnumerable<Error> errors) => {
                foreach(var error in errors)
                {
                    Console.WriteLine(error);
                }
                isValidOptions = false;
            });

            //there were errors in the options so exit
            if (!isValidOptions)
            {
                Console.WriteLine("Invalid options specified.  Process terminated.");
                Console.ReadKey();
                return;
            }

            options = result.MapResult((Options opts) => opts, null);
            

            //create the data directory
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.DataFolder));

            //get the symbols to download
            List<string> symbols = new List<string>();

            if (options.SymbolFile != null)
            {
                symbols = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.SymbolFile)).ToList();
            }
            else
            {
                symbols = options.Symbol.Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            //start the download with either a a single thread (current thread) or multi-threaded
            if (options.Threads == 1)
            {
                DoWorkSingleThreaded(symbols, yahooDownloader);
            }
            else
            {
                semaphore = new SemaphoreSlim(options.Threads);

                DoWorkMultiThreaded(symbols, yahooDownloader);
            }

            //done
            Console.WriteLine("Download Complete!");
            Console.ReadKey();
        }

        public static void DoWorkSingleThreaded(List<string> symbols, Action<string> downloader)
        {

            for (var i = 0; i < symbols.Count; i++)
            {
                var symbolTemp = symbols[i];

                downloader(symbolTemp);
            }
        }

        public static void DoWorkMultiThreaded(List<string> symbols, Action<string> downloader)
        {

            Task[] tasks = new Task[symbols.Count];

            for (var i = 0; i < symbols.Count; i++)
            {
                var symbolTemp = symbols[i];

                tasks[i] = Task.Run(() => downloader(symbolTemp));
            }

            semaphore.Release(options.Threads);
            
            //semaphore.Release(3);

            Task.WaitAll(tasks);
        }

    }
}
