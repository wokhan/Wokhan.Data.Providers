using System;
using System.Collections.Generic;
using System.Linq;

using Wokhan.Data.Providers.Bases;

namespace Wokhan.Data.Providers.Contracts
{
    public interface IDataProvider
    {
        /// <summary>
        /// Name of this Data Provider instance.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Host (server) targetted by this provider instance (default to "localhost").
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// Gets the type of the current provider.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Retrieves this provider definition for the current provider. 
        /// </summary>
        DataProviderDefinition Definition { get; }

        /// <summary>
        /// Computes the type of the data for a given repository, using <see cref="DynamicClassFactory"/>.
        /// </summary>
        /// <param name="repository">Name of the corresponding repository.</param>
        /// <returns></returns>
        Type GetDataType(string repository);

        /// <summary>
        /// ???
        /// </summary>
        /// <param name="srcAttributesCollection"></param>
        /// <param name="srcAttribute"></param>
        /// <returns></returns>
        string GetFormatKey(List<object> srcAttributesCollection, object srcAttribute);

        /// <summary>
        /// Issues a test to ensure the provider's configuration is OK.
        /// </summary>
        /// <param name="details">A message stating if everything is alright.</param>
        /// <returns></returns>
        bool Test(out string details);

        /// <summary>
        /// Defines the selected exclusion groups.
        /// <seealso cref="DataProviderMemberDefinition.ExclusionGroup"/>
        /// </summary>
        List<string> SelectedGroups { get; }

        /// <summary>
        /// Clears the column cache.
        /// Overriding classes must implement this.
        /// </summary>
        /// <param name="repository">Name of the repository to clear cache for.</param>
        void InvalidateColumnsCache(string repository);

        /// <summary>
        /// Gets a list of <see cref="ColumnDescription"/> for a given repository.
        /// </summary>
        /// <param name="repository">Name of the repository to get columns for.</param>
        /// <param name="names">A list of column names if all columns are not required.</param>
        /// <returns></returns>
        List<ColumnDescription> GetColumns(string repository, IList<string> names = null);

        /// <summary>
        /// Gets typed data dynamically (for when the target type is unknown)
        /// </summary>
        /// <param name="repository">Source repository</param>
        /// <param name="attributes">Attributes (amongst repository's ones)</param>
        /// <param name="keys">Unused</param>
        /// <returns></returns>
        IQueryable GetQueryable(string? repository = null, IList<Dictionary<string, string>>? values = null, Dictionary<string, long>? statisticsBag = null);

        /// <summary>
        /// Allows to retrieve a typed IQueryable&lt;<typeparamref name="T"/>&gt;, automatically created by <see cref="GetQueryable(string, IList{Dictionary{string, string}}?, Dictionary{string, long}?)"/> to speed up treatment afterwards.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="repository"></param>
        /// <param name="values"></param>
        /// <param name="statisticsBag"></param>
        /// <returns></returns>
        IQueryable<T> GetQueryable<T>(string? repository = null, IList<Dictionary<string, string>>? values = null, Dictionary<string, long>? statisticsBag = null) where T : class;

        /// <summary>
        /// Gets the default "repositories" (that is, a list of collections of columns: can be tables, files, groups, sheets in a Excel file...)
        /// </summary>
        /// <returns></returns>
        Dictionary<string, object> GetDefaultRepositories();

        /// <summary>
        /// Defines if the user can add repositories or must only used predefined ones.
        /// </summary>
        bool AllowCustomRepository { get; }

        /// <summary>
        /// Gets a dictionary of repositories (name and query, for instance and for a SQL based repository).
        /// The value (query) part can be empty.
        /// </summary>
        Dictionary<string, object> Repositories { get; set; }
    }
}
