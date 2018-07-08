using JSON;
using LogicServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream.Handlers
{
	class QueueHandler : IServable
	{
		public string ContentType => "text/json";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			/* Know what action is being performed */
			var details = url.Value.Split('/');

			if (!int.TryParse(details[3], out int user))
				user = -1;

			var queue = details[4];

			var action =
				(QueueRequest)new DataContractJsonSerializer(typeof(QueueRequest)).ReadObject(
					request.InputStream);

			if (!UserEngine.LoginBySessionId(action.Id, action.SessionKey))
			{
				new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
						new BooleanResponse { Success = false, Message = "Failed Login" });
			}
			else
			{
				/* Parse according to action */
				if (action.Action == e_action.E_WRITE)
				{
					var jsonResponse = new BooleanResponse { Success = false };

					try
					{
						if (user != action.Id)
							throw new Exception("Permission denied: Invalid writer user");

						QueueEngine.WriteQueue(user, queue, action.Data);
						jsonResponse.Success = true;
						jsonResponse.Message = "Success";
					}
					catch (Exception e)
					{
						jsonResponse.Message = e.Message;
					}
					finally
					{
						new DataContractJsonSerializer(typeof(BooleanResponse)).WriteObject(response.OutputStream,
						jsonResponse);
					}
				}
				else if (action.Action == e_action.E_READ)
				{
					var jsonResponse = new QueueReadResponse { Success = false };

					try
					{
						jsonResponse.Messages = QueueEngine.ReadQueue(action.Id, user, queue, action.Commit);
						jsonResponse.Success = true;
					}
					catch (Exception e)
					{
						jsonResponse.Message = e.Message;
					}
					finally
					{
						new DataContractJsonSerializer(typeof(QueueReadResponse)).WriteObject(response.OutputStream,
						jsonResponse);
					}
				}
			}
		}
	}
}
