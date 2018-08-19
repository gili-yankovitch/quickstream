using DAL;
using JSON;
using JSON.PartnerRequests;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LogicServices
{
	public class PartnersEngine
	{
		public static List<string> Partners = new List<string>();

		public static bool IsMaster = false;

		public static void AddPartner(string Partner)
		{
			if (!Partners.Exists(partner => partner.Equals(Partner)))
			{
				Partners.Add(Partner);
			}
		}

		private static PartnerSyncMessage PreparePartnerSyncRequest(PartnerSyncMessageType msgType, byte[] data)
		{
			return PrepareSignedMessage(JSONSerializer<PartnerSyncMessageData>.Serialize(new PartnerSyncMessageData { MessageType = msgType, Data = data }));
		}

		public static PartnerSyncMessageType PartnerMsgToType<T>(T data)
		{
			if (data is PartnerSyncResponseDBDump)
			{
				return PartnerSyncMessageType.DB_DUMP_RSP;
			}
			else if (data is PartnerSyncRequestJoin)
			{
				return PartnerSyncMessageType.PARTNER_JOIN;
			}
			else if (data is PartnerSyncUserCreate)
			{
				return PartnerSyncMessageType.USER_CREATE;
			}
			else if (data is PartnerSyncQueueCreate)
			{
				return PartnerSyncMessageType.QUEUE_CREATE;
			}
			else if (data is PartnerSyncQueueWrite)
			{
				return PartnerSyncMessageType.QUEUE_WRITE;
			}
			else if (data is PartnerSyncRequestCommit)
			{
				return PartnerSyncMessageType.QUEUE_COMMIT;
			}
			else
			{
				return PartnerSyncMessageType.GENERIC_RSP;
			}
		}

		public static PartnerSyncMessage PrepareSignedMessage<T>(T data)
		{
			PartnerSyncMessageType type = PartnersEngine.PartnerMsgToType<T>(data);

			return PrepareSignedMessage(JSONSerializer<PartnerSyncMessageData>.Serialize(new PartnerSyncMessageData { Data = JSONSerializer<T>.Serialize(data), MessageType = type }));
		}

		public static PartnerSyncMessage PrepareSignedMessage(byte[] data)
		{
			PartnerSyncMessage msg = new PartnerSyncMessage();

			/* Provide certificate data */
			msg.certId = CryptoEngine.GetInstance().Certificate.Cert.Id;
			msg.cert = new byte[CryptoEngine.GetInstance().Certificate.Cert.Signature.ToByteArray().Length];
			CryptoEngine.GetInstance().Certificate.Cert.Signature.ToByteArray().CopyTo(msg.cert, 0);
			msg.key = CryptoEngine.GetInstance().Certificate.Keys.PublicKey.PublicKey.ToByteArray();

			/* Copy data itself */
			msg.data = data;

			var dsa = CryptoEngine.GetInstance().ECLoad(CryptoEngine.GetInstance().Certificate.Keys.PublicKey.PublicKey.ToByteArray(), CryptoEngine.GetInstance().Certificate.Keys.PrivateKey.ToByteArray());

			/* Sign the data */
			msg.signature = dsa.SignData(data, HashAlgorithmName.SHA256);

			return msg;
		}

		private static WebRequest Request(string url)
		{
			var request = WebRequest.Create(url + "partnerSync");
			request.Method = "POST";
			request.ContentType = "text/json";

			return request;
		}

		public static void PartnerJoinRequest(PartnerSyncRequestJoin requestJoin)
		{
			/* Build the message */
			var content = PreparePartnerSyncRequest(PartnerSyncMessageType.PARTNER_JOIN, JSONSerializer<PartnerSyncRequestJoin>.Serialize(requestJoin));

			byte[] dbDump = null;

			var newPartners = new List<string>();

			/* Ask each of the participants to send its database version */
			foreach (var partner in Partners)
			{
				var request = Request(partner);

				try
				{
					JSONSerializer<PartnerSyncMessage>.Serialize(content, request.GetRequestStream()).Close();
				}
				catch (WebException)
				{
					/* Node might be down */
					continue;
				}

				/* Get the response */
				var signedResponse = JSONSerializer<PartnerSyncMessage>.Deserialize(request.GetResponse().GetResponseStream());

				if (!CryptoEngine.GetInstance().verifyCertificate(signedResponse.key, signedResponse.certId, signedResponse.cert).VerifyData(signedResponse.data, signedResponse.signature))
				{
					/* Invalid response. Ignore. */
					continue;
				}

				var response = JSONSerializer<PartnerSyncResponseDBDump>.Deserialize(JSONSerializer<PartnerSyncMessageData>.Deserialize(signedResponse.data).Data);

				foreach (var newPartner in response.Partners)
				{
					if ((!Partners.Exists(s => s == newPartner)) && (newPartners.Exists(s => s == newPartner)) && (newPartner != Config<string>.GetInstance()["PUBLIC_ADDRESS"]) && (newPartner.StartsWith("http://") || newPartner.StartsWith("https://")))
					{
						newPartners.Add(newPartner);
					}
				}

				/* Choose largest dump too sync with (TODO: Not sure it's the best idea) - Currently best-effort. */
				if ((dbDump == null) || (dbDump.Length < response.DBDump.Length))
					dbDump = response.DBDump;
			}

			if (dbDump != null)
			{
				/* Write DB To file */
				var dbFile = File.Open(Config<string>.GetInstance()["DB_Filename"], FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

				using (var writer = new BinaryWriter(dbFile))
				{
					writer.Write(dbDump);
				}
			}

			/* Add additional partners */
			foreach (var newPartner in newPartners)
			{
				Partners.Add(newPartner);
			}
		}

		public static void PartnersUpdateRequest<T>(T requestUpdate)
		{
			/* Build the message */
			var content = PreparePartnerSyncRequest(PartnerMsgToType<T>(requestUpdate), JSONSerializer<T>.Serialize(requestUpdate));

			/* Ask each of the participants to send its database version */
			foreach (var partner in Partners)
			{
				var request = Request(partner);

				JSONSerializer<PartnerSyncMessage>.Serialize(content, request.GetRequestStream()).Close();

				/* Get the response */
				var signedResponse = JSONSerializer<PartnerSyncMessage>.Deserialize(request.GetResponse().GetResponseStream());

				if (!CryptoEngine.GetInstance().verifyCertificate(signedResponse.key, signedResponse.certId, signedResponse.cert).VerifyData(signedResponse.data, signedResponse.signature))
				{
					/* Invalid response. Ignore. */
					continue;
				}

				var response = JSONSerializer<BooleanResponse>.Deserialize(JSONSerializer<PartnerSyncMessageData>.Deserialize(signedResponse.data).Data);

				if (!response.Success)
				{
					Console.WriteLine("Error in request...");
				}
			}
		}

		public static void DistributeToPartners(PartnerSyncRequestJoin requestJoin)
		{
			requestJoin.Address = Config<string>.GetInstance()["PUBLIC_ADDRESS"];

		}
	}
}
