using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers.Bases
{
    [DataContract]
    public abstract class DataProvider : IDataProvider
    {
        //public IEnumerable GetDataDirect(string repository = null, IEnumerable<string> attributes = null)
        //{
        //    return null;
        //}

        //private IQueryable<dynamic> GetDataRecursive(KeyValuePair<string, SearchOptions> init, Dictionary<string, SearchOptions> searchRep)
        //{
        //    var query = GetData(init.Key, init.Value.Attributes);
        //    foreach (var rel in init.Value.Relations)
        //    {
        //        if (searchRep.TryGetValue(rel.Target, out var next))
        //        {
        //            query = query.Join(GetDataRecursive(rel, searchRep), rel.)
        //        }
        //        else
        //        {
        //            query = query.Join(GetData(rel.Target), rel.SourceAttribute, rel.TargetAttribute, "new(inner, outer)", null);
        //        }
        //    }

        //}

        private static readonly Dictionary<string, Type> cachedTypes = new Dictionary<string, Type>();

        protected abstract IQueryable<T> GetTypedData<T, TK>(string repository, IEnumerable<string> attributes, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null) where T : class;

        [DataMember]
        public virtual string Name { get; set; }

        public virtual Dictionary<string, object> GetDefaultRepositories()
        {
            return null;
        }
        public virtual Type GetTypedClass(string repository)
        {
            Type ret;
            lock (cachedTypes)
            {
                var cachekey = this.GetHashCode() + "_" + repository;
                if (!cachedTypes.TryGetValue(cachekey, out ret))
                {
                    var properties = new[] { new DynamicProperty("__UID", typeof(long)) }.Concat(this.GetColumns(repository).Select(t => new DynamicProperty(t.Name, t.Type))).ToList();

                    ret = DynamicClassFactory.CreateType(properties);

                    cachedTypes.Add(cachekey, ret);
                }
            }

            return ret;
        }

        public virtual string GetFormattedValue(object src, object c)
        {
            /*if (CustomFormats != null && CustomFormats.ContainsKey(GetFormatKey(null, c)))
            {
                return ((IFormattable)src).ToString(CustomFormats[GetFormatKey(null, c)], null);
            }
            else*/
            {
                return src.ToString();
            }
        }

        /*public string[] RepositoriesColumnNames
        {
            get { return new[] { "Key", "Value" }; }
            set { }
        }*/

        public abstract void InvalidateColumnsCache(string repository);

        public virtual string GetFormatKey(List<object> srcAttributesCollection, object srcAttribute)
        {
            return null;
        }

        [DataMember]
        public virtual List<string> SelectedGroups { get; set; } = new List<string>();

        public virtual string Host { get; set; } = "localhost";

        public Type Type { get { return this.GetType(); } }

        public abstract Dictionary<string, string> MonitoringTypes { get; }

        public DataProviderStruct ProviderTypeInfo { get { return DataProviders.AllProviders.Single(d => d.Type == this.GetType()); } }

        private Dictionary<string, object> _repositories = new Dictionary<string, object>();
        [DataMember]
        public Dictionary<string, object> Repositories
        {
            get { return _repositories; }
            set { _repositories = value.OrderBy(r => r.Key).ToDictionary(r => r.Key, r => r.Value); }
        }

        /// <summary>
        /// Gets typed data dynamically (for when the target type is unknown)
        /// </summary>
        /// <param name="repository">Source repository</param>
        /// <param name="attributes">Attributes (amongst repository's ones)</param>
        /// <param name="keys">Unused</param>
        /// <returns></returns>
        public IQueryable<dynamic> GetData(string repository = null, IEnumerable<string> attributes = null, IList<Dictionary<string, string>> values = null, Dictionary<string, Type> keys = null, Dictionary<string, long> statisticsBag = null)
        {
            var dataType = this.GetTypedClass(repository);
            Type keyType;
            /*if (keys != null)
            {
                lock (cachedTypes)
                {
                    var kx = keys.Select(k => k.Key).Aggregate((a, b) => a + "_" + b);
                    var cachekey = this.GetHashCode() + "_" + repository + kx; 
                    if (!cachedTypes.TryGetValue(cachekey, out xt))
                    {
                        xt = DynamicExpression.CreateClass(keys.Select(t => new DynamicProperty(t.Key, t.Value)));
                        cachedTypes.Add(cachekey, xt);
                    }
                }
            }
            else*/
            {
                keyType = typeof(string);
            }

            var m = this.GetType().GetMethod(nameof(GetTypedData)).MakeGenericMethod(dataType, keyType);

            var sw = Stopwatch.StartNew();

            var data = (IQueryable<dynamic>)m.Invoke(this, new object[] { repository, attributes, statisticsBag });

            sw.Stop();
            statisticsBag?.Add("TOTAL_INVOKE", sw.ElapsedMilliseconds);

            return data;
        }

        protected string UpdateValue(string src, IList<Dictionary<string, string>> values)
        {
            if (values == null)
            {
                return src;
            }

            return Regex.Replace(src, @"\$([^\d]*)(\d*)\$", m => values[int.TryParse(m.Groups[2].Value, out int res) ? res : 0][m.Groups[1].Value], RegexOptions.Compiled);
        }

        protected virtual long Count(string repository = null)
        {
            return this.GetData(repository).Count();
        }

        public abstract bool Test(out string details);

        public abstract List<ColumnDescription> GetColumns(string repository, IList<string> names = null);

        public virtual IEnumerable<RelationDefinition> GetRelations(string repository, IList<string> names = null)
        {
            return new RelationDefinition[0];
        }

        public virtual DataSet GetDataSet(Dictionary<string, SearchOptions> searchRep, int relationdepth, int startFrom, int? count, bool rootNodesOnly)
        {
            throw new NotImplementedException();
        }
    }
}
