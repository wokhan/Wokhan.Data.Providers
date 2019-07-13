﻿using System;
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
        public static List<IGrouping<string, DataProviderMemberStruct>> GetParameters(IDataProvider prv)
        {
            var tr = prv.Type.GetProperties();
            return tr.Where(t => t.GetCustomAttributes<ProviderParameterAttribute>(true).Any())
                    .Select(p =>
                    {
                        var attr = p.GetCustomAttribute<ProviderParameterAttribute>(true);
                        return new
                        {
                            Pos = attr.Position,
                            Prov = new DataProviderMemberStruct()
                            {
                                IsActive = prv.SelectedGroups.Contains(attr.ExclusionGroup),
                                Container = prv,
                                Name = p.Name,
                                Description = attr.Description,
                                MemberType = p.PropertyType,
                                IsFile = attr.IsFile,
                                FileFilter = attr.FileFilter,
                                ExclusionGroup = attr.ExclusionGroup,
                                ValuesGetter = attr.Method
                            }
                        };
                    })
                    .Where(t => t.Prov.Description != null)
                    .OrderBy(t => t.Pos)
                    .ThenBy(t => t.Prov.Description)
                    .Select(t => t.Prov)
                    .GroupBy(c => c.ExclusionGroup ?? null)
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
                var prov = provprm.SelectMany(p => p).SingleOrDefault(p => p.Name == parameter.Key);
                if (prov != null)
                {
                    try
                    {
                        prov.ValueWrapper = Convert.ChangeType(parameter.Value, prov.MemberType);
                    }
                    catch { }
                }
            }

            return ret;
        }

        public static void Init(string basePath)
        {
            externalProviders = GetExternal(basePath);
        }


        private static DataProviderStruct[] externalProviders = new DataProviderStruct[0];
        private static DataProviderStruct[] embeddedProviders = new DataProviderStruct[0];

        public static IEnumerable<DataProviderStruct> AllProviders
        {
            get
            {
                if (embeddedProviders == null)
                {
                    embeddedProviders = GetEmbedded();
                }

                return embeddedProviders.Concat(externalProviders);
            }
        }

        private static DataProviderStruct[] GetExternal(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                throw new DirectoryNotFoundException(basePath);
            }
            else
            {
                var loadContext = new DataProviderLoadContext(basePath);
                var external = Directory.GetFiles(basePath, "*.dll")
                                        .SelectMany(f =>
                                        {
                                            try
                                            {
                                                return loadContext.LoadFromAssemblyPath(f).GetTypes();
                                            }
                                            catch (Exception e)
                                            {
                                                return new Type[0];
                                            }
                                        });

                return external.Where(t => t.IsClass && typeof(IExposedDataProvider).IsAssignableFrom(t))
                       .Select(t => new { External = true, Type = t, Attributes = t.GetCustomAttributes<DataProviderAttribute>(true).SingleOrDefault() })
                       .Select(t => new DataProviderStruct()
                       {
                           IsExternal = true,
                           Description = t.Attributes.Description,
                           Name = t.Attributes.Name,
                           Category = t.Attributes.Category,
                           Copyright = t.Attributes.Copyright,
                           IconPath = t.Attributes.Icon,
                           Type = t.Type,
                           IsDirectlyBindable = t.Attributes.IsDirectlyBindable
                       })
                       .ToArray();
            }
        }

        private static DataProviderStruct[] GetEmbedded()
        {
            return Assembly.GetExecutingAssembly()
                            .GetTypes()
                            .Where(t => t.IsClass && typeof(IExposedDataProvider).IsAssignableFrom(t))
                            .Select(t => new { External = false, Type = t, Attributes = t.GetCustomAttributes<DataProviderAttribute>(true).SingleOrDefault() })
                            .Select(t => new DataProviderStruct() { IsExternal = false, Description = t.Attributes.Description, Name = t.Attributes.Name, Category = t.Attributes.Category, Copyright = t.Attributes.Copyright, IconPath = t.Attributes.Icon, Type = t.Type }).ToArray();
        }



    }
}
