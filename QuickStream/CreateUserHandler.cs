using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using DAL;
using QuickStream.JSON;

namespace QuickStream
{
	class CreateUserHandler : IServable
	{
		public string ContentType => "text/json";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var hash = SHA256.Create();
			hash.Initialize();

			var userCreateRequest = (UserCreateRequest)new DataContractJsonSerializer(typeof(UserCreateRequest)).ReadObject(request.InputStream);
			var u = new User() { Key = hash.ComputeHash(userCreateRequest.Key) };

			using (var ctx = new MessagingContext())
			{
				ctx.Users.Add(u);
				ctx.SaveChanges();
			}

			new DataContractJsonSerializer(typeof(UserCreateResponse)).WriteObject(response.OutputStream, new UserCreateResponse { Id = u.Id });
		}
	}
}
