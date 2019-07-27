using System;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Linq.Expressions;

namespace Wokhan.Data.Providers.Bases.Database
{
    public partial class DynamicDbContext<T, TK> : DbContext, IDynamicDbContext
            where T : class
            where TK : class
    {
        /*  public class DbConfigurationWrapper : DbConfiguration
          {
              public DbConfigurationWrapper()
              {

              }
          }
*/
        private string[] keys;
        public string Table { get; set; }
        public string Schema { get; set; }
        public string BaseQuery { get; set; }

        public event Action<string> LogAction;

        public event StateChangeEventHandler ConnectionStateChange;

        static DynamicDbContext()
        {
            System.Data.Entity.Database.SetInitializer(new NullDatabaseInitializer<DynamicDbContext<T, TK>>());
        }

        public DynamicDbContext(DbConnection cstring, string schema, string table, params string[] keys)
            : base(cstring, true)
        {
            this.Table = table;
            this.keys = keys;
            this.Schema = schema;
            this.Database.Log += LogAction;
            this.Database.Connection.StateChange += ConnectionStateChange;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            this.Configuration.AutoDetectChangesEnabled = false;
            this.Configuration.ProxyCreationEnabled = false;

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.HasDefaultSchema(Schema);

            ParameterExpression param = Expression.Parameter(typeof(T));
            var keyExpression = Expression.Lambda<Func<T, long>>(Expression.Property(param, "__UID"), param);

            modelBuilder.Entity<T>().HasEntitySetName(Table)
                                    .ToTable("DYNAMICTABLE")
                                    .HasKey(keyExpression);
        }

        public IQueryable GetSet()
        {
            this.Database.ExecuteSqlCommand("ALTER SESSION SET NLS_SORT = UNICODE_BINARY");
            return this.Set<T>();
        }
    }

}
