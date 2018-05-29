using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream
{
	public class CreateQueueNamespaceHandler : IServable
	{
		public string ContentType => "text/plain";

		public int StatusCode => 200;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Uri uri)
		{
			string rsp_body = "";

			rsp_body += "METHOD: " + request.HttpMethod + "\n";
			rsp_body += "URI Path: " + request.Url.AbsolutePath + "\n";
			rsp_body += "Headers:\n";

			foreach (string header in request.Headers.AllKeys)
			{
				rsp_body += "\t" + header + ": " + request.Headers[header] + "\n";
			}

			byte[] rsp = Encoding.ASCII.GetBytes(rsp_body);
			response.OutputStream.Write(rsp, 0, rsp.Length);
		}
	}
}
