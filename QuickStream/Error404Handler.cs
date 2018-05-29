using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream
{
	public class Error404Handler : IServable
	{
		public string ContentType => "text/plain";

		public int StatusCode => 404;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Uri uri)
		{
			string rsp_body = "Error 404. URI " + uri.ToString() + " not found.";

			byte[] rsp = Encoding.ASCII.GetBytes(rsp_body);
			response.OutputStream.Write(rsp, 0, rsp.Length);
		}
	}
}
