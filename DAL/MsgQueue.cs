using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.CodeFirst;

namespace DAL
{
	public class MsgQueue : IEntity
	{
		[Autoincrement]
		[Key]
		public uint Id { get; set; }

		[Required]
		public string Name { get; set; }

		public virtual List<Message> Messages { get; set; }
		public virtual List<User> Readers { get; set; }
	}
}
