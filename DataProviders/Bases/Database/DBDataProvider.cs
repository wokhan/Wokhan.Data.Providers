using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases.Database;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers.Bases
{
    public abstract class DBDataProvider : DataProvider, IDBDataProvider
    {
        [ProviderParameter("Connection String", ExclusionGroup = "Connection string", Position = 0)]
        public string ConnectionString { get; set; }

        [ProviderParameter("Schema (optional)", ExclusionGroup = "Connection details", Position = 30)]
        public string Schema { get; set; }

        [ProviderParameter("Host", ExclusionGroup = "Connection details", Position = 10)]
        public new string Host { get; set; }

        [ProviderParameter("Port", ExclusionGroup = "Connection details", Position = 20)]
        public int Port { get; set; }

        [ProviderParameter("Username", ExclusionGroup = "Connection details", Position = 40)]
        public string Username { get; set; }

        [ProviderParameter("Password", true, ExclusionGroup = "Connection details", Position = 50)]
        public string Password { get; set; }

        [ProviderParameter("Globally excluded columns", true, ExclusionGroup = "Advanced", Position = 60)]
        public string[] HiddenFields { get; set; }

        public Dictionary<string, string> TODOColumnsRelAttributes { get; private set; }
        public Dictionary<string, string> TODOColumnsHidden { get; private set; }
        public List<string> TODORepositoryRel { get; private set; }
        public List<string> TODORelationSoftLink { get; private set; }
        public List<string> TODORelationCartesian { get; private set; }


        static DBDataProvider()
        {
            DbInterception.Add(new DbInterceptor());
        }

        public abstract DbDataAdapter DataAdapterInstancer();

        public abstract DbConnection GetConnection();

        public static new string GetFormatKey(List<object> srcAttributesCollection, object srcAttribute)
        {
            return ((DataColumn)srcAttribute).ColumnName;
        }

        /*public new IEnumerable GetDataDirect(string repository = null, IEnumerable<string> attributes = null)
        {
            var tmp = new DataTable();
            if (Repositories.ContainsKey(repository))
            {
                using (var oda = ((IDBDataProvider)this).DataAdapterInstancer())
                {
                    if (attributes == null || !attributes.Any())
                    {
                        oda.SelectCommand.CommandText = (string)Repositories[repository];
                    }
                    else
                    {
                        oda.SelectCommand.CommandText = "SELECT " + String.Join(", ", attributes) + " FROM (" + Repositories[repository] + ") a";
                    }

                    tmp.BeginLoadData();
                    oda.Fill(tmp);
                    tmp.EndLoadData();
                }
            }
            return tmp.DefaultView;
        }*/

        //public new Dictionary<string, Type> GetColumns(string repository, IList<string> names = null)
        //{
        //    using (var oda = ((IDBDataProvider)this).DataAdapterInstancer())
        //    {
        //        DataTable tmp = new DataTable();
        //        oda.SelectCommand.CommandText = "SELECT * FROM " + repository + " WHERE ROWNUM = 1";
        //        oda.FillSchema(tmp, SchemaType.Source);

        //        return tmp.Columns.Cast<DataColumn>().ToDictionary(c => c.ColumnName, c => c.DataType);
        //    }
        //}

        public Dictionary<string, string[]> CachedKeys { get; set; } = new Dictionary<string, string[]>();

        private Dictionary<string, List<ColumnDescription>> _headers = new Dictionary<string, List<ColumnDescription>>();
        //public Dictionary<string, Dictionary<string, string>> CachedHeaders
        //{
        //    get { return _headers.ToDictionary(h => h.Key, h => h.Value.ToDictionary(k => k.Key, k => k.Value.FullName)); }
        //    set { _headers = value.ToDictionary(h => h.Key, h => h.Value.ToDictionary(k => k.Key, k => Type.GetType(k.Value))); }
        //}

        public new void RemoveCachedHeaders(string repository)
        {
            //if (_headers.ContainsKey(this.Name + "_" + repository))
            //{
            //    _headers.Remove(this.Name + "_" + repository);
            //}
        }

        public new List<ColumnDescription> GetColumns(string repository, IList<string> names = null)
        {
            List<ColumnDescription> ret;

            lock (_headers)
            {
                if (!_headers.TryGetValue(this.Name + "_" + repository, out ret))
                {
                    var tmp = new DataTable();
                    if (Repositories.ContainsKey(repository))
                    {
                        using (var oda = ((IDBDataProvider)this).DataAdapterInstancer())
                        {
                            oda.SelectCommand.CommandText = (string)Repositories[repository];
                            oda.FillSchema(tmp, SchemaType.Source);
                        }
                    }

                    ret = tmp.Columns.Cast<DataColumn>().Select(c => new ColumnDescription { Name = c.ColumnName, Type = GetRealType(c) }).ToList();
                    _headers.Add(this.Name + "_" + repository, ret);
                }
            }

            return ret;
        }

        public Type GetRealType(DataColumn c)
        {
            if (c.AllowDBNull && c.DataType.IsValueType)
            {
                return typeof(Nullable<>).MakeGenericType(c.DataType);
            }
            else
            {
                return c.DataType;
            }
        }

        Dictionary<string, Type> cachedTypes = new Dictionary<string, Type>();
        public new IQueryable<T> GetTypedData<T, TK>(string repository, IEnumerable<string> attributes) where T : class
        {

            Type tx;
            lock (cachedTypes)
            {
                if (!cachedTypes.TryGetValue(repository, out tx))
                {
                    tx = typeof(DynamicDbContext<,>).MakeGenericType(typeof(T), typeof(TK));
                    cachedTypes.Add(repository, tx);
                }
            }

            var conn = ((IDBDataProvider)this).GetConnection();

            var h = GetColumns(repository).Select(hd => hd.Name).ToArray();
            var dbc = (IDynamicDbContext)Activator.CreateInstance(tx, conn, "DYNAMICSCHEMA", repository, h);

            dbc.basequery = (string)Repositories[repository];

            return (IQueryable<T>)dbc.GetSet().AsNoTracking();//.AsStreaming();
        }



        //[DbConfigurationType("Wokhan.Data.Providers.DBDataProvider.DbConfigurationWrapper")]

        //public new IEnumerable<object[]> GetData(string repository, IEnumerable<string> attributes = null)
        //{
        //    DataTable ret = new DataTable();

        //    try
        //    {
        //        if (Repositories.ContainsKey(repository))
        //        {
        //            using (var oda = ((IDBDataProvider)this).DataAdapterInstancer())
        //            {
        //                var cn = oda.SelectCommand.Connection;
        //                {
        //                    if (attributes == null || !attributes.Any())
        //                    {
        //                        oda.SelectCommand.CommandText = (string)Repositories[repository];
        //                    }
        //                    else
        //                    {
        //                        oda.SelectCommand.CommandText = "SELECT " + String.Join(", ", attributes) + " FROM (" + Repositories[repository] + ") a";
        //                    }

        //                    cn.Open();
        //                    var sdr = oda.SelectCommand.ExecuteReader(CommandBehavior.CloseConnection);
        //                    return innerGetData(sdr);
        //                }
        //            }
        //        }
        //        else
        //        {
        //            throw new Exception("Unknown source");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw e;
        //    }
        //}

        protected override long Count(string repository = null)
        {
            try
            {
                if (Repositories.ContainsKey(repository))
                {
                    using (var oda = ((IDBDataProvider)this).DataAdapterInstancer())
                    {
                        var cn = oda.SelectCommand.Connection;
                        oda.SelectCommand.CommandText = "SELECT COUNT(*) FROM (" + (string)Repositories[repository] + ") a";

                        cn.Open();
                        var res = oda.SelectCommand.ExecuteScalar();
                        var ret = (long)((decimal)res);
                        cn.Close();

                        return ret;
                    }
                }
                else
                {
                    throw new Exception("Unknown source");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        //private IEnumerable<object[]> innerGetData(DbDataReader sdr)
        //{
        //    while (sdr.Read())
        //    {
        //        var ot = new object[sdr.FieldCount];

        //        sdr.GetValues(ot);

        //        yield return ot;
        //    }

        //    sdr.Close();
        //}


        public new bool Test(out string details)
        {
            try
            {
                using (var oda = ((IDBDataProvider)this).DataAdapterInstancer())
                {
                    var cn = oda.SelectCommand.Connection;
                    oda.SelectCommand.CommandText = "SELECT 1 FROM DUAL";

                    cn.Open();
                    oda.SelectCommand.ExecuteScalar();
                    cn.Close();
                }

                details = "OK";
                return true;
            }
            catch (Exception e)
            {
                details = e.Message;
                return false;
            }
        }
    }
}
