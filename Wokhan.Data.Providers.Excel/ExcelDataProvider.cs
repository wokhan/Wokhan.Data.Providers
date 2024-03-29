﻿using ExcelDataReader;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Wokhan.Collections.Generic.Extensions;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers
{
    [DataProvider(Category = "Files", IsDirectlyBindable = true, Name = "Excel Workbook", Copyright = "Developed by Wokhan Solutions", Icon = "/Resources/file-excel.png")]
    public class ExcelDataProvider : AbstractDataProvider, IExposedDataProvider
    {
        [ProviderParameter("File", IsFile = true)]
        public string File
        {
            get;
            set;
        }

        [ProviderParameter("Use headers")]
        public bool HasHeader
        {
            get;
            set;
        }

        public ExcelDataProvider() : base() { }

        public override void InvalidateColumnsCache(string repository)
        {
            if (cachedColumns.ContainsKey(repository))
            {
                cachedColumns.Remove(repository);
            }
        }

        ExcelDataSetConfiguration defaultConf = new ExcelDataSetConfiguration()
        {
            ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
            {
                UseHeaderRow = true
            }
        };

        private Dictionary<string, List<ColumnDescription>> cachedColumns = new Dictionary<string, List<ColumnDescription>>();
        public override List<ColumnDescription> GetColumns(string repository, IList<string> names = null)
        {
            if (!cachedColumns.ContainsKey(repository))
            {
                var rep = (string)GetDefaultRepositories()[repository];

                using (var reader = getReader())
                {
                    while (reader.Name != rep)
                    {
                        reader.NextResult();
                    }

                    cachedColumns.Add(repository, reader.AsDataSet(defaultConf).Tables[rep].Columns.Cast<DataColumn>()
                                                          .Select(c => new ColumnDescription() { Name = c.ColumnName, Type = c.DataType })
                                                          .ToList());
                }
            }

            return cachedColumns[repository];
        }
        public override bool AllowCustomRepository { get; } = false;

        public override Dictionary<string, object> GetDefaultRepositories()
        {
            var ret = new Dictionary<string, object>();
            using (var reader = getReader())
            {
                var res = reader.ResultsCount;
                for (var i = 0; i < res; i++)
                {
                    ret.Add(reader.Name + "[#" + (i + 1) + "]", reader.Name);
                }
            }

            return ret;
        }


        //public new IEnumerable GetDataDirect(string repository = null, IEnumerable<string> attributes = null)
        //{
        //    using (var reader = getReader())
        //    {
        //        if (attributes != null)
        //        {
        //            return reader.AsDataSet(defaultConf).Tables[repository].AsEnumerable().Select(r => attributes.Select(a => r[a]));
        //        }
        //        else
        //        {
        //            return reader.AsDataSet(defaultConf).Tables[repository].AsEnumerable().Select(r => r.ItemArray);
        //        }
        //    }
        //}

        public override bool Test(out string details)
        {
            if (!System.IO.File.Exists(File))
            {
                details = "File is not accessible.";
                return false;
            }

            details = "OK";
            return true;
        }

        private IExcelDataReader getReader()
        {
            IExcelDataReader ret;
            if (File.EndsWith("xlsx"))
            {
                ret = ExcelReaderFactory.CreateOpenXmlReader(new FileStream(File, FileMode.Open, FileAccess.Read));
            }
            else
            {
                ret = ExcelReaderFactory.CreateBinaryReader(new FileStream(File, FileMode.Open, FileAccess.Read));
            }

            return ret;
        }

        public override IQueryable<T> GetQueryable<T>(string repository, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null)
        {
            return _getdata<T>(repository).AsQueryable();
        }

        private IEnumerable<T> _getdata<T>(string repository)
        {
            var attrlst = GetColumns(repository).Select(c => c.Name).ToList();
            var rep = (string)GetDefaultRepositories()[repository];

            using (var reader = getReader())
            {
                while (reader.Name != rep)
                {
                    reader.NextResult();
                }

                /*if (attributes != null)
                {
                    var attrIdx = attributes.Select(a => attrlst.IndexOf(a)).ToArray();
                    while (reader.Read())
                    {
                        yield return attrIdx.Select(i => reader.GetValue(i)).ToArray().ToObject<T>(attributes);
                    }
                }
                else
                */
                {
                    while (reader.Read())
                    {
                        yield return Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i)).ToArray().ToObject<T>(attrlst.ToArray());
                    }
                }
            }
        }

        //public virtual DbSet<T> CATALOG { get; set; }
    }
}

