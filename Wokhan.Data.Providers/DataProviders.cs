using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers
{
    /// <summary>
    /// Utility class that hosts a list of all data providers, allowing to import new ones.
    /// Also provides method to access they columns/attributes, parameters, or to create a new instance directly.
    /// </summary>
    public static class DataProviders
    {
        /// <summary>
        /// List of all available providers (including scanned ones).
        /// </summary>
        public static List<DataProviderDefinition> AllProviders { get; private set; }

        static DataProviders()
        {
            AllProviders = FromTypes(false, Assembly.GetExecutingAssembly().GetTypes()).ToList();
        }

        /// <summary>
        /// Retrieves all parameters for a <see cref="IDataProvider"/>.
        /// Parameters are grouped using the <see cref="DataProviderMemberDefinition.ExclusionGroup"/> (or "Default" if null) to allow exclusive parameters to be used altogether.
        /// </summary>
        /// <param name="provider">Provider to get members from.</param>
        /// <returns>A list of <see cref="DataProviderMemberDefinition"/>, grouped by the <see cref="DataProviderMemberDefinition.ExclusionGroup"/> property.</returns>
        public static List<IGrouping<string, DataProviderMemberDefinition>> GetParameters(IDataProvider provider)
        {
            return provider.Type.GetProperties()
                    .Select(p =>
                    {
                        var attr = p.GetCustomAttributes<ProviderParameterAttribute>(true).SingleOrDefault();
                        if (attr == null)
                        {
                            return null;
                        }
                        return new DataProviderMemberDefinition
                        {
                            Position = attr.Position,
                            IsActive = provider.SelectedGroups.Contains(attr.ExclusionGroup),
                            Container = provider,
                            Name = p.Name,
                            Description = attr.Description,
                            MemberType = p.PropertyType,
                            IsFile = attr.IsFile,
                            FileFilter = attr.FileFilter,
                            ExclusionGroup = attr.ExclusionGroup,
                            ValuesGetter = attr.Method
                        };
                    })
                    .Where(definition => definition != null)
                    .OrderBy(definition => definition.Position).ThenBy(definition => definition.Description)
                    .GroupBy(definition => definition.ExclusionGroup ?? "Default")
                    .ToList();
        }


        /// <summary>
        /// Creates an instance of a provider (using it's name).
        /// Requires a dictionary describing the parameters for this provider.
        /// </summary>
        /// <param name="provider">Name of the provider to create an instance of.</param>
        /// <param name="parameters">A dictionary containing all parameters to set the provider properties from.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static IDataProvider CreateInstance(string provider, Dictionary<string, object> parameters)
        {
            var tp = AllProviders.FirstOrDefault(a => a.Name == provider);
            if (tp == null)
            {
                throw new Exception($"Unable to find provider '{provider}'. Please check your configuration.");
            }

            var ret = (IDataProvider)Activator.CreateInstance(tp.Type);
            if (ret == null)
            {
                throw new NullReferenceException("Activator failed to create an instance for the given type");
            }
            var provprm = GetParameters(ret);
            foreach (var parameter in parameters)
            {
                var prov = provprm.SelectMany(p => p).FirstOrDefault(p => p.Name == parameter.Key);
                if (prov != null)
                {
                    prov.ValueWrapper = parameter.Value;
                }
            }

            return ret;
        }

        /// <summary>
        /// Scans a folder and adds any data providers found (i.e. implementing <see cref="IExposedDataProvider"/>).
        /// </summary>
        /// <param name="basePath">Path to the directory to scan.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public static void AddPath(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException(basePath);
            }
            else
            {
                var loadContext = new DataProviderLoadContext(basePath);
                var assemblies = Directory.GetFiles(basePath, "*.dll")
                                        .Select(f =>
                                        {
                                            try
                                            {
                                                return loadContext.LoadFromAssemblyPath(f);
                                            }
                                            catch (Exception e)
                                            {
                                                return null;
                                            }
                                        })
                                        .Where(_ => _ != null)
                                        .ToArray();

                AllProviders.AddRange(FromTypes(true, assemblies.SelectMany(a => a.GetTypes()).ToArray()));
            }
        }

        /// <summary>
        /// Directly add data providers from the specified assemblies.
        /// </summary>
        /// <param name="assemblies">Assemblies to retrieve data providers from.</param>
        public static void AddAssemblies(params Assembly[] assemblies)
        {
            AllProviders.AddRange(FromTypes(false, assemblies.SelectMany(a => a.GetTypes()).ToArray()));
        }

        /// <summary>
        /// Directly add data providers from the specified types.
        /// </summary>
        /// <param name="types">Types to to retrieve data providers from.</param>
        /// <exception cref="ArgumentException"></exception>
        public static void AddTypes(params Type[] types)
        {
            var failed = types.FirstOrDefault(t => !t.IsClass || !typeof(IExposedDataProvider).IsAssignableFrom(t));
            if (failed != null)
            {
                throw new ArgumentException($"{failed.Name} type doesn't inherit from {nameof(AbstractDataProvider)} or doesn't implement {nameof(IExposedDataProvider)}. Cannot continue.");
            }
            AllProviders.AddRange(FromTypes(false, types));
        }

        private static DataProviderDefinition[] FromTypes(bool external, params Type[] types)
        {
            return types.Where(t => t.IsClass && typeof(IExposedDataProvider).IsAssignableFrom(t))
                        .Select(t => DataProviderDefinition.From(t, external))
                        .Where(t => t != null)
                        .ToArray();
        }
    }
}
