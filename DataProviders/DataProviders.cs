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
    public class DataProviders
    {
        static DataProviders()
        {
            AllProviders = FromTypes(false, Assembly.GetExecutingAssembly().GetTypes()).ToList();
        }

        public static List<IGrouping<string, DataProviderMemberDefinition>> GetParameters(IDataProvider prv)
        {
            return prv.Type.GetProperties()
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
                            IsActive = prv.SelectedGroups.Contains(attr.ExclusionGroup),
                            Container = prv,
                            Name = p.Name,
                            Description = attr.Description,
                            MemberType = p.PropertyType,
                            IsFile = attr.IsFile,
                            FileFilter = attr.FileFilter,
                            ExclusionGroup = attr.ExclusionGroup,
                            ValuesGetter = attr.Method
                        };
                    })
                    .Where(_ => _?.Description != null)
                    .OrderBy(_ => _.Position).ThenBy(_ => _.Description)
                    .GroupBy(_ => _.ExclusionGroup ?? "Default")
                    .ToList();
        }

        public static IDataProvider CreateInstance(string provider, Dictionary<string, object> parameters)
        {
            var tp = AllProviders.FirstOrDefault(a => a.Name == provider);
            if (tp == null)
            {
                throw new Exception($"Unable to find provider '{provider}'. Please check your configuration.");
            }

            var ret = (IDataProvider)Activator.CreateInstance(tp.Type);
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

        public static void AddAssemblies(params Assembly[] assemblies)
        {
            AllProviders.AddRange(FromTypes(false, assemblies.SelectMany(a => a.GetTypes()).ToArray()));
        }

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

        public static List<DataProviderDefinition> AllProviders { get; private set; }
    }
}
