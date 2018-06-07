using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.CodeFirst;

namespace DAL
{
	public class Namespace
	{
		[Autoincrement]
		[Key]
		public uint Id { get; set; }

		public User Owner { get; set; }

		public virtual List<MsgQueue> Queues { get; set; }
	}
}
