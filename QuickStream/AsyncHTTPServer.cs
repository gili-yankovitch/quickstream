using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Threading;
using LogicServices;
using QuickStream.Handlers;

namespace QuickStream
{
	public class AsyncHTTPServer
	{
		public AsyncHTTPServer(ushort port)
		{
			m_listener = new HttpListener();
			m_listener.Prefixes.Add("http://+:" + port + "/");
			m_listener.Prefixes.Add("https://+:443/");


			m_handlers = new Dictionary<string, IServable>();
			m_404Handler = new Error404Handler();
			m_500Handler = new Error500Handler();
			m_HTMLHandler = new HTMLHandler();
		}

		public void AddHandler(string uri, IServable handler)
		{
			m_handlers[uri] = handler;
		}

		public void Set404Handler(IServable handler)
		{
			m_404Handler = handler;
		}

		public void Set500Handler(IServable handler)
		{
			m_500Handler = handler;
		}

		public void Start()
		{
			m_listener.Start();

			while (true)
				try
				{
					var context = m_listener.GetContext();
					ThreadPool.QueueUserWorkItem(o => { HandleRequest(context); });
				}
				catch (Exception e)
				{
					Console.WriteLine("Unexpected exception: " + e);
				}
		}

		private void HandleRequest(HttpListenerContext context)
		{
			if (context.Request.Url.Port == 80)
			{
				context.Response.Redirect(context.Request.Url.ToString().Replace("http", "https"));
				context.Response.OutputStream.Close();

				return;
			}

			/* Find the best-fit for the given URI */
			var handler = m_404Handler;
			var subsetUri = context.Request.RawUrl.TrimEnd('/');

			var trimDoubleSlashes = subsetUri.Replace("//", "/");

			while (trimDoubleSlashes != subsetUri)
			{
				subsetUri = trimDoubleSlashes;
				trimDoubleSlashes = subsetUri.Replace("//", "/");
			}

			var requestUri = subsetUri.Split('/');
			var origuri = subsetUri;

			for (var i = requestUri.Length - 1; i > 0; --i)
			{
				subsetUri = string.Join("/", requestUri.Take(i + 1));

				if (m_handlers.ContainsKey(subsetUri))
				{
					handler = m_handlers[subsetUri];

					/* Finish up here */
					break;
				}
			}

			if (origuri == "")
			{
				origuri = "/index.html";
			}

			if (handler == m_404Handler)
			{
				var html = Config<string>.GetInstance()["HTML_DIR"] + Path.DirectorySeparatorChar + origuri;

				html = html.Replace("/..", "/").Replace("../", "/").Replace("/.", "/").Replace("./", "/");

				/* Try to find a webpage with such a path */
				if (File.Exists(html))
				{
					handler = m_HTMLHandler;
				}
			}

			context.Response.SendChunked = true;
			try
			{
				Console.WriteLine("Access: " + context.Request.Url.ToString().Substring(context.Request.Url.ToString().IndexOf(subsetUri)));
				
				handler.Serve(context.Request, context.Response,
					new Url(context.Request.Url.ToString().Substring(context.Request.Url.ToString().IndexOf(subsetUri))));
				context.Response.StatusCode = handler.StatusCode;
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);

				m_500Handler.Serve(context.Request, context.Response,
					new Url(context.Request.Url.ToString().Substring(subsetUri.Length)));
				context.Response.StatusCode = m_500Handler.StatusCode;
			}

			context.Response.OutputStream.Close();
		}

		#region Data Memembers

		private readonly HttpListener m_listener;
		private readonly Dictionary<string, IServable> m_handlers;
		private IServable m_404Handler;
		private IServable m_500Handler;
		private IServable m_HTMLHandler;

		#endregion
	}
}