using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MayidaliDemo
{
    /// <summary>
    /// 自动获取环境变量中关于蚂蚁代理的参数，初始化HttpClient。
    /// 环境变量定义：
    /// MYDL_PROXY 代理地址。
    /// MYDL_APP_SECRET app secret。
    /// MYDL_APP_KEY app_key。
    /// 其他参数请以 MYDL_PARAM_ 开头，全部大写，“-”替换为“_”。
    /// </summary>
    public class MayidaliHttpClientHandler : HttpClientHandler
    {
        private const string _paramsPrefix = "MYDL_PARAM_";
        private static readonly WebProxy _proxy;
        private static readonly string _appSecret;
        private static readonly IReadOnlyDictionary<string, string> _params;

        static MayidaliHttpClientHandler()
        {
            //获取代理参数
            string proxyUrl = GetEnvironmentVariable("MYDL_PROXY");
            if (proxyUrl.StartsWith("http://") == false)
            {
                proxyUrl = "http://" + proxyUrl;
            }

            _proxy = new WebProxy(proxyUrl);
            _appSecret = GetEnvironmentVariable("MYDL_APP_SECRET");

            var vars = GetPrefixedEnvironmentVariables();
            vars.Add("app_key", GetEnvironmentVariable("MYDL_APP_KEY"));
            _params = vars;
        }

        public MayidaliHttpClientHandler()
        {
            Proxy = _proxy;
            ServerCertificateCustomValidationCallback = (message, cert, chain, error) => true;//忽略HTTPS请求时，由于代理产生的证书错误。
        }

        /// <summary>
        /// 预处理HTTP请求。
        /// </summary>
        /// <param name="request">HTTP请求消息。</param>
        /// <param name="cancellationToken">用于取消操作的取消标记。</param>
        /// <returns>表示异步操作的任务对象。</returns>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string auth = GetProxyAuthorization();
            request.Headers.Add("Proxy-Authorization", auth);
            return base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// 获取环境变量。
        /// </summary>
        /// <param name="key">环境变量的名称。</param>
        /// <param name="exception">没有值时是否会抛出异常。默认为true。</param>
        /// <exception cref="ArgumentNullException" />
        /// <returns>环境变量的值。</returns>
        private static string GetEnvironmentVariable(string key, bool exception = true)
        {
            string value = Environment.GetEnvironmentVariable(key);
            if (exception && value == null)
            {
                throw new ArgumentNullException(key);
            }

            return value;
        }

        /// <summary>
        /// 获取所有 _paramsPrefix 开头的环境变量。
        /// </summary>
        /// <returns>所有 _paramsPrefix 开头的环境变量的集合。</returns>
        private static SortedList<string, string> GetPrefixedEnvironmentVariables()
        {
            SortedList<string, string> vars = new SortedList<string, string>();
            foreach (DictionaryEntry i in Environment.GetEnvironmentVariables())
            {
                if (!(i.Key is string key) ||
                    !key.StartsWith(_paramsPrefix)||
                    !(i.Value is string value))
                {
                    continue;
                }

                vars.Add(key.Remove(0, _paramsPrefix.Length).ToLower().Replace('_', '-'),
                         value);
            }

            return vars;
        }

        /// <summary>
        /// 获取当前东八区时间字符串。
        /// </summary>
        /// <returns>当前东八区时间字符串</returns>
        private static string GetTimestampString()
        {
            return DateTime.UtcNow.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 获取除签名外，所有请求代理的参数。
        /// </summary>
        /// <returns>除签名外，所有请求代理的参数。</returns>
        private static SortedList<string, string> GetParams()
        {
            SortedList<string, string> vars = new SortedList<string, string>();
            foreach (var i in _params)
            {
                vars.Add(i.Key, i.Value);
            }

            vars.Add("timestamp", GetTimestampString());
            return vars;
        }

        /// <summary>
        /// 获取签名。
        /// </summary>
        /// <param name="vars">请求代理的参数。</param>
        /// <returns>签名。</returns>
        private static string GetSign(SortedList<string, string> vars)
        {
            StringBuilder builder = new StringBuilder(_appSecret);
            foreach (var i in vars)
            {
                builder.Append(i.Key);
                builder.Append(i.Value);
            }

            builder.Append(_appSecret);
            string src = builder.ToString();
            return CreateMD5(src);
        }

        /// <summary>
        /// 生成MD5值。
        /// </summary>
        /// <param name="src">源字符串。</param>
        /// <returns>MD5值。</returns>
        private static string CreateMD5(string src)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(src));
                StringBuilder builder = new StringBuilder();
                foreach (var i in data)
                {
                    builder.Append(i.ToString("X2"));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// 获取代理验证字符串。
        /// </summary>
        /// <returns>代理验证字符串。</returns>
        private static string GetProxyAuthorization()
        {
            var vars = GetParams();
            string sign = GetSign(vars);
            vars.Add("sign", sign);
            StringBuilder builder = new StringBuilder("MYH-AUTH-MD5 ");
            for (int i = 0; i < vars.Count; ++i)
            {
                if (i != 0)
                {
                    builder.Append('&');
                }

                builder.Append(vars.Keys[i]);
                builder.Append('=');
                builder.Append(vars.Values[i]);
            }

            return builder.ToString();
        }
    }
}
