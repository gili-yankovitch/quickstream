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

			var jsonResponse = new QueueCreateResponse { Success = false };

			if (UserEngine.GetInstance().Login(queueCreateRequest.Id, queueCreateRequest.Key))
			{
				jsonResponse.Success = QueueEngine.GetInstance()
					.CreateQueue(queueCreateRequest.Id, queueCreateRequest.QueueName);
			}

			new DataContractJsonSerializer(typeof(QueueCreateResponse)).WriteObject(response.OutputStream,
				jsonResponse);
		}
	}
}