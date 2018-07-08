using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
	public class Reader : IEntity
	{
		[Key]
		[Required]
		public int Id { get; set; }
		
		[Required]
		[ForeignKey("UserId")]
		public virtual User User { get; set; }
		public int UserId { get; set; }

		[Required]
		[ForeignKey("QueueId")]
		public virtual MsgQueue Queue { get; set; }
		public int QueueId { get; set; }

		[Required]
		public int Position { get; set; }
	}
}
