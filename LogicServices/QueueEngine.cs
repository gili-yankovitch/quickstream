using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Text;
using System.Threading.Tasks;
using DAL;
using System.Threading;

namespace LogicServices
{
	public class QueueEngine
	{
		/* This thread ensures that messages aggregated from various partners are added to the queue in the correct order */
		public static void HandleQueueBuffer()
		{
			/* Iterate over all the queues and look for messages that expired their indexing period and should enter the queue */
			using (var ctx = new MessagingContext())
			{
				foreach (var q in ctx.MsgQueues)
				{
					var messages = ctx.QueueBuffer.Where(m => (m.QueueId == q.Id));

					var messageList = new List<QueueBuffer>();

					foreach (var m in messages)
					{
						if (DateTime.Now.Ticks > m.Timestamp.Ticks + Config<int>.GetInstance()["QUEUE_GRACE_PERIOD"])
						{
							/* Add this message to the queue. We assume that by now all slaves have synced */
							messageList.Add(m);
						}
					}

					/* Sort by time of arrival */
					messageList.Sort((a, b) => ((int)(a.Timestamp.Ticks - b.Timestamp.Ticks)));

					foreach (var m in messageList)
					{
						WriteQueue(ctx, m.UserId, m.NodeId, m.QueueId, m.Data, m.Timestamp);

						ctx.QueueBuffer.Remove(m);

						/* Important to save here for consistancy */
						ctx.SaveChanges();
					}
				}
			}
		}

		public static void CreateQueue(int userId, int nodeId, string queueName, List<List<int>> readers)
		{
			using (var ctx = new MessagingContext())
			{
				var u = ctx.Users.Include(user => user.Queues).First(user => user.Id == userId && user.IssueNodeId == nodeId);

				if (u == null)
					throw new Exception("Invalid userId");

				if (u.Queues == null)
					u.Queues = new List<MsgQueue>();

				if (u.Queues.Find(queue => (queue.Name == queueName)) != null)
					throw new Exception("Invalid queue name: Already exists");

				/* Create a dictionary with mapping for everyone */
				var newQueue = new MsgQueue() { Name = queueName, TopMsgIdx = 0, Readers = new List<Reader>() };

				foreach (var uid in readers)
				{
					var reader = ctx.Users.Find(uid[0], uid[1]);

					/* Invalid reader Id */
					if (reader == null)
						continue;

					var r = new Reader { User = reader, Position = newQueue.TopMsgIdx - 1, NodeId = nodeId, UserId = userId, Queue = newQueue };

					ctx.Readers.Add(r);
					newQueue.Readers.Add(r);
				}

				u.Queues.Add(newQueue);

				ctx.SaveChanges();
			}
		}

		private static bool IsEnoughQueueSpace(int bufferedSize, int queueContentSize, string Data)
		{
			/* Calculate the queue size so we know if it actually enters the local queue */
			return bufferedSize + queueContentSize + Data.Length < Config<int>.GetInstance()["QUEUE_DATA_SIZE"];
		}

		private static void CleanOldMessageQueues(MessagingContext ctx, MsgQueue q)
		{
			/* Clean old messages */
			q.Messages.RemoveAll(m => (m.Timestamp.Ticks + Config<int>.GetInstance()["QUEUE_MESSAGE_MAX_AGE"] < DateTime.Now.Ticks));

			ctx.SaveChanges();
		}

		private static void WriteQueue(MessagingContext ctx, int userId, int nodeId, int queueId, string Data, DateTime Timestamp)
		{
			var u = ctx.Users.Include(user => user.Queues).FirstOrDefault(user => user.Id == userId && user.IssueNodeId == nodeId);

			if (u == null)
				throw new Exception("Invalid userId");

			var q = u.Queues.Find(queue => (queue.Id == queueId));

			if (q == null)
				throw new Exception("Invalid queue");

			q.Messages.Add(new Message { Content = Data, MsgIdx = q.TopMsgIdx++, Timestamp = Timestamp });
		}

		public static bool WriteBufferedQueue(int userId, int nodeId, string queueName, string Data)
		{
			return WriteBufferedQueue(userId, nodeId, queueName, Data, DateTime.Now);
		}

		public static bool WriteBufferedQueue(int userId, int nodeId, string queueName, string Data, DateTime Timestamp)
		{
			using (var ctx = new MessagingContext())
			{
				var u = ctx.Users.Include(user => user.Queues).FirstOrDefault(user => user.Id == userId && user.IssueNodeId == nodeId);

				if (u == null)
					throw new Exception("Invalid userId");

				var q = u.Queues.Find(queue => (queue.Name == queueName));

				if (q == null)
					throw new Exception("Invalid queue");

				/* Clean old messages */
				CleanOldMessageQueues(ctx, q);

				var queuedMessagesSize = ctx.QueueBuffer.Where(qb => qb.QueueId == q.Id).Select(qb => qb.Data).ToList().Sum(d => d.Length);

				var queueContentLength = q.Messages.Select(m => m.Content).ToList().Sum(m => m.Length);

				if (!IsEnoughQueueSpace(queuedMessagesSize, queueContentLength, Data))
					return false;

				ctx.QueueBuffer.Add(new QueueBuffer { User = u, Queue = q, Timestamp = Timestamp, Data = Data });

				ctx.SaveChanges();
			}

			return true;
		}

		public static void CommitQueue(int userId, int nodeId, int readerId, int readerNodeId, string queueName)
		{
			using (var ctx = new MessagingContext())
			{
				//var r = ctx.Readers.FirstOrDefault(reader => reader.UserId == userId && reader.NodeId == nodeId);
				var r = ctx.Readers.FirstOrDefault(reader => reader.UserId == readerId && reader.NodeId == readerNodeId);

				/* For now, remember messages only until the last user read them */
				var q = ctx.MsgQueues.FirstOrDefault(queue => queue.Name == queueName && queue.UserId == userId && queue.NodeId == nodeId);
				
				int highestIndex = ctx.Readers.FirstOrDefault(reader => reader.UserId == readerId && reader.NodeId == readerNodeId).Position;

				foreach (Message m in q.Messages.FindAll(m => (m.MsgIdx > highestIndex)))
				{
					if (m.MsgIdx > highestIndex)
						highestIndex = m.MsgIdx;
				}

				r.Position = highestIndex;
				q.Messages.RemoveAll(m => (m.MsgIdx <= q.Readers.Min(rUser => (rUser.Position))));

				ctx.SaveChanges();
			}
		}

		public static List<string> ReadQueue(int requestingUser, int userId, int nodeId, string queueName, bool commit)
		{
			var messages = new List<string>();

			using (var ctx = new MessagingContext())
			{
				var u = ctx.Users.Include(user => user.Queues).FirstOrDefault(user => user.Id == userId && user.IssueNodeId == nodeId);

				if (u == null)
					throw new Exception("Invalid userId");

				var reader = ctx.Users.Find(requestingUser, nodeId);

				if (reader == null)
					throw new Exception("Invalid readerId");

				var q = u.Queues.Find(queue => (queue.Name == queueName));

				if (q == null)
					throw new Exception("Invalid queue");

				/* Clean old messages */
				CleanOldMessageQueues(ctx, q);

				var r = q.Readers.Find(uReader => (uReader.UserId == requestingUser && uReader.NodeId == nodeId));

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
