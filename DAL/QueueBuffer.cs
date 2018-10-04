using SQLite.CodeFirst;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
	public class QueueBuffer : IEntity
	{
		[Autoincrement]
		[Key]
		public int Id { get; set; }

		[Required]
		public virtual User User { get; set; }

		[ForeignKey("User"), Column(Order = 0)]
		public int UserId { get; set; }

		[ForeignKey("User"), Column(Order = 1)]
		public int NodeId { get; set; }

		[Required]
		[ForeignKey("QueueId")]
		public virtual MsgQueue Queue { get; set; }
		public int QueueId { get; set; }

		[Required]
		public long Timestamp { get; set; }

		[Required]
		public string Data { get; set; }
	}
}
