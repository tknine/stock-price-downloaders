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

namespace findata_dl
{

    public class GoogleDailyData : IDataDownloader
    {
        private SemaphoreSlim semaphore;
        private GoogleDailyOptions options;

        public GoogleDailyData(GoogleDailyOptions options)
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
                //Google downloading with multiple threads will fail as it will cause a IP lockout due to requests exceeding rate limits.

                //semaphore = new SemaphoreSlim(options.Threads);

                //DoWorkMultiThreaded(symbols, yahooDownloader);

                Console.WriteLine("Google downloading with multiple threads will fail as it will cause a IP lockout due to requests exceeding rate limits.");
            }
        }


        private void Downloader(string symbol)
        {
            var requestCount = 0;

            Console.WriteLine($"Downloading: {symbol}");

            //if multi-threaded, then have task wait on the semaphore for a slot to run.
            if (options.Threads > 1)
            {
                semaphore.Wait();
            }

            try
            {

                var endDate = options.EndDate;
                var beginDate = options.BeginDate;
                var dataSpan = endDate.Subtract(beginDate);

                var maxRequestDays = 15 * 365; //only download a limited max years per loop request

                if (dataSpan.TotalDays > maxRequestDays)
                {
                    endDate = beginDate.AddDays(maxRequestDays);
                }

                var stillDownloading = true;

                //bucket to hold all the records until all read looping is complete
                var newDataList = new List<string[]>();

                //loop through date ranges as only a certain number of days can be requested at a time.
                while (stillDownloading)
                {
                    requestCount++;

                    Console.WriteLine($"...Downloading: {symbol} - {beginDate.ToShortDateString()} to {endDate.ToShortDateString()}");

                    var url = BuildUrl(symbol, endDate, beginDate);

                    string data = DownloadCsv(url);

                    //add the new data to the record
                    using (var newData = new CachedCsvReader(data.ToStreamReader(), true))
                    {
                        newDataList = ReadStreamData(newDataList, newData);
                    }

                    //move the date pointers down the history
                    beginDate = endDate.AddDays(1);
                    endDate = beginDate.AddDays(maxRequestDays);

                    //if the begin date has dropped below the request begin date, then limit the date to original date
                    if (endDate > options.EndDate)
                    {
                        endDate = options.EndDate;
                    }

                    //if the end date has dropped below the requested begin date, we are done
                    if (beginDate > options.EndDate)
                    {
                        stillDownloading = false;
                    }
                }

                newDataList = PreprocessRecords(newDataList);

                SaveDataStream(symbol, newDataList);

                Console.WriteLine($"Completed: {symbol}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on: {symbol}{Environment.NewLine}{ex}");
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.DataFolder, options.LogFile), $"Error on: {symbol}{Environment.NewLine}{ex}{Environment.NewLine}");
            }
            finally
            {
                Console.WriteLine($"...Pausing {(1100 * requestCount) / 1000} seconds to prevent Google request lockout.");
                Thread.Sleep(1100 * requestCount);

                if (options.Threads > 1)
                {
                    //if multi-threaded, then release the semphore so that next task can run.
                    semaphore.Release();
                }
            }
        }

        private string DownloadCsv(Uri url)
        {
            var wc = new WebClient();

            //wc.Headers.Add("Host: www.google.com");
            wc.Headers.Add("User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0");

            wc.Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            wc.Headers.Add("Accept-Language: en-US,en;q=0.5");
            wc.Headers.Add("Accept-Encoding: gzip, deflate, br");

            //get the new data
            string data = wc.DownloadString(url);
            return data;
        }

        private List<string[]> ReadStreamData(List<string[]> newDataList, CachedCsvReader newData)
        {
            newData.ReadToEnd();

            //add new set of records to the record builder
            newDataList = newDataList.Union(newData.Records).ToList();
            return newDataList;
        }

        private List<string[]> PreprocessRecords(List<string[]> newDataList)
        {
            for (int i = 0; i < newDataList.Count; i++)
            {
                //parse google date to option date format
                string[] dateParts = (newDataList[i][0]).Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                var year = int.Parse(dateParts[2]);

                if (year < 70)
                {
                    year += 2000;
                }
                else
                {
                    year += 1900;
                }

                dateParts[2] = year.ToString();
                newDataList[i][0] = DateTime.Parse(string.Join("-", dateParts)).ToString(options.DateFormat);
            }

            if (options.DoSort)
            {
                newDataList = newDataList.OrderBy(s => DateTime.Parse(s[0])).ToList();
            }

            return newDataList;
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

        private Uri BuildUrl(string symbol, DateTime endDate, DateTime beginDate)
        {
            //Format: http://www.google.com/finance/historical?q=AAPL&startdate=01-01-1980&enddate=04-01-2017&output=csv

            return new Uri($@"http://www.google.com/finance/historical?q={symbol}&startdate={beginDate.ToString("MM-dd-yyyy")}&enddate={endDate.ToString("MM-dd-yyyy")}&output=csv");
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
