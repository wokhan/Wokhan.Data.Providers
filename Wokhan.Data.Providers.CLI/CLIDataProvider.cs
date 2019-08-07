﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wokhan.Data.Providers.Bases;

namespace Wokhan.Data.Providers.CLI
{
    public class CLIDataProvider : DataProvider
    {
        public string Path { get; set; }
        public string Arguments { get; set; }

        public override Dictionary<string, string> MonitoringTypes => throw new NotImplementedException();

        public override List<ColumnDescription> GetColumns(string repository, IList<string> names = null)
        {
            throw new NotImplementedException();
        }

        public override void InvalidateColumnsCache(string repository)
        {
            throw new NotImplementedException();
        }

        public override bool Test(out string details)
        {
            throw new NotImplementedException();
        }

        protected override IQueryable<T> GetTypedData<T, TK>(string repository, IEnumerable<string> attributes, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null)
        {
            var p = new Process();
            string path = Path;
            string args = Arguments;
            /*if (values != null)
            {
                path = Regex.Replace(Path, @"\$([^\$]*)\$", m => values[int.TryParse(m.Groups[2].Value, out int res) ? res : 0][m.Groups[1].Value]);
                args = Regex.Replace(Arguments, @"\$([^\$]*)\$", m => values[int.TryParse(m.Groups[2].Value, out int res) ? res : 0][m.Groups[1].Value]);
            }*/
            p.StartInfo.FileName = path;
            p.StartInfo.Arguments = args;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.LoadUserProfile = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            //p.OutputDataReceived += (sender, args) => sb.Append(args.Data);

            var sw = Stopwatch.StartNew();

            p.Start();
            object output = p.StandardOutput.ReadToEnd();
            object error = p.StandardError.ReadToEnd();
            p.WaitForExit();

            sw.Stop();
            statisticsBag?.Add("ProcessDone", sw.ElapsedMilliseconds);

            return new[] { (T)output }.AsQueryable();
        }
    }
}