using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DAL;

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

	        using (MessagingContext ctx = new MessagingContext())
	        {
				Message m = new Message
		        {
			        Content = Encoding.ASCII.GetBytes("Hello, world!")
		        };

		        MsgQueue mq0 = new MsgQueue
		        {
			        Messages = new List<Message>
			        {
				        m
			        },
			        Name = "My First Real Queue",
			        Readers = new List<User>()
		        };

		        MsgQueue mq1 = new MsgQueue
		        {
			        Name = "Second Queue",
			        Readers = new List<User>()
		        };

		        User u = new User
		        {
			        Key = Encoding.ASCII.GetBytes("00010203040506070809"),
			        Queues = new List<MsgQueue>()
			        {
				        mq0, mq1
			        }
		        };

		        mq0.Readers.Add(u);

		        ctx.MsgQueues.Add(mq0);
		        ctx.MsgQueues.Add(mq1);
		        ctx.Users.Add(u);
		        ctx.Messages.Add(m);

				ctx.SaveChanges();
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
