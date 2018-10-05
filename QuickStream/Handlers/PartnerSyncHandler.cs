using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using JSON;
using JSON.PartnerRequests;
using LogicServices;

namespace QuickStream.Handlers
{
	public class PartnerSyncHandler : IServable
	{
		public string ContentType => "text/json";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var partnerSyncRequest = new JSONSerializer<PartnerSyncMessage>().Deserialize(request.InputStream);

			var jsonResponse = new BooleanResponse { Success = false };

			/* Validate incoming certificate */
			try
			{
				if (!new CryptoEngine().verifyCertificate(partnerSyncRequest.key, partnerSyncRequest.certId, partnerSyncRequest.cert)
					.VerifyData(partnerSyncRequest.data, partnerSyncRequest.signature, HashAlgorithmName.SHA256))
				{
					throw new CryptographicException("Data verification failed");
				}

				/* Parse action */
				var partnerSyncRequestData = new JSONSerializer<PartnerSyncMessageData>().Deserialize(partnerSyncRequest.data);

				/* Figure out which message type need to be handled */
				switch (partnerSyncRequestData.MessageType)
				{
					case PartnerSyncMessageType.PARTNER_JOIN:
						{
							/* Parse join request */
							var partnerJoinRequest = new JSONSerializer<PartnerSyncRequestJoin>().Deserialize(partnerSyncRequestData.Data);

							/* Add to partners */
							new PartnersEngine().AddPartner(partnerJoinRequest.Address);

							/* Create a DB Dump object */
							var partnerDBDump = new PartnerSyncResponseDBDump { Partners = PartnersEngine.Partners.ToArray() };

							/* Dump te DB */
							var dbFile = File.Open(Config<string>.GetInstance()["DB_Filename"], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

							using (var reader = new BinaryReader(dbFile))
							{
								/* Hopefully DB will not be larger than 2GB */
								partnerDBDump.DBDump = reader.ReadBytes((int)dbFile.Length);
							}

							dbFile.Close();

							new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(partnerDBDump), response.OutputStream);

							break;
						}

					case PartnerSyncMessageType.USER_CREATE:
						{
							/* Parse register request */
							var userRegisterRequest = new JSONSerializer<PartnerSyncUserCreate>().Deserialize(partnerSyncRequestData.Data);

							/* Update here */
							new UserEngine().RegisterUser(partnerSyncRequest.certId, userRegisterRequest.Id, Encoding.ASCII.GetBytes(userRegisterRequest.Key));

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";

							new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(jsonResponse), response.OutputStream);

							break;
						}

					case PartnerSyncMessageType.QUEUE_CREATE:
						{
							/* Parse queue create request */
							var queueCreateRequest = new JSONSerializer<PartnerSyncQueueCreate>().Deserialize(partnerSyncRequestData.Data);

							new QueueEngine().CreateQueue(queueCreateRequest.UID, queueCreateRequest.NodeId, queueCreateRequest.QueueName, queueCreateRequest.Readers);

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";

							new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(jsonResponse), response.OutputStream);

							break;
						}

					case PartnerSyncMessageType.QUEUE_WRITE:
						{
							/* Parse queue write request */
							var queueWriteRequest = new JSONSerializer<PartnerSyncQueueWrite>().Deserialize(partnerSyncRequestData.Data);

							/* Try to correct timezone issues */
							Config<long>.GetInstance()["TIMEZONE_CORRECTION"] = queueWriteRequest.Timestamp.ToFileTimeUtc() - DateTime.UtcNow.ToFileTimeUtc();

							/* Add to buffered queue */
							if (new QueueEngine().WriteBufferedQueue(queueWriteRequest.UID, queueWriteRequest.NodeId, queueWriteRequest.QueueName, queueWriteRequest.Data, queueWriteRequest.Timestamp))
							{
								jsonResponse.Success = true;
								jsonResponse.Message = "Success";
							}
							else
							{
								jsonResponse.Success = false;
								jsonResponse.Message = "Not enough space in queue";
							}

							new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(jsonResponse), response.OutputStream);

							break;
						}

					case PartnerSyncMessageType.QUEUE_COMMIT:
						{
							/* Parse queue commit request */
							var queueCommitRequest = new JSONSerializer<PartnerSyncRequestCommit>().Deserialize(partnerSyncRequestData.Data);

							new QueueEngine().CommitQueue(queueCommitRequest.UID, queueCommitRequest.NodeId, queueCommitRequest.ReaderId, queueCommitRequest.ReaderNodeId, queueCommitRequest.QueueName);

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";

							new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(jsonResponse), response.OutputStream);

							break;
						}

					default:
						{
							jsonResponse.Message = "Invalid Message ID";

							break;
						}
				}
			}
			catch (CryptographicException e)
			{
				Console.WriteLine(e);

				jsonResponse.Message = e.Message;

				new JSONSerializer<PartnerSyncMessage>().Serialize(new PartnersEngine().PrepareSignedMessage(jsonResponse), response.OutputStream);
			}
		}
	}
}
