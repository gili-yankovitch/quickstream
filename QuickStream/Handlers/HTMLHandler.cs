using LogicServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream.Handlers
{
	public class HTMLHandler : IServable
	{
		public string ContentType => m_contentType;

		public int StatusCode => 200;

		public HTMLHandler()
		{
			this.m_types = new Dictionary<string, string>();

			this.m_types["html"] = "text/html";
			this.m_types["css"] = "text/css";
			this.m_types["js"] = "text/script";
			this.m_types["gif"] = "image/gif";
			this.m_types["png"] = "image/png";
			this.m_types["jpg"] = "image/jpg";
			this.m_types["svg"] = "text/svg";
			this.m_types["ico"] = "image/ico";
		}

		public void Serve(HttpListenerRequest request, HttpListenerResponse response, Url url)
		{
			var origuri = request.RawUrl.TrimEnd('/');

			if (origuri == "")
			{
				origuri = "/index.html";
			}

			var path = Config<string>.GetInstance()["HTML_DIR"] + Path.DirectorySeparatorChar + origuri;

			var b = File.ReadAllBytes(path);
			var ext = Path.GetExtension(path).Substring(1);

			if (!this.m_types.ContainsKey(ext))
			{
				this.m_contentType = "text/plain";
			}
			else
			{
				this.m_contentType = this.m_types[ext];
			}

			response.OutputStream.Write(b, 0, b.Length);
		}

		private Dictionary<string, string> m_types;

		private string m_contentType = "text/plain";
	}
}
