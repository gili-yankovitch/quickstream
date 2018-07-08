using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using DAL;

namespace LogicServices
{
	public class QueueEngine
	{
		public static void CreateQueue(int userId, string queueName, List<int> readers)
		{
			using (var ctx = new MessagingContext())
			{
				var u = ctx.Users.First(user => user.Id == userId);

				if (u == null)
					throw new Exception("Invalid userId");

				if (u.Queues == null)
					u.Queues = new List<MsgQueue>();

				if (u.Queues.Find(queue => (queue.Name == queueName)) != null)
					throw new Exception("Invalid queue name: Already exists");

				/* Create a dictionary with mapping for everyone */
				var newQueue = new MsgQueue() { Name = queueName, TopMsgIdx = 0, Readers = new List<Reader>() };

				foreach (var id in readers)
				{
					var reader = ctx.Users.Find(id);

					/* Invalid reader Id */
					if (reader == null)
						continue;

					newQueue.Readers.Add(new Reader { User = reader, Position = newQueue.TopMsgIdx - 1 });
				}

				u.Queues.Add(newQueue);

				ctx.SaveChanges();
			}
		}

		public static void WriteQueue(int userId, string queueName, string Data)
		{
			using (var ctx = new MessagingContext())
			{
				var u = ctx.Users.Include(user => user.Queues).FirstOrDefault(user => user.Id == userId);

				if (u == null)
					throw new Exception("Invalid userId");

				var q = u.Queues.Find(queue => (queue.Name == queueName));

				if (q == null)
					throw new Exception("Invalid queue");

				q.Messages.Add(new Message { Content = Data, MsgIdx = q.TopMsgIdx++ });

				ctx.SaveChanges();
			}
		}

		public static List<string> ReadQueue(int requestingUser, int userId, string queueName, bool commit)
		{
			var messages = new List<string>();

			using (var ctx = new MessagingContext())
			{
				var u = ctx.Users.Include(user => user.Queues).FirstOrDefault(user => user.Id == userId);

				if (u == null)
					throw new Exception("Invalid userId");

				var reader = ctx.Users.Find(requestingUser);

				if (reader == null)
					throw new Exception("Invalid readerId");

				var q = u.Queues.Find(queue => (queue.Name == queueName));

				if (q == null)
					throw new Exception("Invalid queue");

				var r = q.Readers.Find(uReader => (uReader.UserId == requestingUser));

				if (r == null)
					throw new Exception("Permission denied: Invalid reader");

				int highestIndex = r.Position;

				foreach (Message m in q.Messages.FindAll(m => (m.MsgIdx > highestIndex)))
				{
					messages.Add(m.Content);

					if (m.MsgIdx > highestIndex)
						highestIndex = m.MsgIdx;
				}

				if (commit)
				{
					/* Update user commit message */
					r.Position = highestIndex;

					/* For now, remember messages only until the last user read them */
					q.Messages.RemoveAll(m => (m.MsgIdx <= q.Readers.Min(rUser => (rUser.Position))));
				}

				ctx.SaveChanges();
			}

			return messages;
		}
	}
}
