using System.Net;
using System.Security.Policy;

namespace QuickStream
{
	public interface IServable
	{
		string ContentType { get; }
		int StatusCode { get; }

		void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url);
	}
}