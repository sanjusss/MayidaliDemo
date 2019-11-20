using System;
using System.Net.Http;

namespace MayidaliDemo
{
    class Program
    {
        static void Main(string[] _)
        {
            using (HttpClient client = new HttpClient(new MayidaliHttpClientHandler()))
            {
                string info = client.GetStringAsync("https://myip.ipip.net").Result;
                Console.WriteLine(info);
            }
        }
    }
}
