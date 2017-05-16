using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading;
using CommandLine;
using LumenWorks.Framework.IO.Csv;
using System.Collections;

namespace YahooPriceDownloader
{
    public class YahooDailyHistoricalDownloader
    {
        private SemaphoreSlim semaphore;
        private Options options;


        /// <summary>
        /// Provide date comparer for the data from files or from server.  Assumes date is in first index.
        /// </summary>
        private class CsvDateComparer : IEqualityComparer<string[]>
        {

            public bool Equals(string[] x, string[] y)
            {
                return x[0] == y[0];
            }

            public int GetHashCode(string[] obj)
            {
                return obj[0].GetHashCode();
            }
        }


        public void Downloader(string symbol)
        {

            Console.WriteLine($"Downloading: {symbol}");

            //if multi-threaded, then have task wait on the semphore for a slot to run.
            if (options.Threads > 1)
            {
                semaphore.Wait();
            }

            try
            {
                var wc = new WebClient();
                var url = BuildUrl(symbol);

                //download the data for the specified date range.
                var data = wc.DownloadString(url);

                //parse the download and reverse the records to date ascending order
                //The csv reader expects the input to be a stream, so use a extension method
                //which will turn the download string into a StreamReader on a MemoryStream
                using (var csv = new CachedCsvReader(data.ToStreamReader(), true))
                {
                    ReadDataStream(csv);

                    SaveDataStream(symbol, csv.Records.ToList());
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
        }

        private Uri BuildUrl(string symbol)
        {
            //download the data for the specified date range.
            return new Uri(
                $@"https://ichart.finance.yahoo.com/table.csv"
                    + $"?d={options.EndDate.Month - 1}"
                    + $"&e={options.EndDate.Day}"
                    + $"&f={options.EndDate.Year}"
                    + $"&g=d"
                    + $"&a={options.BeginDate.Month - 1}"
                    + $"&b={options.BeginDate.Day}"
                    + $"&c={options.BeginDate.Year}"
                    + $"&ignore=.csv&s={symbol}");
        }

        /// <summary>
        /// Save the data to file.  If merge is enabled, then keep existing data file and merge new data with overwrite of duplicates.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="csv"></param>
        private void SaveDataStream(string symbol, List<string[]> csv)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.DataFolder, $"{symbol}.csv");

            List<string[]> writeData = null;

            if (options.Merge && File.Exists(path))
            {
                List<string[]> fileData = null;

                //merge the data stream into the existing data - overriding any old data.
                using (TextReader reader = File.OpenText(path))
                {
                    using (CachedCsvReader oldData = new CachedCsvReader(reader, false, ','))
                    {
                        oldData.ReadToEnd();
                        fileData = oldData.Records.ToList();
                    }
                }

                CsvDateComparer csvDateComparer = new CsvDateComparer();

                //use comparer on the first element of each record (date) to replace old records.
                writeData =
                    fileData
                        .Except(csv, csvDateComparer)
                        .Union(csv)
                        .Where(s => s.Length > 0)
                        .OrderBy(s => DateTime.Parse(s[0]))
                        .ToList();
            }
            else
            {
                //save the new data and discard any existing data.
                writeData = csv
                        .OrderBy(s => DateTime.Parse(s[0]))
                        .ToList();
            }

            if (writeData != null)
            {
                //write the data to file
                File.WriteAllLines(path, writeData.Select(r => DateTime.Parse(r[0]).ToString("MM/dd/yyy,") + string.Join(",", r.Skip(1))));
            }
        }

        private void ReadDataStream(CachedCsvReader csv)
        {
            csv.ReadToEnd();

            if (options.DoSort)
            {
                csv.Records.Reverse();
            }

            if (options.BackAdjust)
            {
                for (var i = 0; i < csv.Records.Count; i++)
                {
                    var adjustedClose = double.Parse(csv.Records[i][6]);
                    var ratio = adjustedClose / double.Parse(csv.Records[i][4]);
                    csv.Records[i][1] = (ratio * double.Parse(csv.Records[i][1])).ToString();
                    csv.Records[i][2] = (ratio * double.Parse(csv.Records[i][2])).ToString();
                    csv.Records[i][3] = (ratio * double.Parse(csv.Records[i][3])).ToString();
                    csv.Records[i][4] = (ratio * double.Parse(csv.Records[i][4])).ToString();
                }
            }
        }

        public void DoWorkSingleThreaded(List<string> symbols, Action<string> downloader)
        {
            for (var i = 0; i < symbols.Count; i++)
            {
                var symbolTemp = symbols[i];

                downloader(symbolTemp);
            }
        }

        public void DoWorkMultiThreaded(List<string> symbols, Action<string> downloader)
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

        public YahooDailyHistoricalDownloader(Options options)
        {
            this.options = options;
        }

        public void Start()
        {


            if (options.BackAdjust)
            {
                Console.Write("Yahoo back adjusting does not provide data that matches other sources.  Continue (Y/N)");

                if (!string.Equals(Console.ReadLine(), "Y", StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }
            }


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
                DoWorkSingleThreaded(symbols, Downloader);
            }
            else
            {
                semaphore = new SemaphoreSlim(options.Threads);

                DoWorkMultiThreaded(symbols, Downloader);
            }

            //done
            Console.WriteLine("Download Complete!");
            Console.ReadKey();
        }


        static void Main(string[] args)
        {
            ParserResult<Options> result;
            bool isValidOptions = true;

            //map the CommandLine to the options object
            result = CommandLine.Parser.Default.ParseArguments<Options>(args);

            ParserResult<Options> errorResult = result.WithNotParsed<Options>((IEnumerable<Error> errors) => {
                foreach (var error in errors)
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

            var options = result.MapResult((Options opts) => opts, null);

            YahooDailyHistoricalDownloader downloader = new YahooDailyHistoricalDownloader(options);
            downloader.Start();
        }
    }
}
