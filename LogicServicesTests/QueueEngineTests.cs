using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LogicServices.Tests
{
	[TestClass()]
	public class QueueEngineTests
	{
		[TestInitialize()]
		public void InitializeTest()
		{
			var Filename = "QuickStream.sqlite";
			if (File.Exists(Filename))
				File.Delete(Filename);
		}

		[TestMethod()]
		public void CreateQueueTestNegativeNoUser()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid + 1, 0, "Hello, world!", new List<List<int>>());
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		[TestMethod()]
		public void CreateQueueTestNegativeNoNode()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid, 1, "Hello, world!", new List<List<int>>());
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		[TestMethod()]
		public void CreateQueueTest()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid, 0, "Hello, world!", new List<List<int>>());
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		[TestMethod()]
		public void CreateQueueTestNegativeNullReaders()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid, 0, "Hello, world!", null);

				Assert.Fail("Shouldn't create a queue with null readers value");
			}
			catch
			{

			}
		}

		[TestMethod()]
		public void CreateQueueTestNegativeNullName()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid, 0, null, new List<List<int>>());

				Assert.Fail("Shouldn't create a queue with null name value");
			}
			catch
			{

			}
		}

		[TestMethod()]
		public void CreateQueueTestNegativeNameExists()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>>());
				new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>>());

				Assert.Fail("Shouldn't create a queue with null name value");
			}
			catch
			{

			}
		}

		[TestMethod()]
		public void CreateQueueTestNegativeTooManyQueues()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				for (int i = 0; i < 32; ++i)
					new QueueEngine().CreateQueue(uid, 0, "QueueNum" + i.ToString(), new List<List<int>>());

				Assert.Fail("Shouldn't be able to create so many queues");
			}
			catch
			{

			}
		}

		[TestMethod()]
		public void CreateQueueTestNoSuchReader()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { 1, 1 } });
			}
			catch (Exception e)
			{
				Assert.Fail("Queue should just ignore nonexistent UIDs: " + e.Message);
			}
		}

		[TestMethod()]
		public void CreateQueueTestWithReader()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid1 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { 0, uid1 } });
			}
			catch (Exception e)
			{
				Assert.Fail("Queue should just ignore nonexistent UIDs: " + e.Message);
			}
		}

		[TestMethod()]
		public void CreateQueueTestWithReaders()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid1 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid2 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { 0, uid1 }, new List<int> { 0, uid2 } });
			}
			catch (Exception e)
			{
				Assert.Fail("Queue should just ignore nonexistent UIDs: " + e.Message);
			}
		}

		[TestMethod()]
		public void WriteBufferedQueueTest()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { 0, uid1 }, new List<int> { 0, uid2 } });

			try
			{
				new QueueEngine().WriteBufferedQueue(uid, 0, "A", "Hello, world!");
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		[TestMethod()]
		public void WriteBufferedQueueTestNegativeUID()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid1 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { 0, uid1 } });

			try
			{
				new QueueEngine().WriteBufferedQueue(uid1, 0, "A", "Hello, world!");
				Assert.Fail("Shouldn't be able to write to a different user's queue");
			}
			catch
			{

			}
		}

		[TestMethod()]
		public void WriteBufferedQueueTestNegativeNonexistentUID()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { });

			try
			{
				new QueueEngine().WriteBufferedQueue(uid + 1, 0, "A", "Hello, world!");
				Assert.Fail("Shouldn't be able to write to a different user's queue");
			}
			catch
			{

			}
		}

		[TestMethod()]
		public void WriteBufferedQueueTestNegativeOverloadData()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { });

			try
			{
				for (int i = 0; i < 1024 * 64; ++i)
				new QueueEngine().WriteBufferedQueue(uid + 1, 0, "AAAAAAAAAAAAAAAA", "Hello, world!");
				Assert.Fail("Shouldn't be able to write to a different user's queue");
			}
			catch
			{

			}
		}
	}
}