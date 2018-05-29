using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream
{
    class Program
    {
        static void Main(string[] args)
        {
			ushort port = 8080;

			if (args.Length == 1)
			{
				try
				{
					port = (ushort)Int32.Parse(args[0]);
				}
				catch(Exception e)
				{
					Console.WriteLine("Invalid port number " + e);

					return;
				}
			}

			try
			{
				AsyncHTTPServer server = new AsyncHTTPServer(port);
				server.AddHandler("/createQueueNamespace", new CreateQueueNamespaceHandler());

				server.Start();
			}
			catch (HttpListenerException e)
			{
				Console.WriteLine("External port diallowed. Please run as Administrator (" + e.Message + ":");
				Console.WriteLine("\tnetsh http add urlacl url=http://+:" + port + "/ user=\"" + Environment.UserName  + "\"");

			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
        }
    }
}
