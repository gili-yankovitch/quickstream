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
	class PartnerSyncHandler : IServable
	{
		public string ContentType => "text/json";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var partnerSyncRequest = JSONSerializer<PartnerSyncMessage>.Deserialize(request.InputStream);

			var jsonResponse = new BooleanResponse { Success = false };

			/* Validate incoming certificate */
			try
			{
				if (!CryptoEngine.GetInstance().verifyCertificate(partnerSyncRequest.key, partnerSyncRequest.certId, partnerSyncRequest.cert)
					.VerifyData(partnerSyncRequest.data, partnerSyncRequest.signature, HashAlgorithmName.SHA256))
				{
					throw new CryptographicException("Data verification failed");
				}

				/* Parse action */
				var partnerSyncRequestData = JSONSerializer<PartnerSyncMessageData>.Deserialize(partnerSyncRequest.data);

				/* Figure out which message type need to be handled */
				switch (partnerSyncRequestData.MessageType)
				{
					case PartnerSyncMessageType.PARTNER_JOIN:
						{
							/* Parse join request */
							var partnerJoinRequest = JSONSerializer<PartnerSyncRequestJoin>.Deserialize(partnerSyncRequestData.Data);

							/* Add to partners */
							PartnersEngine.AddPartner(partnerJoinRequest.Address);

							/* Create a DB Dump object */
							var partnerDBDump = new PartnerSyncResponseDBDump { Partners = PartnersEngine.Partners.ToArray() };

							/* Dump te DB */
							var dbFile = File.Open(Config.DB_Filename, FileMode.Open);

							using (var reader = new BinaryReader(dbFile))
							{
								/* Hopefully DB will not be larger than 2GB */
								partnerDBDump.DBDump = reader.ReadBytes((int)dbFile.Length);
							}

							JSONSerializer<PartnerSyncMessage>.Serialize(PartnersEngine.PrepareSignedMessage(partnerDBDump), response.OutputStream);

							break;
						}

					case PartnerSyncMessageType.USER_CREATE:
						{
							/* Parse register request */
							var userRegisterRequest = JSONSerializer<PartnerSyncUserCreate>.Deserialize(partnerSyncRequestData.Data);

							/* Update here */
							UserEngine.RegisterUser(partnerSyncRequest.certId, userRegisterRequest.Id, Encoding.ASCII.GetBytes(userRegisterRequest.Key));

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";

							JSONSerializer<PartnerSyncMessage>.Serialize(PartnersEngine.PrepareSignedMessage(jsonResponse), response.OutputStream);

							break;
						}

					case PartnerSyncMessageType.QUEUE_CREATE:
						{
							/* Parse queue create request */
							var queueCreateRequest = JSONSerializer<PartnerSyncQueueCreate>.Deserialize(partnerSyncRequestData.Data);

							QueueEngine.CreateQueue(queueCreateRequest.UID, queueCreateRequest.NodeId, queueCreateRequest.QueueName, queueCreateRequest.Readers);

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";

							JSONSerializer<PartnerSyncMessage>.Serialize(PartnersEngine.PrepareSignedMessage(jsonResponse), response.OutputStream);

							break;
						}

					case PartnerSyncMessageType.QUEUE_WRITE:
						{
							/* Parse queue write request */
							var queueWriteRequest = JSONSerializer<PartnerSyncQueueWrite>.Deserialize(partnerSyncRequestData.Data);

							/* Add to buffered queue */
							QueueEngine.WriteBufferedQueue(queueWriteRequest.UID, queueWriteRequest.NodeId, queueWriteRequest.QueueName, queueWriteRequest.Data, queueWriteRequest.Timestamp);

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";

							JSONSerializer<PartnerSyncMessage>.Serialize(PartnersEngine.PrepareSignedMessage(jsonResponse), response.OutputStream);

							break;
						}

					case PartnerSyncMessageType.QUEUE_COMMIT:
						{
							/* Parse queue commit request */
							var queueCommitRequest = JSONSerializer<PartnerSyncRequestCommit>.Deserialize(partnerSyncRequestData.Data);

							QueueEngine.CommitQueue(queueCommitRequest.UID, queueCommitRequest.NodeId, queueCommitRequest.QueueName);

							JSONSerializer<PartnerSyncMessage>.Serialize(PartnersEngine.PrepareSignedMessage(jsonResponse), response.OutputStream);

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

				JSONSerializer<PartnerSyncMessage>.Serialize(PartnersEngine.PrepareSignedMessage(jsonResponse), response.OutputStream);
			}
		}
	}
}
