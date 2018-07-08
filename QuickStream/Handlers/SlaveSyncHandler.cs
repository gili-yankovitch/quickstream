using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using JSON;
using LogicServices;

namespace QuickStream.Handlers
{
	class SlaveSyncHandler : IServable
	{
		public string ContentType => "text/json";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var slaveSyncRequest =
				(SlaveSyncRequest)new DataContractJsonSerializer(typeof(SlaveSyncRequest)).ReadObject(
					request.InputStream);

			var jsonResponse = new BooleanResponse { Success = false };

			/* Validate incoming certificate */
			try
			{
				if (!CryptoEngine.GetInstance().verifyCertificate(slaveSyncRequest.key, slaveSyncRequest.cert)
					.VerifyData(slaveSyncRequest.data, slaveSyncRequest.signature, HashAlgorithmName.SHA256))
				{
					throw new CryptographicException("Data verification failed");
				}

				/* Parse action */

				/* Relay message to all slaves */

				jsonResponse.Success = true;
			}
			catch (CryptographicException e)
			{
				Console.WriteLine(e);
			}
			finally
			{
				new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
					jsonResponse);
			}
		}
	}
}
