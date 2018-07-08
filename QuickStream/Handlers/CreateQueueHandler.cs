using System;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Policy;
using JSON;
using LogicServices;

namespace QuickStream.Handlers
{
	public class CreateQueueHandler : IServable
	{
		public string ContentType => "text/json";
		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var queueCreateRequest =
				(QueueCreateRequest)new DataContractJsonSerializer(typeof(QueueCreateRequest)).ReadObject(
					request.InputStream);

			var jsonResponse = new BooleanResponse { Success = false };

		
			if (!UserEngine.LoginBySessionId(queueCreateRequest.Id, queueCreateRequest.SessionKey))
			{
				jsonResponse.Message = "Login Failed";
			}
			else
			{
				try
				{
					QueueEngine.CreateQueue(queueCreateRequest.Id, queueCreateRequest.QueueName, queueCreateRequest.Readers);

					jsonResponse.Success = true;
				}
				catch (Exception e)
				{
					jsonResponse.Message = e.Message;
				}
			}

			new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
				jsonResponse);
		}
	}
}