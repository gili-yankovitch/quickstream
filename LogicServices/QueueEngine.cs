using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL;

namespace LogicServices
{
	public class QueueEngine : Singleton<QueueEngine>
	{
		public bool CreateQueue(int userId, string queueName)
		{
			using (var ctx = new MessagingContext())
			{
				var u = ctx.Users.Find(userId);

				if (u == null)
					return false;

				if (u.Queues.Find(queue => (queue.Name == queueName)) != null)
					return false;

				u.Queues.Add(new MsgQueue() {Name = queueName});

				ctx.SaveChanges();
			}

			return true;
		}
	}
}
