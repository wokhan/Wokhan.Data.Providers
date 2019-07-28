using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using Wokhan.Collections.Extensions;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers
{
    [DataProvider(Category = "Database", IsDirectlyBindable = true, Name = "LDAP Data Provider")]
    public class LdapDataProvider : DataProvider, IExposedDataProvider
    {
        public string Uri { get; set; }
        public string UserDn { get; set; }
        public string Password { get; set; }
        public string RootDn { get; set; }
        //public string GroupsRootDn { get; set; }
        public string Query { get; set; }
        public string[] Attributes { get; set; }
        public bool UseSSL { get; set; }
        public bool UseServerBind { get; set; }
        public bool UseCache { get; set; } = false;

        protected override IQueryable<T> GetTypedData<T, TK>(string repository, IEnumerable<string> attributes, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null)
        {
            string qry = updateValue(this.Query, values);
            string userdn = updateValue(this.UserDn, values);
            string passwd = updateValue(this.Password, values);

            var authOptions = (this.UseSSL ? AuthenticationTypes.SecureSocketsLayer : AuthenticationTypes.None) | (this.UseServerBind ? AuthenticationTypes.ServerBind : AuthenticationTypes.None);

            using (var root = new DirectoryEntry(Uri + "/" + RootDn, userdn, passwd, authOptions))
            {
                using (var ds = new DirectorySearcher(root, qry, Attributes ?? new string[0]))
                {
                    var sw = Stopwatch.StartNew();

                    ds.SearchScope = SearchScope.Subtree;
                    ds.CacheResults = UseCache;
                    ds.Asynchronous = false;

                    var ret = ds.FindAll()
                                .Cast<SearchResult>()
                                .Select(c => attributes.Select(a => c.Properties[a].Cast<object>().FirstOrDefault()).ToArray())
                                .Select(x => x.ToObject<T>(attributes.ToArray()))
                                .AsQueryable();
                    sw.Stop();

                    statisticsBag?.Add("FindAll", sw.ElapsedMilliseconds);

                    return ret;
                }
            }
        }
    }
}
