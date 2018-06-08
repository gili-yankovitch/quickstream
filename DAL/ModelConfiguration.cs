using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
	class ModelConfiguration
	{
		public static void Configure(DbModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Message>().ToTable("Messages");
			modelBuilder.Entity<MsgQueue>().ToTable("Queues");
			modelBuilder.Entity<User>().ToTable("Users");
		}
	}
}
