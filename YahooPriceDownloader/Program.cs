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

namespace YahooPriceDownloader
{
    class Program
    {
        private static SemaphoreSlim semaphore;

        static void Main(string[] args)
        {
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));

            semaphore = new SemaphoreSlim(3);

            Action<string> a = (string symbol) =>
            {

                semaphore.Wait();

                Console.WriteLine($"Downloading: {symbol}");

                var wc = new WebClient();


                try
                {
                    var data = wc.DownloadString(new Uri($@"https://ichart.finance.yahoo.com/table.csv?d=3&e=31&f=2017&g=d&a=1&b=31&c=2016&ignore=.csv&s={symbol}"));
                
                    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"{symbol}.csv");

                    Console.WriteLine($"Completed: {symbol}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.ToString()}: {symbol}");
                }

                semaphore.Release(1);
            };

            List<Action> actions = new List<Action>();

            List<string> symbols = "AAPL NUGT DUST CSCO IBM C NFLX FSLR".Split(" ".ToArray(), StringSplitOptions.RemoveEmptyEntries).ToList();

            DoWork(symbols, a);

            Console.ReadKey();
        }

        public static void DoWork(List<string> symbols, Action<string> downloader)
        {

            Task[] tasks = new Task[symbols.Count];

            for (var i= 0; i < symbols.Count; i++)
            {
                var symbolTemp = symbols[i];

                tasks[i] = (Task.Run(() =>
                {
                    Console.WriteLine($"Downloading: {symbolTemp}");

                    semaphore.Wait();

                    

                    var wc = new WebClient();


                    try
                    {
                        wc.DownloadFile(
                        new Uri($@"https://ichart.finance.yahoo.com/table.csv?d=3&e=1&f=2017&g=d&a=1&b=1&c=2016&ignore=.csv&s={symbolTemp}"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"{symbolTemp}.csv"));

                        Console.WriteLine($"Completed: {symbolTemp}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex}: {symbolTemp}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }

            semaphore.Release(2);
            
            //semaphore.Release(3);

            Task.WaitAll(tasks);
        }


    }
}
