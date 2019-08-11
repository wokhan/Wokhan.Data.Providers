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

        DataProviderStruct ProviderTypeInfo { get; }

        /// <summary>
        /// Get data (for when the target type is already known)
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <typeparam name="TK">Key type</typeparam>
        /// <param name="repository">Source repository</param>
        /// <param name="attributes">Attributes</param>
        /// <returns></returns>
        Type GetDataType(string repository);

        string GetFormatKey(List<object> srcAttributesCollection, object srcAttribute);

        bool Test(out string details);

        List<string> SelectedGroups { get; }

        void InvalidateColumnsCache(string repository);

        List<ColumnDescription> GetColumns(string repository, IList<string> names = null);

        IEnumerable<RelationDefinition> GetRelations(string repository, IList<string> names = null);//

       // [Obsolete("Should be replaced by IQueryable Linq statements")]
        //DataSet GetDataSet(Dictionary<string, SearchOptions> searchRep, int relationdepth, int startFrom, int? count, bool rootNodesOnly);

        IQueryable GetQueryable(string repository = null, Dictionary<string, long> statisticsBag = null);

        IQueryable<T> GetQueryable<T>(string repository = null, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null) where T : class;

        /// <summary>
        /// Retrieves all repositories along with the query to access each of them
        /// </summary>
        /// <returns></returns>
        Dictionary<string, object> GetDefaultRepositories();

        bool AllowCustomRepository { get; }

        /*[Obsolete]
        IEnumerable GetDataDirect(string repository = null, IEnumerable<string> attributes = null);
        */
        Dictionary<string, object> Repositories { get; set; }

        //string[] RepositoriesColumnNames { get; set; }
    }
}
