using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers
{
    [DataProvider(Category = "Demo", Name = "Random data", Description = "Randomly generated data following simple settings.", Copyright = "Developed by Wokhan Solutions", Icon = "Resources/Providers/reload.png")]
    public class RandomDataProvider : AbstractDataProvider, IExposedDataProvider
    {
        [ProviderParameter("Number of items")]
        public int ItemsCount { get; set; } = 10000;

        [ProviderParameter("Keep data cached for the current session")]
        public bool KeepCache { get; set; } = true;

        //[ProviderParameter("Custom columns (will use random columns if left empty)")]
        //public Dictionary<string, Type> Columns { get; set; }
        public override bool AllowCustomRepository => false;

        private static string GetRandomString(Random rnd, int minLength, int maxLength)
        {
            var buffer = new byte[rnd.Next(minLength, maxLength)];
            rnd.NextBytes(buffer);

            return UTF8Encoding.UTF8.GetString(buffer.Select(b => (byte)(b % 127)).ToArray());
        }

        private Random rnd = new Random();

        private Dictionary<string, object> _defaultRepositories = new Dictionary<string, object>
        {
            ["Address book"] = "",
            ["Random numbers"] = ""
        };
        private Dictionary<string, Type> repositoryTypes = new Dictionary<string, Type>
        {
            ["Address book"] = typeof(AddressBookData),
            ["Random numbers"] = typeof(RandomDoubleData)
        };

        public override Dictionary<string, object> GetDefaultRepositories() => _defaultRepositories;

        public override Type GetDataType(string repository) => repositoryTypes[repository];

        private Dictionary<Type, IList> _caches = new Dictionary<Type, IList>();
        public override IQueryable<T> GetQueryable<T>(string repository, IList<Dictionary<string, string>> values = null, Dictionary<string, long> statisticsBag = null)
        {
            var type = GetDataType(repository);
            if (!_caches.TryGetValue(type, out var ret))
            {
                //var ctor = type.GetConstructor(new[] { typeof(Random), typeof(int) });
                ret = Enumerable.Range(0, ItemsCount)
                                .Select(i => (T)Activator.CreateInstance(type, rnd, i))
                                .ToList();

                if (KeepCache)
                {
                    _caches.Add(type, ret);
                }
            }

            return ret.Cast<T>().AsQueryable();
        }

        public override void InvalidateColumnsCache(string repository)
        {

        }

        public override bool Test(out string details)
        {
            details = "OK";
            return true;
        }

        public override List<ColumnDescription> GetColumns(string repository, IList<string> names = null)
        {
            return ColumnDescription.FromType(GetDataType(repository));
        }

        public class AddressBookData
        {
            public int RowId { get; private set; }

            [ColumnDescription(IsKey = true)]
            public string Lastname { get; private set; }

            [ColumnDescription(IsKey = true)]
            public string Firstname { get; private set; }

            public int Age { get; private set; }

            public string PhoneNumber { get; private set; }

            public AddressBookData(Random rnd, int i)
            {
                RowId = i;
                Lastname = GetRandomString(rnd, 3, 10);
                Firstname = GetRandomString(rnd, 3, 10);
                PhoneNumber = GetRandomString(rnd, 10, 10);
                Age = rnd.Next(1, 120);
            }
        }

        private class RandomDoubleData
        {
            [ColumnDescription(IsKey = true)]
            public int RowId { get; set; }
            public double Number { get; set; }

            public RandomDoubleData(Random rnd, int i)
            {
                RowId = i;
                Number = rnd.NextDouble();
            }
        }
    }
}
