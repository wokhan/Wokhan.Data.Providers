using System;
using System.Data;
using System.Linq;

namespace Wokhan.Data.Providers.Bases.Database
{
    public interface IDynamicDbContext
    {
        string Table { get; set; }
        string Schema { get; set; }
        string BaseQuery { get; set; }

        event Action<string> LogAction;

        event StateChangeEventHandler ConnectionStateChange;

        IQueryable GetSet();
    }
}
