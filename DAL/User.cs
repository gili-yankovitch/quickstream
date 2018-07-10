using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite.CodeFirst;

namespace DAL
{
	public class User : IEntity
	{
		[Key, Autoincrement, Column(Order = 0)]
		public int Id { get; set; }

		[Key, Column(Order = 1)]
		public int IssueNodeId { get; set; }

		[Required]
		public byte[] Key { get; set; }

		public string SessionKey { get; set; }

		public List<MsgQueue> Queues { get; set; }
	}
}
