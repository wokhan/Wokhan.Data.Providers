using System.Linq;

namespace Wokhan.Data.Providers.Bases.Database
{
    public interface IDynamicDbContext
    {
        string table { get; set; }
        string schema { get; set; }
        string basequery { get; set; }

        IQueryable GetSet();
    }
}
