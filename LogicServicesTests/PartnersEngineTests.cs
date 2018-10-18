using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSON;
using Moq;
using JSON.PartnerRequests;
using MockHttpServer;
using System.IO;
using Google.Protobuf;

namespace LogicServices.Tests
{
	[TestClass()]
	public class PartnersEngineTests
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
		public void InitializeCrypto()
		{
			new CryptoEngine().loadCertificate("qs1.cert");

			if (!File.Exists("DBDump.sqlite"))
			{
				File.Copy("..\\..\\DBDump.sqlite", "DBDump.sqlite");
			}

			if (!File.Exists("QuickStream.ini"))
			{
				File.Copy("..\\..\\QuickStream.ini", "QuickStream.ini");
			}

			VerifyGenerateCert();

			new PartnersEngine().AddPartner("http://localhost:8081");
		}

		[TestMethod()]
		public void PrepareSignedMessageTestPositiveMessageVerify()
		{
			var data = "Hello, world!";

			var msg = new PartnersEngine().PrepareSignedMessage<QueueReadResponse>(new QueueReadResponse { Message = "Success", Success = true, Messages = new List<string> { data } });

			new CryptoEngine().loadCertificate("qs1.cert");

			if (!new CryptoEngine().verifyCertificate(msg.key, msg.certId, msg.cert).VerifyData(msg.data, msg.signature))
			{
				Assert.Fail("Failed verifying partner signed message");
			}
		}

		[TestMethod()]
		public void PrepareSignedMessageTestNegqativeMessageVerify()
		{
			var data = "Hello, world!";

			var msg = new PartnersEngine().PrepareSignedMessage<QueueReadResponse>(new QueueReadResponse { Message = "Success", Success = true, Messages = new List<string> { data } });

			new CryptoEngine().loadCertificate("qs1.cert");

			msg.data[0]++;

			if (new CryptoEngine().verifyCertificate(msg.key, msg.certId, msg.cert).VerifyData(msg.data, msg.signature))
			{
				Assert.Fail("Failed verifying partner signed message");
			}
		}

		[TestMethod()]
		public void PartnerJoinRequestTestSimpleEchoTest()
		{
			try
			{
				using (new MockServer(8081, "/partnerSync", (req, rsp, prm) => rsp.Content(new JSONSerializer<PartnerSyncMessage>()
					.Serialize(new PartnersEngine()
								.PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = new byte[0], Message = "Success", Partners = new string[0], Success = true })))))
				{
					var msg = new PartnerSyncRequestJoin { Address = "localhost:8081" };

					try
					{
						new PartnersEngine().PartnerJoinRequest(msg);
					}
					catch (Exception e)
					{
						Assert.Fail("Failed with: " + e.Message);
					}
				}
			}
			catch (Exception e)
			{

			}
		}

		[TestMethod()]
		public void PartnerJoinRequestTestPartnersListVariants()
		{
			using (new MockServer(8081, "/partnerSync", (req, rsp, prm) => rsp.Content(new JSONSerializer<PartnerSyncMessage>()
					.Serialize(new PartnersEngine()
								.PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = new byte[0], Message = "Success", Partners = new string[] { "http://localhost:8081", "localhost:8081" }, Success = true })))))
			{
				var msg = new PartnerSyncRequestJoin { Address = "localhost:8081" };

				try
				{
					new PartnersEngine().PartnerJoinRequest(msg);
				}
				catch (Exception e)
				{
					Assert.Fail("Failed with: " + e.Message);
				}
			}
		}

		[TestMethod()]
		public void PartnerJoinRequestTestEmptyMessage()
		{
			using (new MockServer(8081, "/partnerSync", (req, rsp, prm) => rsp.Content(new JSONSerializer<PartnerSyncMessage>()
					.Serialize(new PartnersEngine()
								.PrepareSignedMessage(new PartnerSyncResponseDBDump { Success = false })))))
			{
				try
				{
					new PartnersEngine().PartnerJoinRequest(new PartnerSyncRequestJoin { Address = "localhost:8081" });
				}
				catch (Exception e)
				{
					Assert.Fail("Failed with: " + e.Message);
				}
			}
		}

		[TestMethod()]
		public void PartnerJoinRequestTestSuccessfulMessage()
		{
			using (new MockServer(8081, "/partnerSync", (req, rsp, prm) => rsp.Content(new JSONSerializer<PartnerSyncMessage>()
					.Serialize(new PartnersEngine()
								.PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = new byte[0], Message = "Success", Partners = new string[] { "http://localhost:8081", "http://localhost:8082" }, Success = true })))))
			{
				try
				{
					new PartnersEngine().PartnerJoinRequest(new PartnerSyncRequestJoin { Address = "localhost:8081" });
				}
				catch (Exception e)
				{
					Assert.Fail("Failed with: " + e.Message);
				}
			}
		}

		[TestMethod()]
		public void PartnerJoinRequestTestDBDump()
		{
			/* Dump te DB */
			var dbFile = File.Open("DBDump.sqlite", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			var reader = new BinaryReader(dbFile);

			using (new MockServer(8081, "/partnerSync", (req, rsp, prm) => rsp.Content(new JSONSerializer<PartnerSyncMessage>()
					.Serialize(new PartnersEngine()
								.PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = reader.ReadBytes((int)dbFile.Length), Message = "Success", Partners = new string[] { "http://localhost:8081", "localhost:8081" }, Success = true })))))
			{
				try
				{
					new PartnersEngine().PartnerJoinRequest(new PartnerSyncRequestJoin { Address = "localhost:8081" });
				}
				catch (Exception e)
				{
					Assert.Fail("Failed with: " + e.Message);
				}
			}
		}

		[TestMethod()]
		public void PartnersUpdateRequestTest()
		{
			var dbFile = File.Open(Config<string>.GetInstance()["DB_Filename"], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			var reader = new BinaryReader(dbFile);

			using (new MockServer(8081, "/partnerSync", (req, rsp, prm) => rsp.Content(new JSONSerializer<PartnerSyncMessage>()
					.Serialize(new PartnersEngine()
								.PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = reader.ReadBytes((int)dbFile.Length), Message = "Success", Partners = new string[] { "http://localhost:8081", "localhost:8081" }, Success = true })))))
			{
				var errorNodes = new PartnersEngine().PartnersUpdateRequest<PartnerSyncRequestJoin>(new PartnerSyncRequestJoin { Address = "localhost:8081" });
				
				if (errorNodes.Count != 0)
				{
					Assert.Fail("Got errors from nodes: " + String.Join(" ", errorNodes));
				}
			}

			dbFile.Close();
		}
	}
}