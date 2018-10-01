﻿using System;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Policy;
using JSON;
using JSON.PartnerRequests;
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

		
			if (!new UserEngine().LoginBySessionId(queueCreateRequest.Id, queueCreateRequest.NodeId, queueCreateRequest.SessionKey))
			{
				jsonResponse.Message = "Login Failed";
			}
			else
			{
				try
				{
					new QueueEngine().CreateQueue(queueCreateRequest.Id, queueCreateRequest.NodeId, queueCreateRequest.QueueName, queueCreateRequest.Readers);

					new PartnersEngine().PartnersUpdateRequest(new PartnerSyncQueueCreate { NodeId = queueCreateRequest.NodeId, UID = queueCreateRequest.Id, QueueName = queueCreateRequest.QueueName, Readers = queueCreateRequest.Readers } );

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