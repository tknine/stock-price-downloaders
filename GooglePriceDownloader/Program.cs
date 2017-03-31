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

namespace GooglePriceDownloader
{

    class Program
    {
        private static SemaphoreSlim semaphore;
        private static Options options;

        private static Action<string> yahooDownloader = (string symbol) =>
        {
            var requestCount = 0;

            Console.WriteLine($"Downloading: {symbol}");

            //if multi-threaded, then have task wait on the semphore for a slot to run.
            if (options.Threads > 1)
            {
                semaphore.Wait();
            }


            var wc = new WebClient();

            //wc.Headers.Add("Host: www.google.com");
            wc.Headers.Add("User-Agent: Mozilla/5.0 (Windows NT 10.0; WOW64; rv:52.0) Gecko/20100101 Firefox/52.0");

            wc.Headers.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            wc.Headers.Add("Accept-Language: en-US,en;q=0.5");
            wc.Headers.Add("Accept-Encoding: gzip, deflate, br");
            //wc.Headers.Add("Upgrade-Insecure-Requests: 1");
            //wc.Headers.Add("Cookie: SC=RV=:ED=us; _ga=GA1.2-2.1711606738.1490908757; SID=eQQCyqFI-YKMzKlKgXFEZkLNG6yxauW86b9mGnXwcFUbWpKH6nsNhEycHG2xN8VhmvxYKQ.; HSID=A-tD5dX2OkCu66-GP; SSID=Ajn9ybJbH9rpC2x6S; APISID=9oH6vfAg30ZCJi1F/AtUfI58QCVXAKEBDq; SAPISID=nJtm4lioCFp58MqV/AhnXQYte7AjOeikX1; NID=100=WbBDR2bmGDW9KMbd8HRdFKp6KgEi_ocmmIzYBH3Yo_gDRiNzslZ_tyo91PjvAJ7fl1azkaNTfKr5k_5FdKo6gYTf8MXoCLZ2AUP40h9pWknCZ1u6BD0IXV5Rb4ra7uXX-8xaV8goVCLmiovU1cSEOE_n8m4buEe9sFUFb6uxWC9416DPn4SlePR2O1POiqc41k1g_E22Cn-fXpoKfl8RU20nglPs5CAIKXP7Ol6CCqyTHpD85YlVfPenH9mlK0OBjKkL-BXEyQ; OGPC=5061451-23:5061821-2:; OGP=-5061451:-5061821:; S=quotestreamer=iK18ADsNdxH5pQHkMMs1ug; __gads=ID=c6482686470e3fc3:T=1490946422:S=ALNI_MbujP2YQOyr0-pt1i5jPenAjWL-ew; GOOGLE_ABUSE_EXEMPTION=ID=4293d9a89f3ffa5a:TM=1490947388:C=r:IP=2600:8801:c400:3430:4175:322a:5dd6:e0f0-:S=APGng0v2v5-MfDAtdz6Zkho9TCdHCbIeqQ");
            //wc.Headers.Add("Connection: keep-alive");


            //http://www.google.com/finance/historical?q=AAPL&startdate=01-01-1980&enddate=04-01-2017&output=csv&ei=DXvdWOndDcrUjAHOsLHACg

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
               

                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.DataFolder, $"{symbol}.csv");

                //remove any existing file as we are appending data to the file
                File.Delete(filePath);


                while (stillDownloading)
                {

                    Console.WriteLine($"...Downloading: {symbol} - {beginDate.ToShortDateString()} to {endDate.ToShortDateString()}");
                    string data = null;

                    //try
                    //{
                        //get the new data
                        data = wc.DownloadString(
                            new Uri($@"http://www.google.com/finance/historical?q={symbol}&startdate={beginDate.ToString("MM-dd-yyyy")}&enddate={endDate.ToString("MM-dd-yyyy")}&output=csv"));

                    //}
                    //catch (Exception exception)
                    //{
                    //    //get the new data
                    //    data = wc.DownloadString(
                    //        new Uri($@"http://www.google.com/finance/historical?q=NYSEMKT%3A{symbol}&startdate={beginDate.ToString("MM-dd-yyyy")}&enddate={endDate.ToString("MM-dd-yyyy")}&output=csv"));


                    //}

                    requestCount++;

                    //add the new data to the record
                    using (var csv = new CachedCsvReader(data.ToStreamReader(), true))
                    {
                        csv.ReadToEnd();

                        if (options.DoSort)
                        {
                            csv.Records.Reverse();
                        }

                        for (int i = 0; i < csv.Records.Count; i++)
                        {
                            string[] dateParts = (csv.Records[i][0]).Split("-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
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
                            csv.Records[i][0] = DateTime.Parse(string.Join("-", dateParts)).ToString("MM/dd/yyyy");
                        }

                        //output all the lines of the download using a Join of the fields sub-array to write out each line.
                        File.AppendAllLines(filePath, csv.Records.Select(r => string.Join(",", r)));
                    }

                    //move the date pointers down the history
                    beginDate = endDate.AddDays(1);
                    endDate = beginDate.AddDays(maxRequestDays);

                    //if the begin date has dropped below the request begin date, then limit the date to original date
                    if (endDate > options.EndDate)
                    {
                        endDate = options.EndDate;
                    }

                    //if the end date has dropeed below the requested begin date, we are done
                    if (beginDate > options.EndDate)
                    {
                        stillDownloading = false;
                    }
                }

                Console.WriteLine($"Completed: {symbol}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on: {symbol}{Environment.NewLine}{ex}");
                File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.DataFolder, "-- errors.log"), $"Error on: {symbol}{Environment.NewLine}{ex}{Environment.NewLine}");
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
        };

        static void Main(string[] args)
        {
            ParserResult<Options> result;
            bool isValidOptions = true;

            //map the CommandLine to the options object
            result = CommandLine.Parser.Default.ParseArguments<Options>(args);

            ParserResult<Options> errorResult = result.WithNotParsed<Options>((IEnumerable<Error> errors) =>
            {
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
                //semaphore = new SemaphoreSlim(options.Threads);

                //DoWorkMultiThreaded(symbols, yahooDownloader);

                Console.WriteLine("Google data cannot be dowwnloaded with multiple threads as it will quickly overcome the request rate limit and cause ip lockout.");
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
