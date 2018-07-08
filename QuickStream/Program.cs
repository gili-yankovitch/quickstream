using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Google.Protobuf;
using LogicServices;
using QuickStream.Handlers;

namespace QuickStream
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			ushort port = 8080;

			if (args.Length == 1)
				try
				{
					port = (ushort) int.Parse(args[0]);
				}
				catch (Exception e)
				{
					Console.WriteLine("Invalid port number " + e);

					return;
				}

			try
			{
				var server = new AsyncHTTPServer(port);
				server.AddHandler("/testQuery", new TestQueryHandler());
				server.AddHandler("/createUser", new CreateUserHandler());
				server.AddHandler("/createQueue", new CreateQueueHandler());
				server.AddHandler("/slaveSync", new SlaveSyncHandler());
				server.AddHandler("/login", new LoginHandler());
				server.AddHandler("/queue", new QueueHandler());

				server.Start();
			}
			catch (HttpListenerException e)
			{
				Console.WriteLine("External port diallowed. Please run as Administrator (" + e.Message + ":");
				Console.WriteLine("\tnetsh http add urlacl url=http://+:" + port + "/ user=\"" + Environment.UserName +
				                  "\"");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Console.WriteLine(e.Message);
			}
		}
	}
}