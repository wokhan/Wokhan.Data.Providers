using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Wokhan.Core.Extensions;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;

namespace Wokhan.Data.Providers.TIBCOMDMDataProvider
{
    public class DBProvider : OracleManagedDataProvider
    {
        public enum RelationshipModeEnum
        {
            MODELEGACY = 1,
            MODENEW = 2
        }

        [ProviderParameter("RelationshipMode")]
        public RelationshipModeEnum RelationshipMode { get; set; }

        [ProviderParameter("RelationshipTable")]
        public string RelationshipTable { get; set; }

        [ProviderParameter("DBNamesMappings")]
        public Dictionary<string, string> DBNamesMappings { get; set; }

        [ProviderParameter("ViewPrefix")]
        public string ViewPrefix { get; set; }

        [ProviderParameter("ViewRelationPrefix")]
        public string ViewRelationPrefix { get; set; }

        [ProviderParameter("ViewsConnectionString")]
        public string ViewsConnectionString { get; set; }


        protected string enterprise;

        private string[] DefaultSortColumns = new[] { "PRODUCTKEYID" };
        private const string DefaultKey = "PRODUCTKEYID";

        public DBProvider(string ent)
        {
            this.enterprise = ent;
        }

        private const string QUERY_REL_DETAILS =
@"SELECT 
r.NAME,
--CASE WHEN r.REVERSENAME = :relation AND r.TARGETCATALOGID <> -1 THEN 1 ELSE 0 END AS REVERSE,
CASE WHEN r.REVERSENAME = :relation THEN 1 ELSE 0 END AS REVERSE,
c1.NAME AS SOURCE,
c2.NAME AS TARGET
FROM RELATIONSHIPDEFINITION r 
INNER JOIN ORGANIZATION org ON r.ORGANIZATIONID = org.ID 
INNER JOIN ENTERPRISE ent ON ent.ID = org.ENTERPRISEID
INNER JOIN CATALOG c1 ON c1.ID = r.OWNERID 
INNER JOIN CATALOG c2 ON (r.TARGETCATALOGID <> -1 AND c2.ID = r.TARGETCATALOGID) OR (r.TARGETCATALOGID = -1 AND c2.ID = c1.ID)
WHERE UPPER(org.NAME) = UPPER(:enterprise)
AND (r.NAME = :relation OR r.REVERSENAME = :relation) 
AND r.ACTIVE = 'Y' 
AND ROWNUM = 1
AND c1.MODVERSION = (SELECT MAX(MODVERSION) FROM CATALOG WHERE ID = c1.ID)
AND c2.MODVERSION = (SELECT MAX(MODVERSION) FROM CATALOG WHERE ID = c2.ID)
AND r.MODVERSION = (SELECT MAX(MODVERSION) FROM RELATIONSHIPDEFINITION WHERE ID = r.ID)";

        private const string QUERY_REL_DETAILS_RELTABLE =
@"SELECT NAME, 
CASE WHEN NAME = :relation THEN 0 ELSE 1 END AS REVERSE,
SOURCE, TARGET, 
SOURCE_VIEW, TARGET_VIEW, 
VIEW_NAME FROM {0} WHERE NAME = :relation OR REVERSENAME = :relation";

        private const string QUERY_REL_NAMES_FOR_REPO =
@"SELECT DISTINCT r.NAME
  FROM RELATIONSHIPDEFINITION r 
  INNER JOIN ORGANIZATION org ON r.ORGANIZATIONID = org.ID 
  INNER JOIN ENTERPRISE ent ON ent.ID = org.ENTERPRISEID
  INNER JOIN CATALOG c1 ON c1.ID = r.OWNERID 
  LEFT JOIN CATALOG c2 ON c2.ID = r.TARGETCATALOGID 
  WHERE (c1.NAME = :repository OR c2.NAME = :repository)
  AND UPPER(ent.NAME) = UPPER(:enterprise) AND r.ACTIVE = 'Y'
  AND c1.MODVERSION = (SELECT MAX(MODVERSION) FROM CATALOG WHERE ID = c1.ID)
  AND (r.TARGETCATALOGID = -1 OR c2.MODVERSION = (SELECT MAX (MODVERSION) FROM CATALOG WHERE ID = c2.ID))
  AND r.MODVERSION = (SELECT MAX(MODVERSION) FROM RELATIONSHIPDEFINITION WHERE ID = r.ID)";

