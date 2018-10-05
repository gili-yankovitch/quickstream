using JSON;
using LogicServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream.Handlers
{
	public class LoginHandler : IServable
	{
		public string ContentType => "text/json";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var credRequest = (LoginRequest)new DataContractJsonSerializer(typeof(LoginRequest)).ReadObject(
								request.InputStream);

			var jsonResponse = new SessionKeyResponse { Success = false };
			jsonResponse.Message = "Login Failed";

			if (new UserEngine().Login(credRequest.Id, credRequest.NodeId, credRequest.Key))
			{
				jsonResponse.SessionKey = new UserEngine().generateSessionKey(credRequest.Id, credRequest.NodeId);
				jsonResponse.Success = true;
				jsonResponse.Message = "Successs";
			}

			new DataContractJsonSerializer(typeof(SessionKeyResponse)).WriteObject(response.OutputStream,
						jsonResponse);
		}
	}
}
