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
	public class Message : IEntity
	{
		[Autoincrement]
		[Key]
		public int Id { get; set; }

		[Required]
		public int MsgIdx { get; set; }

		[Required]
		public string Content { get; set; }

		[Required]
		public DateTime Timestamp { get; set; }

		[Required]
		[ForeignKey("QueueId")]
		public virtual MsgQueue Queue { get; set; }
		public int QueueId { get; set; }
	}
}
