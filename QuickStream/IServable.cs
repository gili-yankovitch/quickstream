using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream
{
	public interface IServable
	{
		string ContentType { get; }
		int StatusCode { get; }

		void Serve(HttpListenerRequest request, HttpListenerResponse response, Url uri);
	}
}
