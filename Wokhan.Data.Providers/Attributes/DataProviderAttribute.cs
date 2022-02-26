using System;

using Wokhan.Data.Providers.Bases;

namespace Wokhan.Data.Providers.Attributes
{
    /// <summary>
    /// Class attribute to get basic information about a data provider (name, description, category...).
    /// <seealso cref="DataProviderDefinition"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DataProviderAttribute : Attribute
    {
        /// <summary>
        /// The category the provider related to.
        /// </summary>
        public string Category;

        /// <summary>
        /// The provider name.
        /// </summary>
        public string Name;

        /// <summary>
        /// Path to an icon in the assembly.
        /// </summary>
        public string Icon;

        /// <summary>
        /// A short copyright to identify the author.
        /// </summary>
        public string Copyright;

        /// <summary>
        /// A description explaining what the provider is for.
        /// </summary>
        public string Description;

        /// <summary>
        /// Indicates if the provider can be bound to directly.
        /// </summary>
        public bool IsDirectlyBindable;

        public DataProviderAttribute() { }
    }
}
