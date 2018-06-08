using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
	public class AutosavingContext : DbContext
	{
		public AutosavingContext(string conn) : base(conn) { }

		~AutosavingContext()
		{
			this.SaveChanges();
		}
	}
}
