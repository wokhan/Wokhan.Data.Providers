using System;
using System.Collections.Generic;
using System.Data;
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
    public class LdapDataProvider : AbstractDataProvider, IExposedDataProvider
    {
        public string Uri { get; set; }
        public string UserDn { get; set; }
        public string Password { get; set; }
        public string RootDn { get; set; }
        //public string GroupsRootDn { get; set; }
        public string Query { get; set; }

        private string[] _attributes = new[] { "dn" };

        public string[] Attributes { get => _attributes;
            set
            {
                _attributes = value;
                _columns = value.Select(a => new ColumnDescription() { Name = a, DisplayName = a, Description = a }).ToList();
            }
        }
        public bool UseSSL { get; set; }
        public bool UseServerBind { get; set; }
        public bool UseCache { get; set; } = false;

        private List<ColumnDescription> _columns;
        public override List<ColumnDescription> GetColumns(string repository, IList<string> names = null)
        {
            return _columns;
        }

        public override bool AllowCustomRepository => false;

        public override void InvalidateColumnsCache(string repository)
        {
            throw new NotImplementedException();
        }

        public override bool Test(out string details)
        {
            throw new NotImplementedException();
        }

        public override IQueryable<T> GetQueryable<T>(string repository, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null)
        {
            string qry = UpdateValue(this.Query, values);
            string userdn = UpdateValue(this.UserDn, values);
            string passwd = UpdateValue(this.Password, values);

            var authOptions = (this.UseSSL ? AuthenticationTypes.SecureSocketsLayer : AuthenticationTypes.None) | (this.UseServerBind ? AuthenticationTypes.ServerBind : AuthenticationTypes.None);

            using (var root = new DirectoryEntry(Uri + "/" + RootDn, userdn, passwd, authOptions))
            {
                using (var ds = new DirectorySearcher(root, qry, Attributes))
                {
                    var sw = Stopwatch.StartNew();

                    ds.SearchScope = SearchScope.Subtree;
                    ds.CacheResults = UseCache;
                    ds.Asynchronous = false;

                    var ret = ds.FindAll()
                                .Cast<SearchResult>()
                                .Select(c => Attributes.Select(a => c.Properties[a].Cast<object>().FirstOrDefault()).ToArray())
                                .Select(x => x.ToObject<T>(Attributes))
                                .AsQueryable();
                    sw.Stop();

                    statisticsBag?.Add("FindAll", sw.ElapsedMilliseconds);

                    return ret;
                }
            }
        }
    }
}
