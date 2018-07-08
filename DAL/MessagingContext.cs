using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.CodeFirst;
using System.Data.Entity.ModelConfiguration.Conventions;
namespace DAL
{
	public class MessagingContext : AutosavingContext
	{
		public MessagingContext() : base("QuickStream")
		{
			Configuration.ProxyCreationEnabled = true;
			Configuration.LazyLoadingEnabled = true;
		}

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			ModelConfiguration.Configure(modelBuilder);
			var sqliteConnectionInitializer = new SqliteDropCreateDatabaseWhenModelChanges<MessagingContext>(modelBuilder);
			Database.SetInitializer(sqliteConnectionInitializer);
		}

		public DbSet<Message> Messages { get; set; }
		public DbSet<MsgQueue> MsgQueues { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<Reader> Readers { get; set; }
	}
}
