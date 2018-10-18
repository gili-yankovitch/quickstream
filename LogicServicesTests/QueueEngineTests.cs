using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using DAL;
using System.Data.SQLite;
using System.Data;
using Google.Protobuf;

namespace LogicServices.Tests
{
	[TestClass()]
	public class QueueEngineTests
	{
		private void VerifyGenerateCert()
		{
			if (!File.Exists(Config<string>.GetInstance()["Certificate"]))
			{
				var pbMasterKeyPair = CertGen.Program.GenerateKeyPair();
				var pbKeyPair = CertGen.Program.GenerateKeyPair();

				var pbCertFile = new PBCertFile();

				/* Copy public master key */
				pbCertFile.MasterPublic = new PBPublicKey();
				pbCertFile.MasterPublic.PublicKey = pbMasterKeyPair.PublicKey.PublicKey;
				pbCertFile.Cert = CertGen.Program.SignCertificate(Config<string>.GetInstance()["Certificate"], pbMasterKeyPair, pbKeyPair);
				pbCertFile.Keys = pbKeyPair;

				using (var fs = File.Open(Config<string>.GetInstance()["Certificate"], FileMode.Create))
				{
					pbCertFile.WriteTo(fs);
				}
			}
		}

		[TestInitialize()]
		public void InitializeTest()
		{
			if (!File.Exists("DBDump.sqlite"))
			{
				File.Copy("..\\..\\DBDump.sqlite", "DBDump.sqlite");
			}

			if (!File.Exists("QuickStream.ini"))
			{
				File.Copy("..\\..\\QuickStream.ini", "QuickStream.ini");
			}

			VerifyGenerateCert();

			Config<long>.GetInstance()["TIMEZONE_CORRECTION"] = 0;

			using (SQLiteConnection connect = new SQLiteConnection(@"Data Source=QuickStream.sqlite"))
			{
				connect.Open();

				foreach (var table in new string[] { "Users", "Readers", "Messages", "Queues", "QueueBuffers" })
				{
					using (SQLiteCommand fmd = connect.CreateCommand())
					{
						fmd.CommandText = @"DELETE FROM " + table;
						fmd.CommandType = CommandType.Text;
						try
						{
							fmd.ExecuteNonQuery();
						}
						catch (Exception e)
						{

						}
					}
				}
			}
		}

		[TestMethod()]
		public void CreateQueueTestNegativeNoUser()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid + 1, 0, "Hello, world!", new List<List<int>>());

