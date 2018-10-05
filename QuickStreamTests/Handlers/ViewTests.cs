using Google.Protobuf;
using JSON;
using LogicServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickStream.Handlers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickStream.Handlers.Tests
{
	[TestClass()]
	public class ViewTests
	{
		private Thread WebserverThread = null;

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

		private void ResetDB()
		{
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

		public ViewTests()
		{
			VerifyGenerateCert();

			WebserverThread = new Thread(() =>
			{
				try
				{
					/* Load certificate */
					new CryptoEngine().loadCertificate(Config<string>.GetInstance()["Certificate"]);

					/* Add preconfigured partners */
					foreach (var partner in Config<string[]>.GetInstance()["PARTNERS"])
					{
						if (partner == string.Empty)
							continue;

						new PartnersEngine().AddPartner(partner);
					}

					var server = new AsyncHTTPServer(8080);
					server.AddHandler("/testQuery", new TestQueryHandler());
					server.AddHandler("/createUser", new CreateUserHandler());
					server.AddHandler("/createQueue", new CreateQueueHandler());
					server.AddHandler("/partnerSync", new PartnerSyncHandler());
					server.AddHandler("/login", new LoginHandler());
					server.AddHandler("/queue", new QueueHandler());

					server.Start();
				}
				catch (Exception e)
				{
					Assert.Fail("Failed initializing: " + e.Message);
				}
			});

			WebserverThread.Start();
		}

		~ViewTests()
		{
			if (WebserverThread != null)
				WebserverThread.Abort();
		}

		[TestInitialize()]
		public void InitTest()
		{
			ResetDB();
		}

		private WebRequest Request(string url)
		{
			var request = WebRequest.Create(url);
			request.Method = "POST";
			request.ContentType = "text/json";

			return request;
		}

		private U JSONTransaction<T,U>(string url, T JSONObj)
		{
			var req = Request(url);

			try
			{
				new JSONSerializer<T>().Serialize(JSONObj, req.GetRequestStream()).Close();
			}
			catch (WebException e)
			{
				/* Node might be down */
				Assert.Fail(e.Message);
			}

			try
			{
				/* Get the response */
				return new JSONSerializer<U>().Deserialize(req.GetResponse().GetResponseStream());
			}
			catch (SerializationException e)
			{
				Assert.Fail(e.Message);
			}

			return default(U);
		}

		private UserCreateResponse RegisterNewUser(string Passphrase)
		{
			return JSONTransaction<UserCreateRequest, UserCreateResponse>("http://localhost:8080/createUser/", new UserCreateRequest { Key = Passphrase });
		}

		private SessionKeyResponse Login(int nodeId, int uid, string Passphrase)
		{
			return JSONTransaction<LoginRequest, SessionKeyResponse>("http://localhost:8080/login/", new LoginRequest { Id = uid, NodeId = nodeId, Key = Passphrase });
		}

		private BooleanResponse CreateQueue(int nodeId, int uid, string sessionKey, string name, List<List<int>> readers)
		{
			return JSONTransaction<QueueCreateRequest, BooleanResponse>("http://localhost:8080/createQueue/", new QueueCreateRequest { Id = uid, NodeId = nodeId, SessionKey = sessionKey, QueueName = name, Readers = readers });
		}

		private QueueReadResponse WriteQueue(int nodeId, int uid, string sessionKey, string name, string data)
		{
			return JSONTransaction<QueueRequest, QueueReadResponse>("http://localhost:8080/queue/" + nodeId.ToString() + "/" + uid.ToString() + "/" + name, new QueueRequest { Id = uid, NodeId = nodeId, SessionKey = sessionKey, Action = e_action.E_WRITE, Data = data });
		}

		[TestMethod()]
		public void CreateUserTest()
		{
			var uid0 = RegisterNewUser("Aa123456");

			if ((!uid0.Success) || (uid0.Id != 0))
				Assert.Fail("Unexpected result registering uid 0");
		}

		[TestMethod()]
		public void LoginTest()
		{
			string passphrase = "Aa123456";
			var uid0 = RegisterNewUser(passphrase);

			if ((!uid0.Success) || (uid0.Id != 0))
				Assert.Fail("Unexpected result registering uid 0");

			var session = Login(uid0.NodeId, uid0.Id, passphrase);

			if (!session.Success)
				Assert.Fail("Failed logging in");
		}

		[TestMethod()]
		public void LoginTestNegative()
		{
			string passphrase = "Aa123456";
			var uid0 = RegisterNewUser(passphrase);

			if ((!uid0.Success) || (uid0.Id != 0))
				Assert.Fail("Unexpected result registering uid 0");

			var session = Login(uid0.NodeId, uid0.Id, passphrase + "A");

			if (session.Success)
				Assert.Fail("Logged in despite wrong passphrase");
		}

		[TestMethod()]
		public void CreateQueueTest()
		{
			string passphrase = "Aa123456";
			var uid0 = RegisterNewUser(passphrase);

			if ((!uid0.Success) || (uid0.Id != 0))
				Assert.Fail("Unexpected result registering uid 0");

			var session = Login(uid0.NodeId, uid0.Id, passphrase);

			if (!session.Success)
				Assert.Fail("Failed logging in");

			var newQueue = CreateQueue(uid0.NodeId, uid0.Id, session.SessionKey, "A", new List<List<int>>());

			if (!newQueue.Success)
				Assert.Fail("Failed creating new queue");
		}

		[TestMethod()]
		public void CreateQueueWithReadersTest()
		{
			string passphrase = "Aa123456";
			var uid0 = RegisterNewUser(passphrase);

			if ((!uid0.Success) || (uid0.Id != 0))
				Assert.Fail("Unexpected result registering uid 0");

			var uid1 = RegisterNewUser(passphrase);

			if ((!uid1.Success) || (uid1.Id != 1))
				Assert.Fail("Unexpected result registering uid 0");

			var session = Login(uid0.NodeId, uid0.Id, passphrase);

			if (!session.Success)
				Assert.Fail("Failed logging in");

			var newQueue = CreateQueue(uid0.NodeId, uid0.Id, session.SessionKey, "A", new List<List<int>> { new List<int> { uid1.Id, 1 } });

			if (!newQueue.Success)
				Assert.Fail("Failed creating new queue");
		}

		[TestMethod()]
		public void CreateQueueWithNonexistingReadersTest()
		{
			string passphrase = "Aa123456";
			var uid0 = RegisterNewUser(passphrase);

			if ((!uid0.Success) || (uid0.Id != 0))
				Assert.Fail("Unexpected result registering uid 0");

			var uid1 = RegisterNewUser(passphrase);

			if ((!uid1.Success) || (uid1.Id != 1))
				Assert.Fail("Unexpected result registering uid 0");

			var session = Login(uid0.NodeId, uid0.Id, passphrase);

			if (!session.Success)
				Assert.Fail("Failed logging in");

			var newQueue = CreateQueue(uid0.NodeId, uid0.Id, session.SessionKey, "A", new List<List<int>> { new List<int> { uid1.Id, 2 } });

			if (!newQueue.Success)
				Assert.Fail("Failed creating new queue");
		}

		[TestMethod()]
		public void WriteQueue()
		{
			string passphrase = "Aa123456";
			var uid0 = RegisterNewUser(passphrase);

			if ((!uid0.Success) || (uid0.Id != 0))
				Assert.Fail("Unexpected result registering uid 0");

			var session = Login(uid0.NodeId, uid0.Id, passphrase);

			if (!session.Success)
				Assert.Fail("Failed logging in");

			var newQueue = CreateQueue(uid0.NodeId, uid0.Id, session.SessionKey, "A", new List<List<int>> {  });

			if (!newQueue.Success)
				Assert.Fail("Failed creating new queue");

			var writeResponse = WriteQueue(uid0.NodeId, uid0.Id, session.SessionKey, "A", "Hello, world!");

			if (!writeResponse.Success)
				Assert.Fail("Failed writing to my own queue");
		}
	}
}