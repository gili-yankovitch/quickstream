using System.Net;
using System.Security.Policy;
using System.Text;

namespace QuickStream.Handlers
{
	public class Error404Handler : IServable
	{
		public string ContentType => "text/plain";

		public int StatusCode => 404;

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var rsp_body = "Error 404. URI " + url + " not found.";

			var rsp = Encoding.ASCII.GetBytes(rsp_body);
			response.OutputStream.Write(rsp, 0, rsp.Length);
		}
	}
}