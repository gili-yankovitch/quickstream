package QuickStream;

import java.io.IOException;
import java.io.OutputStream;
import java.net.InetSocketAddress;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;

import QuickStream.Handlers.*;

public class Main
{
	public static void main(String[] args)
	{
		try
		{
			System.out.println("Running server at port: " + args[0]);
			HttpServer server = HttpServer.create(new InetSocketAddress(Integer.parseInt(args[0])), 0);
			server.createContext("/queue", new QueueHandler());
			server.setExecutor(null); // creates a default executor
			server.start();
		}
		catch (IOException e)
		{
			System.out.println("Error: " + e);
		}
	}
}
