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
using System.Runtime.Serialization;
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

		public void AddPartner(string Partner)
		{
			if (!Partners.Exists(partner => partner.Equals(Partner)))
			{
				Partners.Add(Partner);
			}
		}

		private PartnerSyncMessage PreparePartnerSyncRequest(PartnerSyncMessageType msgType, byte[] data)
		{
			return PrepareSignedMessage(new JSONSerializer<PartnerSyncMessageData>().Serialize(new PartnerSyncMessageData { MessageType = msgType, Data = data }));
		}

		public PartnerSyncMessageType PartnerMsgToType<T>(T data)
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

		public PartnerSyncMessage PrepareSignedMessage<T>(T data)
		{
			PartnerSyncMessageType type = new PartnersEngine().PartnerMsgToType<T>(data);

			return PrepareSignedMessage(new JSONSerializer<PartnerSyncMessageData>().Serialize(new PartnerSyncMessageData { Data = new JSONSerializer<T>().Serialize(data), MessageType = type }));
		}

		public PartnerSyncMessage PrepareSignedMessage(byte[] data)
		{
			PartnerSyncMessage msg = new PartnerSyncMessage();

			/* Provide certificate data */
			msg.certId = CryptoEngine.Certificate.Cert.Id;
			msg.cert = new byte[CryptoEngine.Certificate.Cert.Signature.ToByteArray().Length];
			CryptoEngine.Certificate.Cert.Signature.ToByteArray().CopyTo(msg.cert, 0);
			msg.key = CryptoEngine.Certificate.Keys.PublicKey.PublicKey.ToByteArray();

			/* Copy data itself */
			msg.data = data;

			var dsa = new CryptoEngine().ECLoad(CryptoEngine.Certificate.Keys.PublicKey.PublicKey.ToByteArray(), CryptoEngine.Certificate.Keys.PrivateKey.ToByteArray());

			/* Sign the data */
			msg.signature = dsa.SignData(data, HashAlgorithmName.SHA256);

			return msg;
		}

		private WebRequest Request(string url)
		{
			var request = WebRequest.Create(url + "/partnerSync");
			request.Method = "POST";
			request.ContentType = "text/json";

			return request;
		}

		public void PartnerJoinRequest(PartnerSyncRequestJoin requestJoin)
		{
			/* Build the message */
			var content = PreparePartnerSyncRequest(PartnerSyncMessageType.PARTNER_JOIN, new JSONSerializer<PartnerSyncRequestJoin>().Serialize(requestJoin));

			byte[] dbDump = null;

			var newPartners = new List<string>();

			/* Ask each of the participants to send its database version */
			foreach (var partner in Partners)
			{
				var request = Request(partner);

				try
				{
					new JSONSerializer<PartnerSyncMessage>().Serialize(content, request.GetRequestStream()).Close();
				}
				catch (WebException)
				{
					/* Node might be down */
					continue;
				}

				PartnerSyncMessage signedResponse;

				try
				{
					/* Get the response */
					signedResponse = new JSONSerializer<PartnerSyncMessage>().Deserialize(request.GetResponse().GetResponseStream());
				}
				catch (SerializationException e)
				{
					continue;
				}

				if (!new CryptoEngine().verifyCertificate(signedResponse.key, signedResponse.certId, signedResponse.cert).VerifyData(signedResponse.data, signedResponse.signature))
				{
					/* Invalid response. Ignore. */
					continue;
				}

				var response = new JSONSerializer<PartnerSyncResponseDBDump>().Deserialize(new JSONSerializer<PartnerSyncMessageData>().Deserialize(signedResponse.data).Data);

				if (!response.Success)
				{
					/* Well, something went wrong... */
					continue;
				}

				foreach (var newPartner in response.Partners)
				{
					if ((!Partners.Exists(s => s == newPartner)) && (!newPartners.Exists(s => s == newPartner)) && (newPartner != Config<string>.GetInstance()["PUBLIC_ADDRESS"]) && (newPartner.StartsWith("http://") || newPartner.StartsWith("https://")))
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
				var dbFile = File.Open(Config<string>.GetInstance()["DB_Filename"], FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

				using (var writer = new BinaryWriter(dbFile))
				{
					writer.Write(dbDump);
				}

				dbFile.Close();
			}

			/* Add additional partners */
			foreach (var newPartner in newPartners)
			{
				Partners.Add(newPartner);
			}
		}

		public List<string> PartnersUpdateRequest<T>(T requestUpdate)
		{
			/* Build the message */
			var content = PreparePartnerSyncRequest(PartnerMsgToType<T>(requestUpdate), new JSONSerializer<T>().Serialize(requestUpdate));

			var ErrNodes = new List<string>();

			/* Ask each of the participants to send its database version */
			foreach (var partner in Partners)
			{
				try
				{
					var request = Request(partner);

					new JSONSerializer<PartnerSyncMessage>().Serialize(content, request.GetRequestStream()).Close();

					/* Get the response */
					var signedResponse = new JSONSerializer<PartnerSyncMessage>().Deserialize(request.GetResponse().GetResponseStream());

					if (!new CryptoEngine().verifyCertificate(signedResponse.key, signedResponse.certId, signedResponse.cert).VerifyData(signedResponse.data, signedResponse.signature))
					{
						/* Invalid response. Ignore. */
						continue;
					}

					var response = new JSONSerializer<BooleanResponse>().Deserialize(new JSONSerializer<PartnerSyncMessageData>().Deserialize(signedResponse.data).Data);

					if (!response.Success)
					{
						Console.WriteLine("Error in request...");

						ErrNodes.Add(partner);
					}
				}
				catch (Exception e)
				{
					/* Add to err nodes */
					ErrNodes.Add(partner);
				}
			}

			return ErrNodes;
		}

		public void DistributeToPartners(PartnerSyncRequestJoin requestJoin)
		{
			requestJoin.Address = Config<string>.GetInstance()["PUBLIC_ADDRESS"];

		}
	}
}
