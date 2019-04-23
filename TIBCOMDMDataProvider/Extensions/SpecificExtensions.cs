using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Wokhan.Data.Providers.TIBCOMDMDataProvider.Extensions
{
    public static class SpecificExtensions
    {
        /// <summary>
        /// Turns any array into a DataTable
        /// </summary>
        /// <param name="collection">Source array</param>
        /// <param name="name">Target DataTable name</param>
        /// <param name="cols">Columns the DataTable will contain</param>
        /// <param name="GetValuesDelegate">Delegate to retrieve the columns values</param>
        /// <returns></returns>
        public static DataTable AsDataTable(this IEnumerable<BaseEntityType> collection, string name, DataColumn[] cols, Func<BaseEntityType, object[]> GetValuesDelegate)
        {
            // TEMPORARY FIX FOR THAT FREAKIN BUG IN TIBCO CIM 8.2.1 RETURNING TWICE THE SAME ATTRIBUTE
            // Seriously guys, did a no-brainer created that soft???
            foreach (DataColumn dcol in cols.Reverse())
            {
                int cpt = cols.Count(c => c.ColumnName == dcol.ColumnName);
                if (cpt > 1)
                {
                    dcol.ColumnName += cpt - 1;
                }
            }

            DataTable ret = new DataTable(name);
            ret.Columns.AddRange(cols);

            ret.BeginLoadData();

            foreach (BaseEntityType o in collection)
            {
                ret.Rows.Add(GetValuesDelegate(o));
            }

            ret.EndLoadData();
            //ret.AcceptChanges();

            return ret;
        }

        
        /// <summary>
        /// Simple extensions to ease the retrieval of a KeyType
        /// </summary>
        /// <param name="src"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string Get(this IEnumerable<KeyType> src, string key)
        {
            var r = src.FirstOrDefault(s => s.name == key);
            if (r != null)
            {
                return r.Value;
            }
         
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bet"></param>
        /// <returns></returns>
        public static string ExternalKey(this BaseEntityType bet, string key)
        {
            return bet.Items.OfType<ExternalKeysType>().FirstOrDefault().Key.Get(key);
        }

        /// <summary>
        /// Mimicks the RelationshipData property of CIM 8.0.1 (kept that way for readability)
        /// </summary>
        /// <param name="bet"></param>
        /// <returns></returns>
        public static RelationshipType[] RelationshipData(this BaseEntityType bet)
        {
            //8.0.1: bet.RelationshipData property
            var r = bet.Items.OfType<RelationshipDataType>().FirstOrDefault();
            if (r != null)
            {
                return r.Relationship;
            }

            return null;
        }

        /// <summary>
        /// Mimicks the EntityData property of CIM 8.0.1 (kept that way for readability)
        /// </summary>
        /// <param name="bet"></param>
        /// <returns></returns>
        public static EntityDataType EntityData(this BaseEntityType bet)
        {
            //8.0.1: bet.EntityData
            return bet.Items.OfType<EntityDataType>().FirstOrDefault();
        }

    }
}