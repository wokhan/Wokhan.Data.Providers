using System;
using System.Collections.Generic;
using System.Text;
using Wokhan.Data.Providers.Attributes;

namespace Wokhan.Data.Providers.Bases
{
    public abstract class FileDataProvider : AbstractDataProvider
    {
        protected Encoding _encoding = UTF8Encoding.UTF8;
        [ProviderParameter("Encoding", false, typeof(FileDataProvider), "GetEncoding")]
        public string Encoding
        {
            get { return _encoding.WebName.ToString(); }
            set { _encoding = UTF8Encoding.GetEncoding(value); }
        }

        public virtual string FileFilter
        {
            get { return "All files|*.*"; }
            set { }
        }

        /*public new string[] RepositoriesColumnNames
        {
            get { return new[] { "Identifier", "Full path" }; }
            set { }
        }*/
        
        public static Dictionary<string, string> GetEncoding()
        {
            return new Dictionary<string, string> {
                { System.Text.Encoding.UTF8.WebName, System.Text.Encoding.UTF8.EncodingName },
                { System.Text.Encoding.ASCII.WebName, System.Text.Encoding.ASCII.EncodingName },
                { System.Text.Encoding.BigEndianUnicode.WebName, System.Text.Encoding.BigEndianUnicode.EncodingName },
                { System.Text.Encoding.Unicode.WebName, System.Text.Encoding.Unicode.EncodingName },
                { System.Text.Encoding.Default.WebName, System.Text.Encoding.Default.EncodingName }
            };
        }

        public override bool Test(out string details)
        {
            details = "OK";
            return true;
        }
    }
}
