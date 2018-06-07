using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DAL;

namespace QuickStream
{
    class Program
    {
	    static void InitializeTestDb(MessagingContext context)
	    {
		    context.Set<User>().Add(new User
		    {
			    Id = 0,
			    Key = Encoding.ASCII.GetBytes("00010203040506070809")
		    });

		    context.Set<Message>().Add(new Message
		    {
				Id = 0,
			    Content = Encoding.ASCII.GetBytes("Hello, world!")
			});

		    context.Set<MsgQueue>().Add(new MsgQueue
		    {
				Id = 0,
				Messages = new List<Message>
				{
					context.Set<Message>().First()
				},
				Name = "First Queue",
				Readers = new List<User>
				{
					context.Set<User>().First()
				}
		    });

		    context.Set<Namespace>().Add(new Namespace
		    {
				Id = 0,
				Owner = context.Set<User>().First(),
				Queues = new List<MsgQueue>
				{
					context.Set<MsgQueue>().First()
				}
		    });
	    }

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

	        InitializeTestDb(new MessagingContext());

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
