using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers
{
    [DataProvider(Category = "Processes", Name = "Process output reader", Description = "Allows to retrieve data from a process standard output stream.", Copyright = "Developed by Wokhan Solutions", Icon = "/Resources/Providers/CSV.png")]
    public class CLIDataProvider : AbstractDataProvider, IExposedDataProvider
    {
        public string Path { get; set; }
        public string Arguments { get; set; }

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

        public override IQueryable<T> GetQueryable<T>(string repository, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null)
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