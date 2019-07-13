using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Wokhan.Data.Providers.Bases;

namespace Wokhan.Data.Providers.Contracts
{
    public interface IDataProvider
    {
        string Name { get; set; }

        string Host { get; set; }

        Type Type { get; }

        Dictionary<string, string> MonitoringTypes { get; }

        DataProviderStruct ProviderTypeInfo { get; }

        /// <summary>
        /// Get data (for when the target type is already known)
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <typeparam name="TK">Key type</typeparam>
        /// <param name="repository">Source repository</param>
        /// <param name="attributes">Attributes</param>
        /// <returns></returns>
        Type GetTypedClass(string repository);

        string GetFormatKey(List<object> srcAttributesCollection, object srcAttribute);

        bool Test(out string details);

        List<string> SelectedGroups { get; }

        void RemoveCachedHeaders(string repository);

        List<ColumnDescription> GetColumns(string repository, IList<string> names = null);

        IEnumerable<RelationDefinition> GetRelations(string repository, IList<string> names = null);

        [Obsolete("Should be replaced by IQueryable Linq statements")]
        DataSet GetDataSet(Dictionary<string, SearchOptions> searchRep, int relationdepth, int startFrom, int? count, bool rootNodesOnly);

        IQueryable<dynamic> GetData(string repository = null, IEnumerable<string> attributes = null, Dictionary<string, Type> keys = null);

        IQueryable<T> GetTypedData<T, TK>(string repository, IEnumerable<string> attributes) where T : class;

        /// <summary>
        /// Retrieves all repositories along with the query to access each of them
        /// </summary>
        /// <returns></returns>
        Dictionary<string, object> GetDefaultRepositories();

        /*[Obsolete]
        IEnumerable GetDataDirect(string repository = null, IEnumerable<string> attributes = null);
        */
        Dictionary<string, object> Repositories { get; set; }

        string[] RepositoriesColumnNames { get; set; }
    }
}
