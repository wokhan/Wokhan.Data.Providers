using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Wokhan.Collections.Extensions;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers
{
    [DataProvider(Category = "Files", Name = "CSV", Description = "Allows to load data from CSV or DSV files.", Copyright = "Developed by Wokhan Solutions", Icon = "Resources/Providers/file-delimited.png")]
    public class CSVDataProvider : FileDataProvider, IExposedDataProvider
    {
        [ProviderParameter("Delimiter")]
        public string Delimiter { get; set; } = ";";

        [ProviderParameter("Use quotes to enclose fields")]
        public bool UseQuotes { get; set; } = true;

        [ProviderParameter("Source file contains header")]
        public bool Hasheader { get; set; } = true;

        [ProviderParameter("Merge all selected files")]
        public bool MergeFiles { get; set; } = true;

        public override string FileFilter
        {
            get { return "CSV files|*.csv"; }
            set { }
        }

        public CSVDataProvider() : base() { }

        public static new string GetFormatKey(List<object> srcAttributesCollection, object srcAttribute)
        {
            return srcAttributesCollection.IndexOf(srcAttribute).ToString();
        }

        private Dictionary<string, List<ColumnDescription>> _headers = new Dictionary<string, List<ColumnDescription>>();

        public override List<ColumnDescription> GetColumns(string repository, IList<string> names = null)
        {
            List<ColumnDescription> ret = null;

            lock (_headers)
            {
                if (!_headers.TryGetValue(repository, out ret))
                {
                    var csvParser = new TextFieldParser((string)Repositories[repository], this._encoding);
                    csvParser.Delimiters = new[] { this.Delimiter };
                    csvParser.HasFieldsEnclosedInQuotes = this.UseQuotes;

                    var fields = csvParser.ReadFields();
                    if (this.Hasheader)
                    {
                        ret = fields.Select(f => Regex.Replace(f, "[^a-zA-Z0-9]", "_"))
                                    .Select((f, i) => new
                                    {
                                        f = String.IsNullOrEmpty(f) ? "X" + i : f,
                                        i,
                                        cnt = fields.Count(ff => !String.IsNullOrEmpty(f) && ff == f)
                                    })
                                    .Select(s => new ColumnDescription() { Name = s.cnt > 1 ? s.f + s.i : s.f, Type = typeof(string) })
                                    .ToList();
                    }
                    else
                    {
                        var i = 0;
                        ret = fields.Select(f => new ColumnDescription() { Name = "X" + i++, Type = typeof(string) }).ToList();
                    }

                    csvParser.Close();

                    _headers.Add(repository, ret);
                }
            }

            return ret;
        }

        public override IQueryable<T> GetQueryable<T>(string repository, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null)
        {
            if (MergeFiles)
            {
                return this.Repositories.SelectMany(r => __GetTypedData<T>(r.Key, true)).AsQueryable();
            }
            else
            {
                return __GetTypedData<T>(repository, false);
            }
        }

        private IQueryable<T> __GetTypedData<T>(string repository, bool ignoreHeader) where T : class
        {
            var csvParser = new TextFieldParser((string)Repositories[repository], this._encoding);
            csvParser.Delimiters = new[] { this.Delimiter };
            csvParser.HasFieldsEnclosedInQuotes = this.UseQuotes;

            //try
            {
                if (this.Hasheader)
                {
                    csvParser.ReadLine();
                }

                var attrlst = this.GetColumns(repository).Select(c => c.Name).ToArray();

                return InnerGetData(csvParser, repository).Select(x => x.ToObject<T>(attrlst)).AsQueryable();
                //if (attributes != null && attributes.Any())
                //{
                //    var attrlst = attributes.ToArray();
                //    return InnerGetData(csvParser, repository, attributes).Select(x => x.ToObject<T>(attrlst)).AsQueryable();
                //}
                //else
                //{
                //    var attrlst = this.GetHeaders(repository).Keys.ToArray();
                //    return InnerGetData2(csvParser, repository, attrlst).Select(x => x.ToObject<T>(attrlst)).AsQueryable();
                //}
            }
            /*catch
            {
               throw;
            }
            finally
            {
                csvParser.Close();
            }*/

        }

        private IEnumerable<string[]> InnerGetData(TextFieldParser csvParser, string repository)
        {
            var headers = this.GetColumns(repository);
            int[] idx = null;
            int targetLength = headers.Count;
            /*if (attributes != null)
            {
                idx = attributes.Join(headers.Select((h, i) => new { h, i }), a => a, h => h.h.Name, (a, h) => h.i)
                                .ToArray();
                targetLength = idx.Length;
            }
            */

            string[] nxt = null;

            while (!csvParser.EndOfData)
            {
                string[] values = null;
                try
                {
                    values = csvParser.ReadFields();//nxt ?? csvParser.ReadFields();
                    while (!csvParser.EndOfData && values.Length < targetLength)
                    {
                        nxt = csvParser.ReadFields();
                        values[values.Length - 1] += "\r\n" + nxt.First();
                        values = values.Concat(nxt.Skip(1)).ToArray();
                    }

                    nxt = null;
                }
                catch
                {
                    values = new string[headers.Count];
                    values[0] = "Unable to parse line " + csvParser.ErrorLineNumber + ": " + csvParser.ErrorLine;
                }

                yield return idx == null ? values : idx.Select(i => i < values.Length ? values[i] : "[Out of bound]").ToArray();
            }
        }

        public override void InvalidateColumnsCache(string repository)
        {
            throw new NotImplementedException();
        }

        /*private IEnumerable<string[]> InnerGetData(TextFieldParser csvParser, string repository, IEnumerable<string> attributes)
        {
            var headers = this.GetHeaders(repository);
            var idx = headers.Select((g, i) => new { g.Key, i }).ToDictionary(gg => gg.Key, gg => gg.i);

            string[] nxt = null;
            while (!csvParser.EndOfData)
            {
                var values = nxt ?? csvParser.ReadFields();
                while (!csvParser.EndOfData && values.Length < headers.Count)
                {
                    nxt = csvParser.ReadFields();
                    values[values.Length - 1] += nxt.First();
                    values = values.Concat(nxt.Skip(1)).ToArray();
                }

                nxt = null;

                yield return attributes.Select(a => values[idx[a]]).ToArray();
            }
        }*/
    }
}
