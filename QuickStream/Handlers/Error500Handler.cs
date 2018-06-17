using System.Net;
using System.Security.Policy;
using System.Text;

namespace QuickStream.Handlers
{
	public class Error500Handler : IServable
	{
		public string ContentType => "text/plain";

		public int StatusCode => 500;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var rsp_body = "Internal Server Error.";

			var rsp = Encoding.ASCII.GetBytes(rsp_body);
			response.OutputStream.Write(rsp, 0, rsp.Length);
		}
	}
}