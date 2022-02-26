using System;
using System.Collections.Generic;

using Wokhan.Data.Providers.Bases;

namespace Wokhan.Data.Providers.Attributes
{
    /// <summary>
    /// Property / field attribute to describe a data provider parameter (name, description, category...).
    /// <seealso cref="DataProviderMemberDefinition"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ProviderParameterAttribute : Attribute
    {
        /// <summary>
        /// Defines a custom category to group parameters on.
        /// </summary>
        public string Category;

        /// <summary>
        /// A description for the parameter.
        /// </summary>
        public string Description;

        /// <summary>
        /// Specify if the field value must be hidden in a UI (for a password for instance).
        /// </summary>
        public bool IsEncoded;

        /// <summary>
        /// Specifies if the property represents a file (in case a file picker is needed).
        /// </summary>
        public bool IsFile;


        /// <summary>
        /// Specifies a file name filter (should obviously be used only when <see cref="IsFile"/> is true).
        /// </summary>
        public string FileFilter;

        /// <summary>
        /// An exclusion group is used to group properties that defines similar configuration items.
        /// For instance, one could have a "ConnectionString" exclusion group, with only one property directly containing the said connection string, 
        /// and then a "Details" goup, containing separated values for the connection string, allowing to specify them independently.
        /// </summary>
        public string ExclusionGroup;

        /// <summary>
        /// Position of the parameter (to order them in a UI).
        /// </summary>
        public int Position = Int32.MaxValue;

        public delegate Dictionary<string, string> ValueGetterDelegate();

        /// <summary>
        /// A delegate to retrieve possible values for the current property.
        /// </summary>
        public ValueGetterDelegate? Method { get; }

        public ProviderParameterAttribute() { }

        /// <summary>
        /// Instanciate a new <see cref="ProviderParameterAttribute"/>, using at least a description.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="type"></param>
        /// <param name="methodName"></param>
        public ProviderParameterAttribute(string description, Type? type = null, string? methodName = null)
        {
            Description = description;
            if (type != null && methodName != null)
            {
                Method = (ValueGetterDelegate)Delegate.CreateDelegate(typeof(ValueGetterDelegate), type, methodName);
            }
        }

    }
}
