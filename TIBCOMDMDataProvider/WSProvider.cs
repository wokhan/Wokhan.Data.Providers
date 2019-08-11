using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers.TIBCOMDMDataProvider
{
    internal class WSProvider :  AbstractDataProvider
    {
        public bool RequiresDepth { get { return true; } }

        [ProviderParameter("WebServiceURI")]
        public string WebServiceURI { get; set; }

        [ProviderParameter("WebServiceLogin")]
        public string WebServiceLogin { get; set; }

        [ProviderParameter("WebServiceLogin")]
        public string WebServiceDefaultCount { get; set; }

        [ProviderParameter("WebServicePass")]
        public string WebServicePass { get; set; }

        public WSProvider(string ent) : base(ent) { }

        internal class BaseEntitySimpleComparer : IEqualityComparer<BaseEntityType>
        {
            internal static BaseEntitySimpleComparer Instance = new BaseEntitySimpleComparer();
            private static AttributeType fakeType = new AttributeType();

            public bool Equals(BaseEntityType x, BaseEntityType y)
            {
                return x.ExternalKey("MASTERCATALOGNAME") == y.ExternalKey("MASTERCATALOGNAME")
                        && x.ExternalKey("PRODUCTID") == y.ExternalKey("PRODUCTID")
                        && x.ExternalKey("PRODUCTIDEXT") == y.ExternalKey("PRODUCTIDEXT");
            }

            public int GetHashCode(BaseEntityType obj)
            {
                return (obj.ExternalKey("MASTERCATALOGNAME") + "#" + obj.ExternalKey("PRODUCTID") + "#" + obj.ExternalKey("PRODUCTIDEXT")).GetHashCode();
            }
        }

        /// <summary>
        /// Initialize a DataServiceType proxy object for a subsequent CIM Web service call.
        /// </summary>
        /// <param name="enterprise"></param>
        /// <returns></returns>
        private DataServiceType InitDataServiceType()
        {
            DataServiceType dst = new DataServiceType();
            dst.Identity = new[] 
            {
	            new IdentityType() { 
		            DirectoryPath = new [] 
                    {  
			            new DirectoryType() { type = DirectoryTypeType.Enterprise, Value = enterprise }, 
			            new DirectoryType() { type = DirectoryTypeType.User, Value = WebServiceLogin }
		            },
		            Authentication = new Pasteur.Aventis.TIBCO.MDM.AuthenticationType() 
                    { 
                        type = "Password", 
						Value = WebServicePass
                    }
	            }
            };
            return dst;
        }

        /// <summary>
        /// Retrieves the schema for a given enterprise and repository
        /// </summary>
        /// <param name="enterprise"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        internal static Stream GetSchema(string enterprise, string repository)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Retrieves data (as a DataServiceType object) from the CIM Web Service.
        /// </summary>
        /// <param name="enterprise"></param>
        /// <param name="repository"></param>
        /// <param name="filter"></param>
        /// <param name="active"></param>
        /// <param name="relations"></param>
        /// <param name="version"></param>
        /// <param name="startFrom"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public DataSet GetData(string repository, ILookup<string, string> attrs, Dictionary<string, string> filtersByRep, string active, out int countRes, List<ProfileRelation> relations, string version, int relationdepth = 1, string[] orderBy = null, int startFrom = 1, int? count = null, bool rootNodesOnly = false)
        {
            string filter = filtersByRep[repository];
            string[] attributes = attrs[repository].ToArray();

            CIMServices svc = new CIMServices() { Url = WebServiceURI };

            DataServiceType dst = InitDataServiceType();

            // ExternalKeys parameter initialization
            var extKeys = new List<KeyType> { new KeyType() { name = "MASTERCATALOGNAME", Value = repository } };

            if (!String.IsNullOrEmpty(active))
            {
                extKeys.Add(new KeyType() { name = "ACTIVE", Value = active });
            }

            if (!String.IsNullOrEmpty(version))
            {
                extKeys.Add(new KeyType() { name = "RECORD_VERSION", Value = version });
            }

            if (orderBy == null)
            {
                orderBy = new[] { "PRODUCTID", "PRODUCTIDEXT" };
            }

            if (!String.IsNullOrEmpty(filter))
            {
                /* Replace with regex */
                var keys = DataManager.OperatorRegEx.Matches(filter)
                                        .Cast<Match>()
                                        .Select(m => new KeyType()
                                        {
                                            name = m.Groups["attr"].Value,
                                            @operator = OperatorsMap[m.Groups["op"].Value],
                                            operatorSpecified = (m.Groups["op"].Value != "="),
                                            Value = m.Groups["val"].Value
                                        });

                extKeys.AddRange(keys);

                if (extKeys.Select(e => e.name).GroupBy(e => e).Any(g => g.Count() > 1))
                {
                    Exception e = new Exception("Multiple attribute values are not allowed when querying the CIM web service and will only work with no version specified.");
                    LogManager.Error("WS.GetData failed on {0}/{1} (productid={2}, active={3}, relations={4}, version={5}, startFrom={6}, count={7})", e, enterprise, repository, filter, active, relations, version, startFrom, count);
                    throw e;
                }
            }

            // Return parameter initialization
            var relToFilter = new RelationshipDataType();
            var relToRetrieve = new ReturnType();
            if (relations != null && relations.Any())
            {
                /*ret.RelationshipData = (relations == "*" ? new[] { new RelationshipMapType() { RelationDepth = "1" } }
                                                         : relations.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                                                                    .Select(r => new RelationshipMapType() { RelationDepth = relationdepth.ToString(), RelationType = r })
                                                                    .ToArray());*/
                var allRelations = relations.Where(r => r.softlink).Select(r => r.name).ToArray();
                relToRetrieve.RelationshipData = allRelations.Select(r => new RelationshipMapType() { RelationDepth = relationdepth.ToString(), RelationType = (r == "*" ? null : r) }).ToArray();

                if (rootNodesOnly && !relations.Any(r => r.name == "*"))
                {
                    var inverseRelations = getReverseRelations(repository, allRelations);
                    relToFilter.Relationship = inverseRelations.Select(ir => new RelationshipType() { RelationDepth = "1", RelationType = ir, checkExistence = false, checkExistenceSpecified = true }).ToArray();
                }
            }

            dst.Transaction = new TransactionType[] 
            {
	            new TransactionType() 
                { 
		            Command = new [] {
                        new Pasteur.Aventis.TIBCO.MDM.CommandType() 
                        { 
			                type = CommtypeType.Query,
                            typeSpecified = true,
			                MaxCount = (count.HasValue ? count.Value.ToString() : WebServiceDefaultCount), 
			                StartCount = startFrom.ToString(), 
                            OrderByColumnList = String.Join(",", orderBy),
			                Items = new [] 
                            {
				                new MasterCatalogRecordType() 
                                {
					                etype = "Entity",
                                    Items = new object[] {
                                        new ExternalKeysType() { Key = extKeys.ToArray() },
                                        relToFilter,
                                        relToRetrieve
                                    }
				                }
			                }
		                }
                    }
	            }
            };

            svc.MasterCatalogRecordAction(ref dst);

            if (dst.Result != null && dst.Result.Any(r => r.severity == ResultTypeSeverity1.Error))
            {
                Exception e = new Exception("TIBCO CIM Web Service query failed. Error(s): " + dst.Result.Select(r => r.code + " - " + r.Description.Value).Aggregate((a, b) => a + ", " + b));
                LogManager.Error("WS.GetData failed on {0}/{1} (productid={2}, active={3}, relations={4}, version={5}, startFrom={6}, count={7})", e, enterprise, repository, filter, active, relations, version, startFrom, count);
                throw e;
            }
            else
            {
                var failed = dst.Transaction.Where(t => t.TransactionResult.result == TransactionResultTypeResult.Failed);
                if (failed.Count() > 0)
                {
                    Exception e = new Exception("TIBCO CIM Web Service query failed. Error(s): " + failed.SelectMany(t => t.Response.SelectMany(r => r.ResultList.Select(rr => rr.Result[0].code + (rr.Result[0].Description != null ? " - " + rr.Result[0].Description.Value : "")))).Aggregate((a, b) => a + ", " + b));
                    LogManager.Error("WS.GetData failed on {0}/{1} (productid={2}, active={3}, relations={4}, version={5}, startFrom={6}, count={7})", e, enterprise, repository, filter, active, relations, version, startFrom, count);
                    throw e;
                }
                else
                {
                    if (dst.Transaction[0].Response[0].Items != null)
                    {
                        countRes = dst.Transaction[0].Response[0].Items.Length;
                        return ItemsToDataSet(enterprise, dst.Transaction[0].Response[0].Items);
                    }
                }

                var res = new DataSet();
                res.Tables.Add(repository);
                countRes = 0;

                return res;
            }
        }

        /// <summary>
        /// Adds all related items in dedicated tables in the DataSet (internal use only)
        /// </summary>
        /// <param name="resp"></param>
        /// <param name="ds"></param>
        /// <param name="dt"></param>
        private static DataSet ItemsToDataSet(string enterprise, BaseEntityType[] allItems)
        {
            DataSet ds = new DataSet("Results") { EnforceConstraints = false };
            ds.Namespace = "http://tibco.pasteur.aventis.com/mdmws";

            // Flattens the records list to build the corresponding DataTables (for each repository)
            // Note the distinct since records can appear multiple times in relationships
            var recordsPerRepo = allItems.Concat(allItems
                                         .Where(r => r.RelationshipData() != null)
                                         .SelectMany(r => getAllRelatedItems(r)))
                                         .Distinct(BaseEntitySimpleComparer.Instance)
                                         .GroupBy(x => x.ExternalKey("MASTERCATALOGNAME"))
                                         .ToList();

            foreach (var records in recordsPerRepo)
            {
                RightsManager.enforceRight(enterprise, RightsManager.BaseType.Read, records.Key);

                DataColumn[] cols = records.First()
                                           .EntityData().Attribute.Where(attr => !ConfigurationManager.Configuration[enterprise].HiddenFields.Contains(attr.name))
                                                                  .Select(a => new DataColumn(a.name, WSProvider.GetType(a.type)))
                                                                  .ToArray();

                DataTable childTable = records.AsDataTable(records.Key,
                                                           cols,
                                                           (o) => cols.Select(c => o.EntityData().Attribute.FirstOrDefault(a => a.name == c.ColumnName).Value).ToArray());

                ds.Tables.Add(childTable);
            }

            // Flattens all the relationships to build a relation table
            // No need for a distinct here... Well, maybe we do in fact, I'll have to double check that.
            var relatedPerType = allItems.Where(r => r.RelationshipData() != null)
                                         .SelectMany(r => getAllRelations(r))
                                         .GroupBy(x => x.RelationType)
                                         .ToList();

            foreach (var rel in relatedPerType)
            {
                DataColumn[] relationshipColumns = new[] 
                {
                        new DataColumn("PARENTID", typeof(string)),
                        new DataColumn("PARENTIDEXT", typeof(string)),
                        new DataColumn("CHILDID", typeof(string)),
                        new DataColumn("CHILDIDEXT", typeof(string))
                };

                DataTable relTable = rel.Select(x => x)
                                        .AsDataTable<RelationStruct>(rel.Key,
                                                     relationshipColumns,
                                                     (o) => new[] { o.Source.ExternalKey("PRODUCTID"),
                                                                    o.Source.EntityData().Attribute.Single(atr => atr.name == "PRODUCTIDEXT").Value,
                                                                    o.Target.ExternalKey("PRODUCTID"),
                                                                    o.Target.EntityData().Attribute.Single(atr => atr.name == "PRODUCTIDEXT").Value
                                                     });

                ds.Tables.Add(relTable);

                string childRepo = rel.First().Target.ExternalKey("MASTERCATALOGNAME");
                if (!ds.Relations.Contains(rel.Key + "+" + childRepo))
                {
                    string parentRepo = rel.First().Source.ExternalKey("MASTERCATALOGNAME");

                    var r1 = ds.Relations.Add(parentRepo + "+" + rel.Key, new[] { ds.Tables[parentRepo].Columns["PRODUCTID"], ds.Tables[parentRepo].Columns["PRODUCTIDEXT"] }, new[] { ds.Tables[rel.Key].Columns["PARENTID"], ds.Tables[rel.Key].Columns["PARENTIDEXT"] }, false);
                    ds.Relations.Add(rel.Key + "+" + childRepo, new[] { ds.Tables[rel.Key].Columns["CHILDID"], ds.Tables[rel.Key].Columns["CHILDIDEXT"] }, new[] { ds.Tables[childRepo].Columns["PRODUCTID"], ds.Tables[childRepo].Columns["PRODUCTIDEXT"] }, false);
                    r1.ExtendedProperties["type"] = "relationship";
                }
            }

            return ds;
        }

        private static IEnumerable<MasterCatalogRecordType> getAllRelatedItems(BaseEntityType resp)
        {
            if (resp.RelationshipData() != null)
            {
                return resp.RelationshipData()
                           .SelectMany(rel => rel.Items.Cast<RelatedEntitiesType>()
                                                       .SelectMany(i => i.Items.OfType<MasterCatalogRecordType>()
                                                                               .SelectMany(relitem => new[] { relitem }.Concat(getAllRelatedItems(relitem)))));
            }
            else
            {
                return new MasterCatalogRecordType[0];
            }
        }


        private class RelationStruct
        {
            public string RelationType;
            public BaseEntityType Source;
            public MasterCatalogRecordType Target;
        }

        private static IEnumerable<RelationStruct> getAllRelations(BaseEntityType resp)
        {
            if (resp.RelationshipData() != null)
            {
                return resp.RelationshipData()
                           .SelectMany(rel => rel.Items.Cast<RelatedEntitiesType>()
                                                  .SelectMany(i => i.Items.OfType<MasterCatalogRecordType>()
                                                                          .SelectMany(relitem => new[] 
                                                                          {
                                                                            new RelationStruct()
                                                                            {
                                                                              RelationType = rel.RelationType,
                                                                              Source = resp,
                                                                              Target = relitem
                                                                            } 
                                                                          }.Concat(getAllRelations(relitem)))));
            }
            else
            {
                return new RelationStruct[0];
            }
        }

        public void OverrideRelationInfo(ref DataManager.EnrichedRelation enrrel)
        {
            base.OverrideRelationInfo(ref enrrel);
        }

        #region Data management

        /// <summary>
        /// Returns the .Net type corresponding to a CIM data type
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        internal static Type GetType(DataType o)
        {
            switch (o)
            {
                case DataType.boolean: return typeof(bool);
                case DataType.amount: return typeof(decimal);
                case DataType.@decimal: return typeof(decimal);
                case DataType.integer: return typeof(int);
                case DataType.date: return typeof(DateTime);
                case DataType.@string: return typeof(string);
                default: return typeof(object);
            }
        }

        internal static object GetValue(AttributeType attr)
        {
            return GetValue(attr.type, attr.Value);
        }

        /// <summary>
        /// Retrieve the value of a given item
        /// </summary>
        /// <param name="t"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        internal static object GetValue(DataType type, string value)
        {
            switch (type)
            {
                case DataType.boolean: return bool.Parse(value);
                case DataType.amount: return decimal.Parse(value);
                case DataType.@decimal: return decimal.Parse(value);
                case DataType.integer: return int.Parse(value);
                case DataType.date: return DateTime.Parse(value);
                case DataType.@string: return value;
                default: return value;
            }
        }

        /// <summary>
        /// Retrieves all values of a multivalue attribute.
        /// IMPORTANT: For this to work properly, the BaseMultiValueType1 class (from which MultiValueType1 inherits) must be modified to include a specific namespace.
        /// If not, Value will be null.
        /// 
        /// [System.Xml.Serialization.XmlElementAttribute(ElementName = "Value", Namespace = "http://www.tibco.com/cim/services/mastercatalogrecord/wsdl/2.0")]
        /// public KeyType[] Value;
        /// </summary>
        /// <param name="t"></param>
        /// <param name="mv"></param>
        /// <returns></returns>
        internal static object GetMultiValue(DataType t, KeyType[] mv)
        {
            if (mv == null)
            {
                return null;
            }
            else
            {
                return mv.Select(v => GetValue(t, v.Value)).ToArray();
            }
        }

        /// <summary>
        /// Return all attributes values for a BaseEntityType (acts as a delegate for AsDataTable extension method, hence the "object" parameter type)
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        internal static object[] GetValues(BaseEntityType o)
        {
            var edata = o.EntityData();
            var alldata = edata.Attribute.Select(a =>
            {
                var r = GetValue(a.type, a.Value);
                if (a.name == "PRODUCTIDEXT" && a.Value == null)
                {
                    return "";
                }
                return r;
            });
            if (edata.MultiValueAttribute != null)
            {
                alldata = alldata.Concat(edata.MultiValueAttribute.Select(a => GetMultiValue(a.type, a.Value)));
            }
            return alldata.ToArray();
        }

        #endregion

        private static Dictionary<string, OperatorType> OperatorsMap = new Dictionary<string, OperatorType>() 
        {
            {"=", OperatorType.@in }, // Equal operator will be ignored anyway as it's the default on MDM's side
            {">", OperatorType.gt},
            {"<", OperatorType.lt},
            {"<>", OperatorType.ne},
            {"<=", OperatorType.le},
            {">=", OperatorType.ge},
            {":=", OperatorType.@in},
            {"~=", OperatorType.lk},
            {"^=", OperatorType.sw},
            {"$=", OperatorType.ew}
        };
    }
}