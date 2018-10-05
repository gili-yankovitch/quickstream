using JSON;
using JSON.PartnerRequests;
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
	public class QueueHandler : IServable
	{
		public string ContentType => "text/json";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			/* Know what action is being performed */
			var details = url.Value.Split('/');

			if (!int.TryParse(details[2], out int node))
				node = -1;

			if (!int.TryParse(details[3], out int user))
				user = -1;

			var queue = details[4];

			var action =
				(QueueRequest)new DataContractJsonSerializer(typeof(QueueRequest)).ReadObject(
					request.InputStream);

			if (!new UserEngine().LoginBySessionId(action.Id, action.NodeId, action.SessionKey))
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
						if ((user != action.Id) || (node != action.NodeId))
						{
							Console.WriteLine("Permission denied: Invalid writer user. user = " + user + " action.Id = " + action.Id + " node = " + node + " action.NodeId = " + action.NodeId);
							for (int i = 0; i < details.Length; ++i)
								Console.WriteLine(details[i]);

							throw new Exception("Permission denied: Invalid writer user");
						}

						if (new QueueEngine().WriteBufferedQueue(user, node, queue, action.Data))
						{
							var errorNodes = new PartnersEngine().PartnersUpdateRequest(new PartnerSyncQueueWrite { NodeId = node, UID = user, QueueName = queue, Data = action.Data, Timestamp = DateTime.UtcNow });

							if (errorNodes.Count > 0)
							{
								jsonResponse.Message = "Could not notify the following partners on write: " + String.Join(" ", errorNodes.ToArray() + " (Port closed?)");

								Console.WriteLine(jsonResponse.Message);
							}

							jsonResponse.Success = true;
							jsonResponse.Message = "Success";
						}
						else
						{
							jsonResponse.Success = false;
							jsonResponse.Message = "Not enough space in queue.";
						}
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
					/* BAE Handle buffered queue */
					new QueueEngine().HandleQueueBuffer();

					var jsonResponse = new QueueReadResponse { Success = false };

					try
					{
						jsonResponse.Messages = new QueueEngine().ReadQueue(action.Id, user, node, queue, action.Commit);

						if (action.Commit)
						{
							var errorNodes = new PartnersEngine().PartnersUpdateRequest(new PartnerSyncRequestCommit { NodeId = node, UID = user, QueueName = queue, Commit = true, Timestamp = DateTime.UtcNow });

							if (errorNodes.Count > 0)
							{
								jsonResponse.Message = "Could not notify the following partners on commit: " + String.Join(" ", errorNodes.ToArray() + " (Port closed?)");

								Console.WriteLine(jsonResponse.Message);
							}
						}

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
