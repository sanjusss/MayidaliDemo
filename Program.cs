using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;

namespace MayidaliDemo
{
    class Program
    {
        static void Main(string[] _)
        {
            using (HttpClient client = new HttpClient(new MayidaliHttpClientHandler()))
            {
                TimeSpan interval = new TimeSpan(0, 0, 1);
                int exceptionCount = 0;
                int count = 0;
                while (true)
                {
                    DateTime start = DateTime.Now;
                    try
                    {
                        string info = client.GetStringAsync("https://myip.ipip.net").Result;
                        string reg = @"^当前\sIP：(\S*)\s*来自于：(.*)\s{2}(.*)";
                        var match = Regex.Match(info, reg);
                        ProxyList.AddProxyInfo(match.Groups[1].Value,
                                               match.Groups[2].Value,
                                               match.Groups[3].Value);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"第{ ++exceptionCount }次异常：{ e }");
                    }
                    finally
                    {
                        ++count;
                        if (count % 100 == 0)
                        {
                            Console.WriteLine($"结束第{ count }次调用。");
                        }

                        var used = DateTime.Now - start;
                        if (used < interval)
                        {
                            Thread.Sleep(interval - used);
                        }
                    }
                }
            }
        }
    }
}