        private const string QUERY_REL_NAMES_FOR_REPO_RELTABLE = "SELECT NAME FROM {0} WHERE SOURCE = :repository";

        /*private const string QUERY_REVERSED_REL =
@"SELECT DISTINCT r.REVERSENAME
  FROM RELATIONSHIPDEFINITION r 
  INNER JOIN CATALOG c1 ON c1.ID = r.TARGETCATALOGID 
  WHERE r.NAME IN ({0})
  AND r.ACTIVE = 'Y'
  AND c1.NAME = :repository
  AND c1.MODVERSION = (SELECT MAX(MODVERSION) FROM CATALOG WHERE ID = c1.ID)
  AND r.MODVERSION = (SELECT MAX(MODVERSION) FROM RELATIONSHIPDEFINITION WHERE ID = r.ID)
UNION ALL 
SELECT DISTINCT r.NAME
  FROM RELATIONSHIPDEFINITION r 
  INNER JOIN CATALOG c1 ON c1.ID = r.OWNERID 
  WHERE r.REVERSENAME IN ({0})
  AND r.ACTIVE = 'Y'
  AND c1.NAME = :repository
  AND c1.MODVERSION = (SELECT MAX(MODVERSION) FROM CATALOG WHERE ID = c1.ID)
  AND r.MODVERSION = (SELECT MAX(MODVERSION) FROM RELATIONSHIPDEFINITION WHERE ID = r.ID)";
        */
        private const string QUERY_COLUMNS =
@"SELECT att.NAME, att.DISPLAYNAME, att.DESCRIPTION, atttype.NAME as DATATYPE, att.CATEGORY
  FROM ENTERPRISE ent
  INNER JOIN ORGANIZATION org ON org.ENTERPRISEID = ent.ID
  INNER JOIN CATALOG cat ON cat.SOURCEORGANIZATIONID = org.ID 
  INNER JOIN CATALOGATTRIBUTE att ON cat.ID = att.CATALOGID AND att.CATALOGVERSIONNUMBER = cat.MODVERSION
  INNER JOIN CATALOGATTRIBUTEDATATYPE atttype ON atttype.ID = att.CATALOGATTRIBUTEDATATYPEID
  WHERE UPPER(ent.NAME) = UPPER(:enterprise) and UPPER(cat.NAME) = upper(:repository)
  AND att.ACTIVE = 'Y'
  AND cat.MODVERSION = (SELECT MAX(modversion) FROM catalog WHERE ID = cat.ID)
  AND att.MODVERSION = (SELECT MAX(modversion) FROM catalogattribute WHERE ID = att.ID)
  AND att.NAME <> 'PRODUCTKEYID'
  ORDER BY att.ATTRIBUTEPOSITION";

        private const string QUERY_BASE = "SELECT {0} \"__LVL\", {1} FROM {2} a";

        private const string QUERY_PAGINATE_SIZE_ORDER_REQ = "SELECT * FROM (SELECT /*+ first_rows({0}) */ a.*, ROW_NUMBER() OVER (ORDER BY {1}) \"__RN\" FROM ({2}) a) WHERE \"__RN\" BETWEEN :startFrom AND (:startFrom + :count) - 1 ORDER BY \"__RN\"";

        private const string QUERY_COUNT = "SELECT COUNT(*) FROM ({0}) a";

        private const string QUERY_IN_BY_XML = "{0} WHERE EXISTS (SELECT 1 FROM XMLTABLE('/xs/x' PASSING :xmlstr COLUMNS X DECIMAL PATH '/x') xml_rel WHERE xml_rel.x = a.\"{1}\")";

        private const string QUERY_CHECK_REVERSED_REL = "NOT EXISTS(SELECT 1 FROM {0} xr WHERE xr.\"{1}\" = {2})";

