using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
			IServable handler = this.m_404Handler;
			string subset_uri = context.Request.RawUrl.ToString().TrimEnd('/');
			string[] request_uri = subset_uri.Split('/');

			for (int i = request_uri.Length - 1; i > 0; --i)
			{
				subset_uri = String.Join("/", request_uri.Take(i + 1));

				if (this.m_handlers.ContainsKey(subset_uri))
				{
					handler = this.m_handlers[subset_uri];

					/* Finish up here */
					break;
				}
			}

			context.Response.SendChunked = true;
			try
			{
				handler.Serve(context.Request, context.Response, new Uri(context.Request.Url.ToString().Substring(subset_uri.Length)));
				context.Response.StatusCode = handler.StatusCode;
			}
			catch (Exception)
			{
				this.m_500Handler.Serve(context.Request, context.Response, new Uri(context.Request.Url.ToString().Substring(subset_uri.Length)));
				context.Response.StatusCode = this.m_500Handler.StatusCode;
			}
			
			context.Response.OutputStream.Close();
		}
	}
}