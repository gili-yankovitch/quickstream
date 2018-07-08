using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.CodeFirst;

namespace DAL
{
	public class User : IEntity
	{
		[Autoincrement]
		[Key]
		public int Id { get; set; }

		[Required]
		public byte[] Key { get; set; }

		public string SessionKey { get; set; }

		public List<MsgQueue> Queues { get; set; }
	}
}