        private const string QUERY_REL_SOFTLINK = "SELECT DISTINCT a.\"{0}\" \"{1}\", b.\"{0}\" \"{2}\" FROM \"{3}\" a INNER JOIN \"{4}\" b ON a.\"{5}\" = b.\"{6}\"";

        private const string QUERY_REL_TABLE = "SELECT a.\"{0}\", a.\"{1}\" {2} FROM {3} a ";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enterprise"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public IEnumerable<RelationDefinition> GetRelations(string repository, IList<string> names = null)
        {
            var ret = new List<string>();

            string req;
            if (RelationshipMode == RelationshipModeEnum.MODELEGACY)
            {
                req = QUERY_REL_NAMES_FOR_REPO;
            }
            else
            {
                req = String.Format(QUERY_REL_NAMES_FOR_REPO_RELTABLE, RelationshipTable);
            }

            using (var subconn = new OracleConnection(ConnectionString))
            {
                using (var subcmd = new OracleCommand(QUERY_REL_NAMES_FOR_REPO, subconn) { BindByName = true })
                {
                    subcmd.Parameters.Add("enterprise", enterprise);
                    subcmd.Parameters.Add("repository", repository);

                    subconn.Open();
                    using (OracleDataReader odr = subcmd.ExecuteReader())
                    {
                        while (odr.Read())
                        {
                            ret.Add(odr[0].ToString());
                        }
                    }
                }
            }

            return ret.Select(r => { var x = new RelationDefinition() { Name = r }; OverrideRelationInfo(ref x); return x; }).ToArray();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="enterprise"></param>
        /// <param name="rel"></param>
        /// <param name="sourcerepo"></param>
        /// <param name="targetRepo"></param>
        /// <returns></returns>
        //private string retrieveRelatedTables(string rel, out bool isreversed, out string relationView, ref string sourcerepo, out string targetRepo, out string sourceView, out string targetView, out string sourceAttribute, out string targetAttribute)
        private void OverrideRelationInfo(ref RelationDefinition enrrel)
        {
            if (enrrel.IsSoftlink)
            {
                enrrel.TechnicalSource = (ViewPrefix + enrrel.Source).Truncate(30);
                enrrel.TechnicalTarget = (ViewPrefix + enrrel.Target).Truncate(30);

                return;
            }

            var islegacy = (RelationshipMode == RelationshipModeEnum.MODELEGACY);

            //var cached = CacheManager.Get<object[]>(enterprise + "_Relations_" + enrrel.Name);
            object[] cached = null;
            if (cached == null)
            {
                string req;
                string cstring;

                if (islegacy)
                {
                    req = QUERY_REL_DETAILS;
                    cstring = ConnectionString;
                }
                else
                {
                    req = String.Format(QUERY_REL_DETAILS_RELTABLE, RelationshipTable);
                    cstring = ViewsConnectionString;
                }

                using (var subconn = new OracleConnection(cstring))
                {
                    using (var subcmd = new OracleCommand(req, subconn) { BindByName = true })
                    {
                        subcmd.Parameters.Add("enterprise", enterprise);
                        subcmd.Parameters.Add("relation", enrrel.Name);

                        subconn.Open();
                        using (OracleDataReader odr = subcmd.ExecuteReader())
                        {
                            if (odr.Read())
                            {
                                cached = new object[odr.FieldCount];
                                odr.GetValues(cached);
                                //CacheManager.Add(enterprise + "_Relations_" + enrrel.Name, cached);
                            }
                            else
                            {
                                Exception e = new ArgumentOutOfRangeException(enrrel.Name, "The relation does not exist.");
                                //LogManager.Error("Unable to retrieve the related tables for the relation.", e);
                                throw e;
                            }
                        }
                    }
                }
            }

            enrrel.IsReversed = ((decimal)cached[1] == 1);

            enrrel.Source = (string)(enrrel.IsReversed ? cached[3] : cached[2]);
            enrrel.Target = (string)(enrrel.IsReversed ? cached[2] : cached[3]);

            if (islegacy)
            {
                enrrel.TechnicalSource = (ViewPrefix + enrrel.Source).Truncate(30);
                enrrel.TechnicalTarget = (ViewPrefix + enrrel.Target).Truncate(30);
                enrrel.TechnicalName = formatName(ViewRelationPrefix + (string)cached[0], 30);

                var childPrefix = (enrrel.Source == enrrel.Target) ? "_CH" : "";
                enrrel.TechnicalSourceAttr = (enrrel.IsReversed ? formatName(enrrel.Source, 25 - childPrefix.Length) + childPrefix : formatName(enrrel.Source, 25)) + "_PKID";
                enrrel.TechnicalTargetAttr = (enrrel.IsReversed ? formatName(enrrel.Target, 25) : formatName(enrrel.Target, 25 - childPrefix.Length) + childPrefix) + "_PKID";
            }
            else
            {
                enrrel.TechnicalSource = (string)(enrrel.IsReversed ? cached[5] : cached[4]);
                enrrel.TechnicalTarget = (string)(enrrel.IsReversed ? cached[4] : cached[5]);
                enrrel.TechnicalName = (string)cached[6];
                enrrel.TechnicalSourceAttr = enrrel.IsReversed ? "CHILDPKID" : "PARENTPKID";
                enrrel.TechnicalTargetAttr = enrrel.IsReversed ? "PARENTPKID" : "CHILDPKID";
            }
        }

        /// <summary>
        /// Formats the relation or column names
        /// </summary>
        /// <param name="relation"></param>
        /// <returns></returns>
        private string formatName(string relation, int maxlength)
        {
            var ret = relation;
            var dbn = DBNamesMappings;
            foreach (var org in dbn.Keys)
            {
                ret = ret.Replace(org, dbn[org]);
            }

            // LogManager.Info("Formatting relation/column name from {0} to {1}", relation, ret);

            return ret.Truncate(maxlength);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enterprise"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public override List<ColumnDescription> GetColumns(string repository, IList<string> names = null)
        {
            var ret = new List<ColumnDescription>();

            using (var subconn = new OracleConnection(ConnectionString))
            {
                using (var subcmd = new OracleCommand(QUERY_COLUMNS, subconn) { BindByName = true })
                {
                    subcmd.Parameters.Add("enterprise", enterprise);
                    subcmd.Parameters.Add("repository", repository);

                    subconn.Open();
                    using (OracleDataReader odr = subcmd.ExecuteReader())
                    {
                        while (odr.Read())
                        {
                            if (!HiddenFields.Contains(odr["NAME"].ToString(), StringComparer.InvariantCultureIgnoreCase))
                            {
                                ret.Add(new ColumnDescription()
                                {
                                    Name = odr["NAME"].ToString().ToUpper(),
                                    DisplayName = odr["DISPLAYNAME"].ToString(),
                                    Category = odr["CATEGORY"].ToString(),
                                    // TODO: UPDAAAAAATE
                                    //Type = odr["DATATYPE"].ToString(),
                                    Description = odr["DESCRIPTION"].ToString()
                                });
                            }
                        }
                    }
                }
            }

            return ret;
        }

        /*public override DataSet GetDataSet(Dictionary<string, SearchOptions> searchRep, int relationdepth = 1, int startFrom = 1, int? count = null, bool rootNodeOnly = false)
        { 
            // TEMPORARY
            // TODO: Remove this to directly use a SearchOptions based implementation for the main GetData method.
            var ret = GetData(searchRep.Keys.First(),
                searchRep.SelectMany(s => s.Value.Attributes.Select(a => new { s.Key, a })).ToLookup(a => a.Key, a => a.a),
                searchRep.ToDictionary(s => s.Key, s => s.Value.Filter),
                searchRep.TryGetValue("IS_ACTIVE", out SearchOptions value) && value.Attributes.Any(a => a == "Y") ? "Y" : "N",
                out var countRes,
                searchRep.Values.SelectMany(s => s.Relations).ToList(),
                relationdepth,
                searchRep.SelectMany(s => s.Value.SortOrders.Select(a => new { s.Key, a })).ToLookup(a => a.Key, a => a.a),
                startFrom,
                count,
                rootNodeOnly);

            searchRep.First().Value.TotalCount = countRes;

            return ret;
        }
        */
        /// <summary>
        /// Retrieves the repository data using the TIBCO CIM views.
        /// </summary>
        /// <param name="sourcerepo"></param>
        /// <param name="active"></param>
        /// <param name="retrieveRelated"></param>
        /// <param name="startFrom"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        private DataSet GetData(string repository, ILookup<string, string> attrs, Dictionary<string, string> filtersByRep, string active, out int countRes, List<RelationDefinition> allrelInfo, int relationdepth = 1, ILookup<string, string> sortOrderByRep = null, int startFrom = 1, int? count = null, bool rootNodeOnly = false)
        {
            string filter = null;
            filtersByRep.TryGetValue(repository, out filter);

            var attributes = (attrs != null ? attrs[repository].ToArray() : null);
            var orderBy = (sortOrderByRep != null ? sortOrderByRep[repository].ToArray() : null);

            //LogManager.Debug("Retrieving data from {0}/{1} [active={2}, relations={3}, productid={4}, startFrom={5}, count={6}", enterprise, repository, active, allrelInfo != null ? allrelInfo.Count : 0, filter, startFrom, count);
            List<DataRow> rootEntries;

            var ds = new DataSet("Results");
            ds.EnforceConstraints = false;

            using (var conn = new OracleConnection(ViewsConnectionString))
            {
                try
                {
                    conn.Open();

                    // To allow case insensitive searches
                    using (OracleCommand setNLSCmd = conn.CreateCommand())
                    {
                        setNLSCmd.CommandText = DefaultSessionCommand;
                        setNLSCmd.ExecuteNonQuery();
                    }

                    var filteredAttributes = "a.*";
                    if (attributes != null && attributes.Any())
                    {
                        filteredAttributes = String.Join(",", attributes.Concat(new[] { DefaultKey }).Distinct().Select(a => "a.\"" + a + "\""));
                    }
                    var req = String.Format(QUERY_BASE, 1, filteredAttributes, ViewPrefix + repository.Truncate(28));

                    using (var oda = new OracleDataAdapter(req, conn))
                    {
                        oda.SelectCommand.BindByName = true;

                        var whereClauses = new List<string>();

                        if (!String.IsNullOrEmpty(filter))
                        {
                            parseFilter(filter, oda, attributes, ref whereClauses);
                        }

                        if (rootNodeOnly && allrelInfo != null)
                        {
                            // Adding a clause to filter out items with parent records
                            whereClauses.AddRange(allrelInfo.Where(r => !r.IsSoftlink && r.Target == repository)
                                                            .Select(r => String.Format(QUERY_CHECK_REVERSED_REL, r.TechnicalName, r.TechnicalTargetAttr, DefaultKey)));
                        }

                        if (whereClauses.Any())
                        {
                            req += " WHERE (" + String.Join(") AND (", whereClauses) + ")";
                        }

                        if (orderBy == null || orderBy.Length == 0)
                        {
                            orderBy = DefaultSortColumns;
                        }

                        oda.SelectCommand.CommandText = String.Format(QUERY_COUNT, req);
                        countRes = (int)(decimal)oda.SelectCommand.ExecuteScalar();

                        if (count.HasValue)
                        {
                            req = String.Format(QUERY_PAGINATE_SIZE_ORDER_REQ, count.Value, String.Join(", ", orderBy), req);

                            oda.SelectCommand.Parameters.Add("startFrom", startFrom);
                            oda.SelectCommand.Parameters.Add("count", count.Value);
                        }
                        else
                        {
                            req += " ORDER BY " + String.Join(",", orderBy);
                        }

                        oda.SelectCommand.CommandText = req;

                        //if (countRes > 0)
                        {
                            oda.Fill(ds, repository);
                        }
                        /*else
                        {
                            oda.FillSchema(ds, SchemaType.Mapped, repository);
                        }*/

                        ds.Tables[0].PrimaryKey = new[] { ds.Tables[0].Columns[DefaultKey] };
                        //ds.Tables[0].Columns[DefaultKey].ExtendedProperties[DataManager.EXT_FLAG_HIDDEN] = true;
                        //ds.Tables[0].Columns["__LVL"].ExtendedProperties[DataManager.EXT_FLAG_HIDDEN] = true;

                        /*if (ds.Tables[0].Columns.Contains("__RN"))
                        {
                            ds.Tables[0].Columns["__RN"].ExtendedProperties[DataManager.EXT_FLAG_HIDDEN] = true;
                        }*/

                        rootEntries = ds.Tables[0].Rows.Cast<DataRow>().ToList();

                        if (allrelInfo != null)
                        {
                            DataTable sourceTable;

                            //for (int i = 2; i <= relationdepth + 1; i++)
                            {
                                foreach (var rel in allrelInfo.Where(a => a.Source == repository))
                                {
                                    // Checks if the user can read related repositories as well
                                    //RightsManager.enforceRight(enterprise, RightsManager.BaseType.Read, rel.Target);

                                    var targetAttributes = (attrs != null && attrs.Contains(rel.Target) ? String.Join(", ", attrs[rel.Target].Concat(new[] { DefaultKey })) : "a.\"" + DefaultKey + "\"");

                                    oda.SelectCommand.Parameters.Clear();

                                    var xmlstr = oda.SelectCommand.Parameters.Add(new OracleParameter("xmlstr", OracleDbType.XmlType) { Direction = ParameterDirection.Input });

                                    sourceTable = ds.Tables[rel.Source];

                                    if (sourceTable == null)
                                    {
                                        throw new ArgumentOutOfRangeException("Relation " + rel.Name + " is not properly defined. The source table does not seem to match. Please check the profiles configuration.");
                                    }

                                    string cmdRel;
                                    if (rel.IsSoftlink)
                                    {
                                        cmdRel = String.Format(QUERY_REL_SOFTLINK, DefaultKey, rel.TechnicalSourceAttr, rel.TechnicalTargetAttr, rel.TechnicalSource, rel.TechnicalTarget, rel.SourceAttribute, rel.TargetAttribute);
                                    }
                                    else
                                    {
                                        cmdRel = String.Format(QUERY_REL_TABLE, rel.TechnicalSourceAttr, rel.TechnicalTargetAttr, String.Join("", attrs[rel.Name].Select(a => ", a.\"" + a + "\"")), rel.TechnicalName);
                                    }

                                    /*List<string> whereClausesRel = new List<string>();
                                    if (filtersByRep.ContainsKey(rel.Name))
                                    {
                                        parseFilter(filtersByRep[rel.Name], oda, attributes, ref whereClausesRel);
                                        parseFilter("{" + (rel.IsSoftlink ? DefaultKey : rel.TechnicalSourceAttr) + ":=" + String.Join(",", sourceTable.AsEnumerable().Select(r => r[DefaultKey])) + "}", oda, attributes, ref whereClausesRel);
                                    }

                                    if (whereClausesRel.Any())
                                    {
                                        req += " WHERE (" + String.Join(") AND (", whereClauses) + ")";
                                    }*/

                                    var cmdTarget = String.Format(QUERY_BASE, 1, targetAttributes, rel.TechnicalTarget);

                                    //var RealKey = rel.softlink ? "PRODUCTKEYID" : rel.sourceattribute;
                                    // Only retrieve relationships for the previously added added records (others have been handled already).
                                    if (sourceTable.Rows.Count > 0)
                                    {
                                        xmlstr.Value = sourceTable.Rows.Cast<DataRow>()
                                                                  .Aggregate(new StringBuilder("<xs>"), (a, b) => a.Append("<x>").Append(b[DefaultKey]).Append("</x>"))
                                                                  .Append("</xs>")
                                                                  .ToString();

                                        oda.SelectCommand.CommandText = String.Format(QUERY_IN_BY_XML, cmdRel, rel.IsSoftlink ? DefaultKey : rel.TechnicalSourceAttr);

                                        using (var relTable = new DataTable(rel.Name))
                                        {
                                            oda.Fill(relTable);
                                            relTable.PrimaryKey = new[] { relTable.Columns[rel.TechnicalSourceAttr], relTable.Columns[rel.TechnicalTargetAttr] };
                                            ds.Merge(relTable);
                                        }

                                        if (ds.Tables[rel.Name].Rows.Count > 0)
                                        {
                                            if (!rel.RetrieveAll || filter != null)
                                            {
                                                var ids = String.Join(",", ds.Tables[rel.Name].Rows.Cast<DataRow>().Select(r => r[rel.TechnicalTargetAttr]).Distinct());
                                                if (filtersByRep.ContainsKey(rel.Target))
                                                {
                                                    filtersByRep[rel.Target] += ",{" + DefaultKey + ":=" + ids + "}";
                                                }
                                                else
                                                {
                                                    filtersByRep[rel.Target] = "{" + DefaultKey + ":=" + ids + "}";
                                                }
                                            }

                                            int cntres2;
                                            // Avoid looping by filtering out allrelInfo for current repository. Ugly hack.
                                            ds.Merge(GetData(rel.Target, attrs, filtersByRep, active, out cntres2, allrelInfo.Where(ar => ar.Source != repository).ToList()));
                                        }

                                        if (rel.Target == repository)
                                        {
                                            // Restoring the root entries when there are loops. This sucks.
                                            rootEntries.ForEach(r => r["__LVL"] = (decimal)1);
                                        }
                                    }

                                    if (!ds.Tables.Contains(rel.Target))
                                    {
                                        int cntres2;
                                        ds.Merge(GetData(rel.Target, attrs, filtersByRep, active, out cntres2, allrelInfo.Where(ar => ar.Source != repository).ToList(), count: 0));
                                        /*
                                        oda.SelectCommand.CommandText = cmdTarget;
                                        oda.FillSchema(ds, SchemaType.Source, rel.Target);*/
                                    }

                                    if (!ds.Tables.Contains(rel.Name))
                                    {
                                        oda.SelectCommand.CommandText = cmdRel;
                                        oda.FillSchema(ds, SchemaType.Source, rel.Name);
                                    }

                                    foreach (var atr in attrs[rel.Name])
                                    {
                                        // ds.Tables[rel.Name].Columns[atr].ExtendedProperties[DataManager.EXT_REL_ATTRIBUTE] = true;
                                        TODOColumnsRelAttributes.Add(rel.Name, atr);
                                    }

                                    ds.Tables[rel.Name].PrimaryKey = new[] { ds.Tables[rel.Name].Columns[rel.TechnicalSourceAttr], ds.Tables[rel.Name].Columns[rel.TechnicalTargetAttr] };
                                    ds.Tables[rel.Target].PrimaryKey = new[] { ds.Tables[rel.Target].Columns[DefaultKey] };
                                    //ds.Tables[rel.Target].Columns[DefaultKey].ExtendedProperties[DataManager.EXT_FLAG_HIDDEN] = true;
                                    //ds.Tables[rel.Target].Columns["__LVL"].ExtendedProperties[DataManager.EXT_FLAG_HIDDEN] = true;
                                    TODOColumnsHidden.Add(rel.Target, DefaultKey);
                                    TODOColumnsHidden.Add(rel.Target, "__LVL");
                                    //ds.Tables[rel.Name].ExtendedProperties[DataManager.EXT_FLAG_REL_TABLE] = true;
                                    TODORepositoryRel.Add(rel.Name);

                                    // *** Dataset Relations ***
                                    if (!ds.Relations.Contains(rel.Name))
                                    {
                                        var r1 = ds.Relations.Add(rel.Name, ds.Tables[rel.Source].Columns[DefaultKey], ds.Tables[rel.Name].Columns[rel.TechnicalSourceAttr], false);
                                        //r1.ExtendedProperties[DataManager.EXT_REL_TYPE] = rel.IsSoftlink ? DataManager.REL_TYPE_SOFTLINK : DataManager.REL_TYPE_REL;
                                        if (rel.IsSoftlink)
                                        {
                                            TODORelationSoftLink.Add(r1.RelationName);
                                        }
                                        //r1.ExtendedProperties[DataManager.EXT_REL_CARTESIAN] = rel.IsCartesian;
                                        TODORelationCartesian.Add(r1.RelationName);
                                        ds.Relations.Add(rel.Name + "_Target", ds.Tables[rel.Name].Columns[rel.TechnicalTargetAttr], ds.Tables[rel.Target].Columns[DefaultKey], false);
                                    }
                                }
                            }
                        }
                    }

                    return ds;
                }
                catch (Exception e)
                {
                    //LogManager.Error("Error retrieving data from {0}/{1} (active={2}, relations={3}, productid={4}, startFrom={5}, count={6})", e, enterprise, repository, active, allrelInfo.Count, filter, startFrom, count);
                    throw e;
                }
            }
        }


        private static void parseFilter(string filter, OracleDataAdapter oda, string[] attributes, ref List<string> whereClauses, string prefix = "P")
        {
            var attrs = OperatorRegEx.Matches(filter)
                                   .Cast<Match>()
                                   .Select((m, i) => new
                                   {
                                       PID = prefix + i,
                                       Key = m.Groups["attr"].Value,
                                       Value = m.Groups["val"].Value,
                                       RawOperator = m.Groups["op"].Value,
                                       Expression = OperatorsMap[m.Groups["op"].Value]
                                   })
                                   .ToList();

            var allAttrs = attrs.Where(a => String.IsNullOrEmpty(a.Key))
                                .Select(a => String.Join(" OR ", attributes.Select(x => String.Format(a.Expression, x, a.PID))));

            whereClauses.AddRange(allAttrs);

            var orClauses = attrs.Where(a => !String.IsNullOrEmpty(a.Key))
                                 .GroupBy(a => a.Key)
                                 .Select(g => String.Join(" OR ", g.Select(a => String.Format(a.Expression, a.Key, a.PID))));

            whereClauses.AddRange(orClauses);

            foreach (var attr in attrs)
            {
                if (attr.RawOperator == ":=")
                {
                    var xmlstr = oda.SelectCommand.Parameters.Add(attr.PID, OracleDbType.XmlType, ParameterDirection.Input);
                    xmlstr.Value = attr.Value.Split(',').Aggregate(new StringBuilder("<xs>"), (a, b) => a.Append("<x>").Append(b.Replace("&", "&amp;")).Append("</x>"))
                                                        .Append("</xs>")
                                                        .ToString();

                    /*var xmlstrT = new OracleXmlType(oda.SelectCommand.Connection, xmlstr);
                    oda.SelectCommand.Parameters.Add(attr.PID, OracleDbType.XmlType, xmlstrT, ParameterDirection.Input);*/
                }
                else
                {
                    oda.SelectCommand.Parameters.Add(attr.PID, (attr.RawOperator == "~=" ? attr.Value.Replace("_", "\\_").Replace("%", "\\%") : attr.Value));
                }
            }
        }

        public static Regex OperatorRegEx = new Regex("{(?<attr>.*?)(?<op>" + OperatorsMap.Keys.Select(k => Regex.Escape(k)).Aggregate((a, b) => a + "|" + b) + ")(?<val>.*?)}+");

        private static Dictionary<string, string> OperatorsMap = new Dictionary<string, string>()
        {
            {"=", "\"{0}\" = :{1}" },
            {">", "\"{0}\" > :{1}" },
            {"<", "\"{0}\" < :{1}" },
            {"<>", "\"{0}\" <> :{1}" },
            {"<=", "\"{0}\" <= :{1}" },
            {">=", "\"{0}\" >= :{1}" },
            // IN
            {":=", "EXISTS (SELECT 1 FROM XMLTABLE('/xs/x' PASSING :{1} columns X VARCHAR2(255) path '/x') WHERE X = \"{0}\")"},
            //{":=", " (0, \"{0}\") IN ({1})"},
            // LIKE
            {"~=", "\"{0}\" LIKE '%' || :{1} || '%' ESCAPE '\\'"},
            // STARTS WITH
            {"^=", "\"{0}\" LIKE :{1} || '%' ESCAPE '\\'"},
            // ENDS WITH
            {"$=", "\"{0}\" LIKE '%' || :{1} ESCAPE '\\'"},
            // NULLS
            {"*", "\"{0}\" IS NOT NULL" },
            {"-", "\"{0}\" IS NULL" }
        };

        public string DefaultSessionCommand { get { return "ALTER SESSION SET NLS_COMP=LINGUISTIC NLS_SORT=BINARY_CI"; } }

    }
}