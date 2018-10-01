﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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

namespace LogicServices.Tests
{
	[TestClass()]
	public class PartnersEngineTests
	{
		[TestMethod()]
		public void PrepareSignedMessageTestPositiveMessageVerify()
		{
			var data = "Hello, world!";

			new CryptoEngine().loadCertificate("qs0.cert");

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

			new CryptoEngine().loadCertificate("qs0.cert");

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
			using (new MockServer(8081, "", (req, rsp, prm) => ""))
			{
				var msg = new PartnerSyncRequestJoin { Address = "localhost:8081" };

				var RequestMock = new Mock<PartnersEngine>();

				var SerializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();
				SerializeMock.Setup(x => x.Serialize(It.IsAny<PartnerSyncMessage>())).Returns(new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(msg)));

				var DeserializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();

				new PartnersEngine().AddPartner("http://localhost:8081");
				DeserializeMock.Setup(x => x.Deserialize(It.IsAny<byte[]>())).Returns(new PartnersEngine().PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = new byte[0], Message = "Success", Partners = { }, Success = true }));

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
		public void PartnerJoinRequestTestPartnersListVariants()
		{
			using (new MockServer(8081, "", (req, rsp, prm) => ""))
			{
				var msg = new PartnerSyncRequestJoin { Address = "localhost:8081" };

				var RequestMock = new Mock<PartnersEngine>();

				var SerializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();
				SerializeMock.Setup(x => x.Serialize(It.IsAny<PartnerSyncMessage>())).Returns(new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(msg)));

				var DeserializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();

				new PartnersEngine().AddPartner("http://localhost:8081");
				DeserializeMock.Setup(x => x.Deserialize(It.IsAny<byte[]>())).Returns(new PartnersEngine().PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = new byte[0], Message = "Success", Partners = new string[] { "http://localhost:8081", "localhost:8081" }, Success = true }));

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
			using (new MockServer(8081, "", (req, rsp, prm) => ""))
			{
				var msg = new PartnerSyncRequestJoin { Address = "localhost:8081" };

				var RequestMock = new Mock<PartnersEngine>();

				var SerializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();
				SerializeMock.Setup(x => x.Serialize(It.IsAny<PartnerSyncMessage>())).Returns(new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(msg)));

				var DeserializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();

				new PartnersEngine().AddPartner("http://localhost:8081");
				DeserializeMock.Setup(x => x.Deserialize(It.IsAny<byte[]>())).Returns(new PartnersEngine().PrepareSignedMessage(new PartnerSyncResponseDBDump { Success = false }));

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
		public void PartnerJoinRequestTestSuccessfulMessage()
		{
			using (new MockServer(8081, "", (req, rsp, prm) => ""))
			{
				var msg = new PartnerSyncRequestJoin { Address = "localhost:8081" };

				var RequestMock = new Mock<PartnersEngine>();

				var SerializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();
				SerializeMock.Setup(x => x.Serialize(It.IsAny<PartnerSyncMessage>())).Returns(new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(msg)));

				var DeserializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();

				new PartnersEngine().AddPartner("http://localhost:8081");
				DeserializeMock.Setup(x => x.Deserialize(It.IsAny<byte[]>())).Returns(new PartnersEngine().PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = new byte[0], Message = "Success", Partners = new string[] { "http://localhost:8081", "localhost:8081" }, Success = true }));

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
		public void PartnerJoinRequestTestDBDump()
		{
			using (new MockServer(8081, "", (req, rsp, prm) => ""))
			{
				var msg = new PartnerSyncRequestJoin { Address = "localhost:8081" };

				var RequestMock = new Mock<PartnersEngine>();

				var SerializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();
				SerializeMock.Setup(x => x.Serialize(It.IsAny<PartnerSyncMessage>())).Returns(new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(msg)));

				var DeserializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();

				new PartnersEngine().AddPartner("http://localhost:8081");

				/* Dump te DB */
				var dbFile = File.Open("DBDump.sqlite", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

				using (var reader = new BinaryReader(dbFile))
				{
					DeserializeMock.Setup(x => x.Deserialize(It.IsAny<byte[]>())).Returns(new PartnersEngine().PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = reader.ReadBytes((int)dbFile.Length), Message = "Success", Partners = new string[] { "http://localhost:8081", "localhost:8081" }, Success = true }));
				}

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
		public void PartnersUpdateRequestTest()
		{
			using (new MockServer(8081, "", (req, rsp, prm) => ""))
			{
				var msg = new PartnerSyncRequestJoin { Address = "localhost:8081" };

				var RequestMock = new Mock<PartnersEngine>();

				var SerializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();
				SerializeMock.Setup(x => x.Serialize(It.IsAny<PartnerSyncMessage>())).Returns(new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(msg)));

				var DeserializeMock = new Mock<JSONSerializer<PartnerSyncMessage>>();

				new PartnersEngine().AddPartner("http://localhost:8081");

				/* Dump te DB */
				var dbFile = File.Open(Config<string>.GetInstance()["DB_Filename"], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

				using (var reader = new BinaryReader(dbFile))
				{
					DeserializeMock.Setup(x => x.Deserialize(It.IsAny<byte[]>())).Returns(new PartnersEngine().PrepareSignedMessage(new PartnerSyncResponseDBDump { DBDump = reader.ReadBytes((int)dbFile.Length), Message = "Success", Partners = new string[] { "http://localhost:8081", "localhost:8081" }, Success = true }));
				}

				var errorNodes = new PartnersEngine().PartnersUpdateRequest<PartnerSyncRequestJoin>(new PartnerSyncRequestJoin { Address = "localhost:8081" });
				
				if (errorNodes.Count != 0)
				{
					Assert.Fail("Got errors from nodes: " + String.Join(" ", errorNodes));
				}
			}
		}
	}
}