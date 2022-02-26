using System;

using Wokhan.Data.Providers.Bases;

namespace Wokhan.Data.Providers.Attributes
{
    /// <summary>
    /// Property attribute to get basic information about it (friendly name, description, category...).
    /// Also defines if the property is part of a key (composite or not).
    /// <seealso cref="ColumnDescription"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnDescriptionAttribute : Attribute
    { 
        /// <summary>
        /// Friendly name for the property (for instance to display it in a UI).
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Short description for this property.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Defines a category (can be anything) to group properties together.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Specifies if the property is used as a key (composite or not, meaning that there can be multiple keys).
        /// </summary>
        public bool IsKey { get; set; }
    }
}