package QuickStream.Handlers;

import java.io.IOException;
import java.io.OutputStream;
import java.net.InetSocketAddress;

import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;

public class QueueHandler implements HttpHandler
{
	@Override
	public void handle(HttpExchange t) throws IOException
	{
		String[] path = t.getRequestURI().toString().split("/");
		String response = "Response for path: ";
		String tabs = "";
		for (int i = 0; i < path.length; ++i)
		{
			response += "\n" + tabs + path[i] + "/";
			tabs += "\t";
		}
		t.sendResponseHeaders(200, response.length());
		OutputStream os = t.getResponseBody();
		os.write(response.getBytes());
		os.close();
	}
}
