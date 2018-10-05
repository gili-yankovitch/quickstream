using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using JSON;
using JSON.PartnerRequests;
using LogicServices;

namespace QuickStream.Handlers
{
	public class CreateUserHandler : IServable
	{
		public string ContentType => "text/json";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var userCreateRequest =
				(UserCreateRequest) new DataContractJsonSerializer(typeof(UserCreateRequest)).ReadObject(
					request.InputStream);

			var newId = new UserEngine().RegisterUser(CryptoEngine.Certificate.Cert.Id, Encoding.ASCII.GetBytes(userCreateRequest.Key));

			new DataContractJsonSerializer(typeof(UserCreateResponse)).WriteObject(response.OutputStream,
				new UserCreateResponse {Id = newId, NodeId = CryptoEngine.Certificate.Cert.Id, Success = true });

			/* Update everyone else that there's a new user */
			new PartnersEngine().PartnersUpdateRequest(new PartnerSyncUserCreate { Id = newId, NodeId = CryptoEngine.Certificate.Cert.Id, Key = userCreateRequest.Key });
		}
	}
}