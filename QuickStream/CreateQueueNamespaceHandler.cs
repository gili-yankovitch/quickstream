using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream
{
	public class CreateQueueNamespaceHandler : IServable
	{
		public string ContentType => "text/plain";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var rspBody = new StringBuilder();
			rspBody.Append("METHOD: ");
			rspBody.Append(request.HttpMethod);
			rspBody.Append("\n");


			rspBody.Append("URI Path: ");
			rspBody.Append(request.Url.AbsolutePath);
			rspBody.Append("\n");

			rspBody.Append("Headers:\n");

			foreach (var header in request.Headers.AllKeys)
			{
				rspBody.Append("\t");
				rspBody.Append(header);
				rspBody.Append(": ");
				rspBody.Append(request.Headers[header]);
				rspBody.Append("\n");
			}

			var rsp = Encoding.ASCII.GetBytes(rspBody.ToString());
			response.OutputStream.Write(rsp, 0, rsp.Length);
		}
	}
}
