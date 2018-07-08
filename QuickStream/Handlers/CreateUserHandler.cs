using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using JSON;
using LogicServices;

namespace QuickStream.Handlers
{
	internal class CreateUserHandler : IServable
	{
		public string ContentType => "text/json";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var userCreateRequest =
				(UserCreateRequest) new DataContractJsonSerializer(typeof(UserCreateRequest)).ReadObject(
					request.InputStream);

			new DataContractJsonSerializer(typeof(UserCreateResponse)).WriteObject(response.OutputStream,
				new UserCreateResponse {Id = UserEngine.RegisterUser(Encoding.ASCII.GetBytes(userCreateRequest.Key)), Success = true });
		}
	}
}