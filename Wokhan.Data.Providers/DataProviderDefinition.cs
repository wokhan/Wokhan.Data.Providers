using System;
using Wokhan.Data.Providers.Attributes;
using System.Reflection;
using System.Linq;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers.Bases
{
    /// <summary>
    /// Hosts a data provider definition (that is, a class representing metadata for a provider).
    /// </summary>
    public class DataProviderDefinition
    {
        /// <summary>
        /// A generic string to categorize this data provider (ex: "Database", "Process", "Files"...).
        /// </summary>
        public string Category { get; private set; }

        /// <summary>
        /// Name of the provider. Should clearly states what it is used for.
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// A clear description of what data the provider is used to access to.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Path to an icon in the containing assembly.
        /// </summary>
        public string IconPath { get; private set; }

        /// <summary>
        /// Contains the author of the current data provider.
        /// </summary>
        public string Copyright { get; private set; }
        
        /// <summary>
        /// The type implementing this data provider.
        /// </summary>
        public Type? Type { get; private set; }

        /// <summary>
        /// Indicates if a direct binding can be used.
        /// Note: I'm not sure it's still used, so might get removed in a later release.
        /// </summary>
        public bool IsDirectlyBindable { get; private set; }

        /// <summary>
        /// Specifies if the provider is embedded in Wokhan.Data.Providers assembly.
        /// Note: I'm not sure it's still used, so might get removed in a later release.
        /// </summary>
        public bool IsExternal { get; private set; }

        /// <summary>
        /// Builds a <see cref="DataProviderDefinition"/> from the specified type, filtering on public properties decorated with the <see cref="ColumnDescriptionAttribute"/> attribute.
        /// </summary>
        /// <param name="sourceType">DataProvider type to extract the definition from.</param>
        /// <returns>If the </returns>
        internal static DataProviderDefinition? From(Type t, bool external)
        {
            if (!typeof(IExposedDataProvider).IsAssignableFrom(t))
            {
                return null;
            }

            var attr = t.GetCustomAttributes<DataProviderAttribute>(true).SingleOrDefault();
            if (attr == null)
            {
                return null;
            }

            return new DataProviderDefinition
            {
                IsExternal = external,
                Description = attr.Description,
                Name = attr.Name,
                Category = attr.Category,
                Copyright = attr.Copyright,
                IconPath = (attr.Icon != null ? $"/{t.Assembly.GetName().Name};{t.Assembly.GetName().Name}.{attr.Icon.Replace("/", ".")}" : "/Wokhan.Data.Providers;Wokhan.Data.Providers.Resources.Providers.source-repository.png"),
                Type = t,
                IsDirectlyBindable = attr.IsDirectlyBindable
            };
        }
    }
}
