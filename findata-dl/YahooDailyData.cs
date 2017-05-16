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
using System.Text.RegularExpressions;

namespace findata_dl
{
    public class YahooDailyData : IDataDownloader
    {
        private SemaphoreSlim semaphore;
        private YahooDailyOptions options;



        public YahooDailyData(YahooDailyOptions options)
        {
            this.options = options;
        }

        public void Start()
        {
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
        }


        public void Downloader(string symbol)
        {

            Console.WriteLine($"Downloading: {symbol}");

            //if multi-threaded, then have task wait on the semaphore for a slot to run.
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
                //The CSV reader expects the input to be a stream, so use a extension method
                //which will turn the download string into a StreamReader on a MemoryStream
                using (var newData = new CachedCsvReader(data.ToStreamReader(), true))
                {
                    ReadDataStream(newData);

                    var newDataList = newData.Records.ToList();

                    newDataList = PreprocessRecords(newDataList);

                    SaveDataStream(symbol, newDataList);
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

        /// <summary>
        /// Modify the server data to a common format for merge functions.
        /// </summary>
        /// <param name="newDataList"></param>
        private List<string[]> PreprocessRecords(List<string[]> newDataList)
        {
            //change dates to MM/dd/yyyy format
            for (int i= 0; i < newDataList.Count; i++)
            {
                newDataList[i][0] = DateUtility.ConvertDateStringFormat(newDataList[i][0], "yyyy-MM-dd", options.DateFormat);

                //remove the Yahoo adjusted close field if option on
                if (options.RemoveAdjust)
                {
                    newDataList[i] = newDataList[i].Take(6).ToArray();
                }

                YahooDailyOptions tempOptions = new YahooDailyOptions();

                //if the digits are different than the default digits, then format the digits
                if (options.RoundDecimal)
                {
                    try
                    {
                        newDataList[i] = newDataList[i].Select(x => x.Contains(".") ? Math.Round(double.Parse(x), options.RoundDigits).ToString() : x).ToArray();
                    }
                    catch { }
                    
                }
            }

            if (options.DoSort)
            {
                newDataList = newDataList.OrderBy(s => DateTime.Parse(s[0])).ToList();
            }

            return newDataList;
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
        /// <param name="newDataList"></param>
        private void SaveDataStream(string symbol, List<string[]> newDataList)
        {
            
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.DataFolder, $"{symbol}.csv");
            
            List<string[]> writeData = null;

            if (options.Merge && File.Exists(path))
            {
                writeData = MergeOldData(newDataList, path);
            }
            else
            {
                //save the new data and discard any existing data.
                writeData = newDataList
                        .OrderBy(s => DateTime.Parse(s[0]))
                        .ToList();
            }

            if (writeData != null)
            {
                //write the data to file
                File.WriteAllLines(path, writeData.Select(
                    r => DateTime.Parse(
                        r[0]).ToString(options.DateFormat + options.Separator) + string.Join(options.Separator, r.Skip(1)
                        )
                    )
                );
            }
        }

        private List<string[]> MergeOldData(List<string[]> newDataList, string path)
        {
            List<string[]> writeData;
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

            CsvDateComparer csvDateComparer = new CsvDateComparer(options.DateFormat);

            //use comparer on the first element of each record (date) to replace old records.
            writeData =
                fileData
                    .Except(newDataList, csvDateComparer)
                    .Union(newDataList)
                    .Where(s => s.Length > 0)
                    .OrderBy(s => DateTime.Parse(s[0]))
                    .ToList();
            return writeData;
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
    }
}
