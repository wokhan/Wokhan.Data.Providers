﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;

namespace Wokhan.Data.Providers.Embedded
{
    public partial class RandomDataProvider
    {
        public class AddressBookData
        {
            public static readonly List<string> lastnames;
            public static readonly List<string> firstnames;
            public static readonly List<string> cities;
            public static readonly List<string> countries;

            private string GetRandomAdressData(Random rnd, List<string> reference)
            {
                return reference.ElementAt(rnd.Next(0, reference.Count));
            }

            public int RowId { get; private set; }

            [ColumnDescription(IsKey = true)]
            public string Lastname { get; private set; }

            [ColumnDescription(IsKey = true)]
            public string Firstname { get; private set; }

            public int Age { get; private set; }

            public string City { get; private set; }

            public string Country { get; private set; }

            Random rnd = new Random();

            static AddressBookData()
            {
                using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Wokhan.Data.Providers.Resources.Samples.AddressBookBase.csv")))
                {
                    var refdata = sr.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Split(';'));
                    lastnames = refdata.Select(r => r.ElementAtOrDefault(0)).ToList();
                    firstnames = refdata.Select(r => r.ElementAtOrDefault(1)).ToList();
                    cities = refdata.Select(r => r.ElementAtOrDefault(2)).ToList();
                    countries = refdata.Select(r => r.ElementAtOrDefault(3)).ToList();
                }
            }

            public AddressBookData(int i)
            {
                RowId = i;
                Lastname = GetRandomAdressData(rnd, lastnames);
                Firstname = GetRandomAdressData(rnd, firstnames);
                City = GetRandomAdressData(rnd, cities);
                Country = GetRandomAdressData(rnd, countries);
                Age = rnd.Next(1, 120);
            }
        }
    }
}
