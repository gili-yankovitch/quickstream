using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream
{
	class HttpUtils
	{
		private const int MAX_CHUNK = 4096;

		public static byte[] GetFullRequestBody(HttpListenerRequest request)
		{
			var s = request.InputStream;
			var sg = new List<byte[]>();
			var total = 0;

			while (true)
			{
				var readBuf = new byte[MAX_CHUNK];
				var t = s.ReadAsync(readBuf, 0, MAX_CHUNK);
				t.Wait();

				if (t.Result == 0)
					break;

				total += t.Result;

				if (t.Result < MAX_CHUNK)
				{
					sg.Add(readBuf.Take(t.Result).ToArray());
				}
				else
				{
					sg.Add(readBuf);
				}
			}

			var full = new byte[total];
			var off = 0;

			foreach (var b in sg)
			{
				b.CopyTo(full, off);
				off += b.Length;
			}

			return full;
		}
	}
}
