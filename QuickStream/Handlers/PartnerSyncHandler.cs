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

		private void RelayMessageToPartners(JSON.PartnerSyncRequest syncRequest)
		{
			foreach (var p in PartnersEngine.Partners)
			{
				var client = new HttpClient();
				var addr = new StringBuilder().Append("http://").Append(p).Append("/partnerSync");
				var memStream = new MemoryStream();

				new DataContractJsonSerializer(typeof(JSON.PartnerSyncRequest)).WriteObject(memStream, syncRequest);

				client.PostAsync(addr.ToString(), new StringContent(new StreamReader(memStream).ReadToEnd()));
			}
		}

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var slaveSyncRequest =
				(JSON.PartnerSyncRequest)new DataContractJsonSerializer(typeof(JSON.PartnerSyncRequest)).ReadObject(
					request.InputStream);

			var jsonResponse = new BooleanResponse { Success = false };

			/* Validate incoming certificate */
			try
			{
				if (!CryptoEngine.GetInstance().verifyCertificate(slaveSyncRequest.key, slaveSyncRequest.certId, slaveSyncRequest.cert)
					.VerifyData(slaveSyncRequest.data, slaveSyncRequest.signature, HashAlgorithmName.SHA256))
				{
					throw new CryptographicException("Data verification failed");
				}

				/* Insert data to MemoryStream to parse the actual data */
				var dataStream = new MemoryStream();
				dataStream.Write(slaveSyncRequest.data, 0, slaveSyncRequest.data.Length);
				dataStream.Seek(0, SeekOrigin.Begin);

				/* Parse action */
				var slaveSyncRequestData =
					(PartnerSyncRequestData)new DataContractJsonSerializer(typeof(PartnerSyncRequestData)).ReadObject(
					dataStream);

				/* Figure out which message type need to be handled */
				switch (slaveSyncRequestData.MessageType)
				{
					case PartnerSyncMessage.PARTNER_JOIN:
						{
							var joinStream = new MemoryStream();
							joinStream.Write(slaveSyncRequestData.Data, 0, slaveSyncRequestData.Data.Length);
							joinStream.Seek(0, SeekOrigin.Begin);

							/* Parse join request */
							var partrnerJoinRequest =
									(PartnerSyncRequestJoin)new DataContractJsonSerializer(typeof(PartnerSyncRequestJoin)).ReadObject(
									dataStream);

							/* Add to partners */
							PartnersEngine.AddPartner(partrnerJoinRequest.Address);

							/* Master is responsible on updating other partners about DB changes once they go up */
							if (PartnersEngine.IsMaster)
							{
								/* Create a DB Dump object */
								var partnerDBDump = new PartnerSyncResponseDBDump();
								var dbFile = File.Open(Config.DB_Filename, FileMode.Open);

								using (var reader = new BinaryReader(dbFile))
								{
									/* Hopefully DB will not be larger than 2GB */
									partnerDBDump.DBDump = reader.ReadBytes((int)dbFile.Length);
								}

							}

							break;
						}

					default:
						{
							jsonResponse.Message = "Invalid Message ID";

							break;
						}
				}

				/* Relay message to all slaves */
			}
			catch (CryptographicException e)
			{
				Console.WriteLine(e);

				jsonResponse.Message = e.Message;
			}
			finally
			{
				new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
					jsonResponse);
			}
		}
	}
}
