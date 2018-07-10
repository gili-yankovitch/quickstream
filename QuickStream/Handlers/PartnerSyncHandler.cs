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
			var partnerSyncRequest =
				(JSON.PartnerSyncRequest)new DataContractJsonSerializer(typeof(JSON.PartnerSyncRequest)).ReadObject(
					request.InputStream);

			var jsonResponse = new BooleanResponse { Success = false };

			/* Validate incoming certificate */
			try
			{
				if (!CryptoEngine.GetInstance().verifyCertificate(partnerSyncRequest.key, partnerSyncRequest.certId, partnerSyncRequest.cert)
					.VerifyData(partnerSyncRequest.data, partnerSyncRequest.signature, HashAlgorithmName.SHA256))
				{
					throw new CryptographicException("Data verification failed");
				}

				/* Insert data to MemoryStream to parse the actual data */
				var dataStream = new MemoryStream();
				dataStream.Write(partnerSyncRequest.data, 0, partnerSyncRequest.data.Length);
				dataStream.Seek(0, SeekOrigin.Begin);

				/* Parse action */
				var partnerSyncRequestData =
					(PartnerSyncRequestData)new DataContractJsonSerializer(typeof(PartnerSyncRequestData)).ReadObject(
					dataStream);

				var partnerData = new MemoryStream();
				partnerData.Write(partnerSyncRequestData.Data, 0, partnerSyncRequestData.Data.Length);
				partnerData.Seek(0, SeekOrigin.Begin);

				/* Figure out which message type need to be handled */
				switch (partnerSyncRequestData.MessageType)
				{
					case PartnerSyncMessage.PARTNER_JOIN:
						{
							/* Parse join request */
							var partnerJoinRequest =
									(PartnerSyncRequestJoin)new DataContractJsonSerializer(typeof(PartnerSyncRequestJoin)).ReadObject(
									partnerData);

							/* Add to partners */
							PartnersEngine.AddPartner(partnerJoinRequest.Address);

							/* Create a DB Dump object */
							var partnerDBDump = new PartnerSyncResponseDBDump();
							var dbFile = File.Open(Config.DB_Filename, FileMode.Open);

							using (var reader = new BinaryReader(dbFile))
							{
								/* Hopefully DB will not be larger than 2GB */
								partnerDBDump.DBDump = reader.ReadBytes((int)dbFile.Length);
							}

							// TODO: Return ALL MESSAGES SIGNED
							new DataContractJsonSerializer(typeof(PartnerSyncResponseDBDump)).WriteObject(response.OutputStream,
								partnerDBDump);

							break;
						}

					case PartnerSyncMessage.USER_CREATE:
						{
							/* Parse register request */
							var userRegisterRequest =
									(PartnerSyncUserCreate)new DataContractJsonSerializer(typeof(PartnerSyncUserCreate)).ReadObject(
									partnerData);

							/* Update here */
							UserEngine.RegisterUser(partnerSyncRequest.certId, userRegisterRequest.Id, Encoding.ASCII.GetBytes(userRegisterRequest.Key));

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";

							// TODO: Return ALL MESSAGES SIGNED
							new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
								jsonResponse);

							break;
						}

					case PartnerSyncMessage.QUEUE_CREATE:
						{
							/* Parse queue create request */
							var queueCreateRequest =
									(PartnerSyncQueueCreate)new DataContractJsonSerializer(typeof(PartnerSyncQueueCreate)).ReadObject(
									partnerData);

							QueueEngine.CreateQueue(queueCreateRequest.UID, queueCreateRequest.NodeId, queueCreateRequest.QueueName, queueCreateRequest.Readers);

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";

							// TODO: Return ALL MESSAGES SIGNED
							new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
								jsonResponse);

							break;
						}

					case PartnerSyncMessage.QUEUE_WRITE:
						{
							/* Parse queue write request */
							var queueWriteRequest =
									(PartnerSyncQueueWrite)new DataContractJsonSerializer(typeof(PartnerSyncQueueWrite)).ReadObject(
									partnerData);

							/* Add to buffered queue */
							QueueEngine.WriteBufferedQueue(queueWriteRequest.UID, queueWriteRequest.NodeId, queueWriteRequest.QueueName, queueWriteRequest.Data, queueWriteRequest.Timestamp);

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";

							// TODO: Return ALL MESSAGES SIGNED
							new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
								jsonResponse);

							break;
						}

					case PartnerSyncMessage.QUEUE_COMMIT:
						{
							/* Parse queue commit request */
							var queueCommitRequest =
									(PartnerSyncRequestCommit)new DataContractJsonSerializer(typeof(PartnerSyncRequestCommit)).ReadObject(
									partnerData);

							QueueEngine.CommitQueue(queueCommitRequest.UID, queueCommitRequest.NodeId, queueCommitRequest.QueueName);

							// TODO: Return ALL MESSAGES SIGNED
							new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
								jsonResponse);

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

				new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
								jsonResponse);
			}
		}
	}
}
