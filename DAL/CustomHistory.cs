using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.CodeFirst;

namespace DAL
{
	public class CustomHistory : IHistory
	{
		public int Id { get; set; }
		public string Hash { get; set; }
		public string Context { get; set; }
		public DateTime CreateDate { get; set; }
	}
}
