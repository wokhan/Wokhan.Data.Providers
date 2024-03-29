﻿using Newtonsoft.Json;

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

namespace Wokhan.Data.Providers.Embedded
{
    /// <summary>
    /// WIP: A simple provider to query a HTTP URI and retrieve metadata and body.
    /// Only works for JSON and XML responses as of now.
    /// </summary>
    [DataProvider(Category = "Database", IsDirectlyBindable = true, Name = "Http Data Provider", Copyright = "Developed by Wokhan Solutions", Icon = "Resources/Providers/web-box.png")]
    public class HttpDataProvider : AbstractDataProvider, IExposedDataProvider
    {
        /// <summary>
        /// Target URL
        /// </summary>
        [ProviderParameter("Target URL")]
        public string Url { get; set; }

        /// <summary>
        /// Timmeout
        /// </summary>
        [ProviderParameter("Timeout")]
        public int Timeout { get; set; } = 20000;

        /// <summary>
        /// HTTP verb to use for this query
        /// </summary>
        [ProviderParameter("HTTP method")]
        public string Method { get; set; } = "GET";

        /// <summary>
        /// Content-type for this query
        /// </summary>
        [ProviderParameter("Content type")]
        public string ContentType { get; set; }

        /// <summary>
        /// Request body (for POST only)
        /// </summary>
        [ProviderParameter("Body (for POST only)")]
        public string? Body { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [ProviderParameter("Headers")]
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Proxy URL (optional)
        /// </summary>
        [ProviderParameter("URL", Category = "Proxy")]
        public string? Proxy { get; set; }

        /// <summary>
        /// Proxy user name (for authentified proxies)
        /// </summary>
        [ProviderParameter("Username", Category = "Proxy")]
        public string? ProxyUsername { get; set; }

        /// <summary>
        /// Proxy password (for authentified proxies)
        /// </summary>
        [ProviderParameter("Password", Category = "Proxy")]
        public string? ProxyPassword { get; set; }

        // TODO: implement
        public override List<ColumnDescription> GetColumns(string? repository, IList<string>? names = null)
        {
            throw new NotImplementedException();
        }

        // TODO: implement
        public override void InvalidateColumnsCache(string repository)
        {
            throw new NotImplementedException();
        }

        // TODO: implement
        public override bool Test(out string details)
        {
            throw new NotImplementedException();
        }

        public override IQueryable<T> GetQueryable<T>(string? repository, IList<Dictionary<string, string>>? values = null, Dictionary<string, long>? statisticsBag = null)
        {
            var sw = new Stopwatch();

            string proxy = null;
            string localurl = UpdateValue(Url, values);
            string localbody = UpdateValue(Body, values);

            var req = (HttpWebRequest)WebRequest.Create(localurl);
            req.ServicePoint.ConnectionLimit = 100;
            req.Timeout = Timeout;
            req.Accept = "*/*";
            //req.KeepAlive = false;
            if (Proxy != null)
            {
                var prx = new WebProxy(Proxy);
                if (ProxyUsername != null && ProxyPassword != null)
                {
                    prx.UseDefaultCredentials = false;
                    prx.Credentials = new NetworkCredential(ProxyUsername, ProxyPassword);
                }
                req.Proxy = prx;
            }
            proxy = req.Proxy.GetProxy(req.RequestUri).ToString();
            req.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);

            if (ContentType != null)
            {
                req.ContentType = ContentType;
            }
            Headers?.ToList().ForEach(h => req.Headers.Add(h.Key, h.Value));
            req.Method = Method;

            if (Body != null)
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
