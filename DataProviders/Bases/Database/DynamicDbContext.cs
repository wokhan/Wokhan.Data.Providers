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
        public string table { get; set; }
        public string schema { get; set; }
        public string basequery { get; set; }

        static DynamicDbContext()
        {
            System.Data.Entity.Database.SetInitializer<DynamicDbContext<T, TK>>(new NullDatabaseInitializer<DynamicDbContext<T, TK>>());
        }

        public DynamicDbContext(DbConnection cstring, string schema, string table, params string[] keys)
            : base(cstring, true)
        {
            this.table = table;
            this.keys = keys;
            this.schema = schema;
            this.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
            this.Database.Connection.StateChange += Connection_StateChange;
        }

        private void Connection_StateChange(object sender, StateChangeEventArgs e)
        {
            /*if (e.OriginalState != ConnectionState.Open || e.CurrentState == ConnectionState.Open)
            {
                this.Database.ExecuteSqlCommand("ALTER SESSION SET NLS_COMP = BINARY");
            }*/
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            this.Configuration.AutoDetectChangesEnabled = false;
            this.Configuration.ProxyCreationEnabled = false;

            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.HasDefaultSchema(schema);

            ParameterExpression param = Expression.Parameter(typeof(T));
            var keyExpression = Expression.Lambda<Func<T, long>>(Expression.Property(param, "__UID"), param);

            modelBuilder.Entity<T>().HasEntitySetName(table)
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
