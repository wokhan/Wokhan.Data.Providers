using System;
using System.Collections.Generic;

namespace Wokhan.Data.Providers.Bases
{
    [Obsolete("Should be replaced by IQueryable Linq statements")]
    public class SearchOptions
    {
        public List<string> Attributes;
        public List<RelationDefinition> Relations = new List<RelationDefinition>();
        public int RelationsDepth;
        public string Filter = String.Empty;
        public List<string> SortOrders = new List<string>();

        public int TotalCount;
    }
}