				Assert.Fail("Cannot succeed. No such user.");
			}
			catch (Exception e)
			{
				
			}
		}

		[TestMethod()]
		public void CreateQueueTestNegativeNoNode()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			try
			{
				new QueueEngine().CreateQueue(uid, 1, "Hello, world!", new List<List<int>>());

				Assert.Fail("Cannot succeed. User does not exist on node 1");
			}
			catch (Exception e)
			{
				
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
				new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { uid1, 0 } });
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
				new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { uid1, 0 }, new List<int> { uid2, 0 } });
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

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>>());

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

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { uid1, 0 } });

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

		[TestMethod()]
		public void ReadQueueTestNegativeUnpermittedUser()
		{
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid1 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { uid1, 0 } });

			try
			{
				new QueueEngine().WriteBufferedQueue(uid, 0, "A", "Hello, world!");
				new QueueEngine().ReadQueue(uid1 + 1, uid, 0, "A", false);

				Assert.Fail("Shouldn't be able to read with non-permitted uid");
			}
			catch
			{

			}
		}

		[TestMethod()]
		public void ReadQueueTest()
		{
			var msg = "Hello, world!";
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid1 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { uid1, 0 } });

			try
			{
				new QueueEngine().WriteBufferedQueue(uid, 0, "A", msg);

				/* Wait for grace period */
				Thread.Sleep(Config<int>.GetInstance()["QUEUE_GRACE_PERIOD"] * 10);
				new QueueEngine().HandleQueueBuffer();

				var messages = new QueueEngine().ReadQueue(uid1, uid, 0, "A", false);

				if (messages.Count != 1)
					Assert.Fail("Invalid number of read messages");

				if (messages[0] != msg)
					Assert.Fail("Invalid read message");
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		[TestMethod()]
		public void ReadQueueTestNegativeNoGracePeriod()
		{
			var msg = "Hello, world!";
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid1 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { uid1, 0 } });

			try
			{
				new QueueEngine().WriteBufferedQueue(uid, 0, "A", msg);

				var messages = new QueueEngine().ReadQueue(uid1, uid, 0, "A", false);

				if (messages.Count != 0)
					Assert.Fail("Invalid number of read messages");
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		[TestMethod()]
		public void ReadQueueTestNoCommit()
		{
			var msg = "Hello, world!";
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid1 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { uid1, 0 } });

			try
			{
				new QueueEngine().WriteBufferedQueue(uid, 0, "A", msg);

				/* Wait for grace period */
				Thread.Sleep(Config<int>.GetInstance()["QUEUE_GRACE_PERIOD"]);
				new QueueEngine().HandleQueueBuffer();

				var messages = new QueueEngine().ReadQueue(uid1, uid, 0, "A", false);

				if (messages.Count != 1)
					Assert.Fail("Invalid number of read messages");

				if (messages[0] != msg)
					Assert.Fail("Invalid read message");

				messages = new QueueEngine().ReadQueue(uid1, uid, 0, "A", false);

				if (messages.Count != 1)
					Assert.Fail("Invalid number of read messages");

				if (messages[0] != msg)
					Assert.Fail("Invalid read message");
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		[TestMethod()]
		public void ReadQueueTestCommit()
		{
			var msg = "Hello, world!";
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid1 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { uid1, 0 } });

			try
			{
				new QueueEngine().WriteBufferedQueue(uid, 0, "A", msg);

				/* Wait for grace period */
				Thread.Sleep(Config<int>.GetInstance()["QUEUE_GRACE_PERIOD"]);
				new QueueEngine().HandleQueueBuffer();

				var messages = new QueueEngine().ReadQueue(uid1, uid, 0, "A", true);

				if (messages.Count != 1)
					Assert.Fail("Invalid number of read messages");

				if (messages[0] != msg)
					Assert.Fail("Invalid read message");

				messages = new QueueEngine().ReadQueue(uid1, uid, 0, "A", true);

				if (messages.Count != 0)
					Assert.Fail("Invalid number of read messages");
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}

		[TestMethod()]
		public void ReadQueueTestCommitMultipleUsers()
		{
			var msg = "Hello, world!";
			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid1 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var uid2 = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			new QueueEngine().CreateQueue(uid, 0, "A", new List<List<int>> { new List<int> { uid1, 0 }, new List<int> { uid2, 0 } });

			try
			{
				new QueueEngine().WriteBufferedQueue(uid, 0, "A", msg);

				/* Wait for grace period */
				Thread.Sleep(Config<int>.GetInstance()["QUEUE_GRACE_PERIOD"]);
				new QueueEngine().HandleQueueBuffer();

				var messages = new QueueEngine().ReadQueue(uid1, uid, 0, "A", true);

				if (messages.Count != 1)
					Assert.Fail("Invalid number of read messages");

				if (messages[0] != msg)
					Assert.Fail("Invalid read message");

				messages = new QueueEngine().ReadQueue(uid1, uid, 0, "A", true);

				if (messages.Count != 0)
					Assert.Fail("Invalid number of read messages");

				messages = new QueueEngine().ReadQueue(uid2, uid, 0, "A", true);

				if (messages.Count != 1)
					Assert.Fail("Invalid number of read messages");

				if (messages[0] != msg)
					Assert.Fail("Invalid read message");

				messages = new QueueEngine().ReadQueue(uid2, uid, 0, "A", true);

				if (messages.Count != 0)
					Assert.Fail("Invalid number of read messages");
			}
			catch (Exception e)
			{
				Assert.Fail(e.Message);
			}
		}
	}
}