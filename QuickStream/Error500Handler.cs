using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream
{
	public class Error500Handler : IServable
	{
		public string ContentType => "text/plain";

		public int StatusCode => 500;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Uri uri)
		{
			string rsp_body = "Internal Server Error.";

			byte[] rsp = Encoding.ASCII.GetBytes(rsp_body);
			response.OutputStream.Write(rsp, 0, rsp.Length);
		}
	}
}
