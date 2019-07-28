using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Xml.Serialization;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers.HttpDataProvider
{
    [DataProvider(Category = "Database", IsDirectlyBindable = true, Name = "Http Data Provider")]
    public class HttpDataProvider : DataProvider, IExposedDataProvider
    {
        public string Url { get; set; }
        public string Proxy { get; set; }
        public NetworkCredential ProxyCredentials { get; set; }
        public string ContentType { get; set; }
        public string Method { get; set; }
        public string Body { get; set; }
        public int Timeout { get; set; } = 20000;
        public Dictionary<string, string> Headers { get; set; }

        protected override IQueryable<T> GetTypedData<T, TK>(string repository, IEnumerable<string> attributes, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null) where T : class
        {
            var sw = new Stopwatch();

            string proxy = null;
            string localurl = updateValue(this.Url, values);
            string localbody = updateValue(this.Body, values);

            var req = (HttpWebRequest)WebRequest.Create(localurl);
            req.ServicePoint.ConnectionLimit = 100;
            req.Timeout = Timeout;
            req.Accept = "*/*";
            //req.KeepAlive = false;
            if (Proxy != null)
            {
                var prx = new WebProxy(Proxy);
                if (ProxyCredentials != null)
                {
                    prx.UseDefaultCredentials = false;
                    prx.Credentials = ProxyCredentials;
                }
                req.Proxy = prx;
            }
            proxy = req.Proxy.GetProxy(req.RequestUri).ToString();
            req.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

            if (this.ContentType != null)
            {
                req.ContentType = this.ContentType;
            }
            this.Headers?.ToList().ForEach(h => req.Headers.Add(h.Key, h.Value));
            req.Method = this.Method ?? "GET";

            if (this.Body != null)
            {
                sw.Restart();

                var bytes = Encoding.UTF8.GetBytes(localbody);
                req.ContentLength = bytes.Length;

                using (var vw = new BinaryWriter(req.GetRequestStream()))
                {
                    vw.Write(bytes);
                }

                sw.Stop();
                statisticsBag?.Add("WriteBody", sw.ElapsedMilliseconds);
            }


            sw.Restart();

            var wr = (HttpWebResponse)req.GetResponse();

            sw.Stop();
            statisticsBag?.Add("GetResponse", sw.ElapsedMilliseconds);

            sw.Restart();

            var stream = wr.GetResponseStream();

            var data = default(T[]);
            switch (wr.ContentType)
            {
                case "application/json":
                    using (var reader = new JsonTextReader(new StreamReader(stream)))
                        data = JsonSerializer.Create().Deserialize<T[]>(reader);
                    break;

                case "application/xml":
                    data = (T[])new XmlSerializer(typeof(T[])).Deserialize(stream);
                    break;
            }

            sw.Stop();
            statisticsBag?.Add("HandleResponse", sw.ElapsedMilliseconds);
            
            return data.AsQueryable();
        }
    }
}
