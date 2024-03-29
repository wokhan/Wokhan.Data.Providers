﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Wokhan.Data.Providers.Attributes;
using Wokhan.Data.Providers.Bases;
using Wokhan.Data.Providers.Contracts;

namespace Wokhan.Data.Providers.Embedded
{
    /// <summary>
    /// A random data provider with two flavors: fake address book, and a random numbers collection.
    /// Mainly used for testing.
    /// </summary>
    [DataProvider(Category = "Demo", Name = "Random data", Description = "Randomly generated data following simple settings.", Copyright = "Developed by Wokhan Solutions", Icon = "Resources/Providers/reload.png")]
    public partial class RandomDataProvider : AbstractDataProvider, IExposedDataProvider
    {
        /// <summary>
        /// Number of items to generate
        /// </summary>
        [ProviderParameter("Number of items")]
        public int ItemsCount { get; set; } = 10000;

        /// <summary>
        /// Indicates if generated data should be kept and reused for the current session
        /// </summary>
        [ProviderParameter("Keep data cached for the current session")]
        public bool KeepCache { get; set; } = true;

        //[ProviderParameter("Custom columns (will use random columns if left empty)")]
        //public Dictionary<string, Type> Columns { get; set; }
        public override bool AllowCustomRepository => false;

        /// <summary>
        /// Minimum response delay (to simulate slow response rate). [Default = 0ms]
        /// </summary>
        [ProviderParameter("Minimum response delay (to simulate slow response rate). [Default = 0ms]")]
        public int MinDelay { get; set; } = 0;

        /// <summary>
        /// Maximum response delay (to simulate slow response rate). [Default = 200ms]
        /// </summary>
        [ProviderParameter("Maximum response delay (to simulate slow response rate). [Default = 200ms]")]
        public int MaxDelay { get; set; } = 200;

        //private static string GetRandomString(Random rnd, int minLength, int maxLength)
        //{
        //    var buffer = new byte[rnd.Next(minLength, maxLength)];
        //    rnd.NextBytes(buffer);

        //    return UTF8Encoding.UTF8.GetString(buffer.Select(b => (byte)(b % 127)).ToArray());
        //}

        public const string ADDRESS_BOOK = "Address book";
        public const string RANDOM_DOUBLES = "Random numbers";

        private readonly Random rnd = new Random();

        private Dictionary<string, object> _defaultRepositories = new Dictionary<string, object>
        {
            [ADDRESS_BOOK] = "",
            [RANDOM_DOUBLES] = ""
        };
        private Dictionary<string, Type> repositoryTypes = new Dictionary<string, Type>
        {
            [ADDRESS_BOOK] = typeof(AddressBookData),
            [RANDOM_DOUBLES] = typeof(RandomDoubleData)
        };

        public override Dictionary<string, object> GetDefaultRepositories() => _defaultRepositories;

        public override Type GetDataType(string repository) => repositoryTypes[repository];

        private Dictionary<Type, IList> _caches = new Dictionary<Type, IList>();

        public override IQueryable<T> GetQueryable<T>(string? repository, IList<Dictionary<string, string>>? values = null, Dictionary<string, long>? statisticsBag = null)
        {
            var type = GetDataType(repository);
            if (!_caches.TryGetValue(type, out var data))
            {
                //var ctor = type.GetConstructor(new[] { typeof(Random), typeof(int) });
                data = Enumerable.Range(0, ItemsCount)
                                .Select(i => (T)Activator.CreateInstance(type, i))
                                .ToList();

                if (KeepCache)
                {
                    _caches.Add(type, data);
                }
            }

            return new DelayedEnumerableQuery<T>(data.Cast<T>(), MinDelay, MaxDelay);
        }

        public override void InvalidateColumnsCache(string repository)
        {

        }

        public override bool Test(out string details)
        {
            details = "OK";
            return true;
        }

        public override List<ColumnDescription> GetColumns(string? repository, IList<string>? names = null)
        {
            return ColumnDescription.FromType(GetDataType(repository));
        }
    }
}
