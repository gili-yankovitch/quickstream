using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.CodeFirst;
using System.Data.Entity.ModelConfiguration.Conventions;
namespace DAL
{
	public class MessagingContext : DbContext
	{
		public MessagingContext() : base("QuickStream.db") { }

		protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			ModelConfiguration.Configure(modelBuilder);
			var sqliteConnectionInitializer = new SqliteCreateDatabaseIfNotExists<MessagingContext>(modelBuilder);
			Database.SetInitializer(sqliteConnectionInitializer);
		}

		public DbSet<Namespace> Namespaces { get; set; }
		public DbSet<MsgQueue> Queues { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<Message> Messages { get; set; }

	}
}
