using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickStream
{
	public class AsyncHTTPServer
	{
		#region Data Memembers

		private HttpListener m_listener;
		private Dictionary<string, IServable> m_handlers;
		private IServable m_404Handler;
		private IServable m_500Handler;

		#endregion

		public AsyncHTTPServer(ushort port)
		{
			this.m_listener = new HttpListener();
			this.m_listener.Prefixes.Add("http://+:" + port.ToString() + "/");

			this.m_handlers = new Dictionary<string, IServable>();
			this.m_404Handler = new Error404Handler();
			this.m_500Handler = new Error500Handler();
		}

		public void AddHandler(string uri, IServable handler)
		{
			this.m_handlers[uri] = handler;
		}

		public void Set404Handler(IServable handler)
		{
			this.m_404Handler = handler;
		}

		public void Set500Handler(IServable handler)
		{
			this.m_500Handler = handler;
		}

		public void Start()
		{
			this.m_listener.Start();

			while (true)
			{
				try
				{
					HttpListenerContext context = this.m_listener.GetContext();
					ThreadPool.QueueUserWorkItem(o =>
					{
						this.HandleRequest(context);
					});
				}
				catch (Exception e)
				{
					Console.WriteLine("Unexpected exception: " + e.ToString());
				}
			}
		}

		private void HandleRequest(HttpListenerContext context)
		{
			/* Find the best-fit for the given URI */
			var handler = this.m_404Handler;
			var subsetUri = context.Request.RawUrl.TrimEnd('/');
			var requestUri = subsetUri.Split('/');

			for (var i = requestUri.Length - 1; i > 0; --i)
			{
				subsetUri = string.Join("/", requestUri.Take(i + 1));

				if (this.m_handlers.ContainsKey(subsetUri))
				{
					handler = this.m_handlers[subsetUri];

					/* Finish up here */
					break;
				}
			}

			context.Response.SendChunked = true;
			try
			{
				handler.Serve(context.Request, context.Response, new Url(context.Request.Url.ToString().Substring(subsetUri.Length)));
				context.Response.StatusCode = handler.StatusCode;
			}
			catch (Exception)
			{
				this.m_500Handler.Serve(context.Request, context.Response, new Url(context.Request.Url.ToString().Substring(subsetUri.Length)));
				context.Response.StatusCode = this.m_500Handler.StatusCode;
			}
			
			context.Response.OutputStream.Close();
		}
	}
}