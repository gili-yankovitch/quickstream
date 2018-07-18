using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace LogicServices
{
	public class JSONSerializer<T>
	{
		public static byte[] Serialize(T obj)
		{
			/* Create the stream */
			var workStream = new MemoryStream();

			/* Serialize */
			new DataContractJsonSerializer(typeof(T)).WriteObject(workStream,
								obj);

			/* Rewind the stream */
			workStream.Seek(0, SeekOrigin.Begin);

			/* Create a new byte array */
			var outputData = new byte[workStream.Length];

			/* Read from stream */
			workStream.Read(outputData, 0, (int)workStream.Length);

			return outputData;
		}

		public static Stream Serialize(T obj, Stream s)
		{
			/* Serialize */
			new DataContractJsonSerializer(typeof(T)).WriteObject(s, obj);

			return s;
		}

		public static T Deserialize(byte[] input)
		{
			/* Create the stream */
			var workStream = new MemoryStream();

			/* Write to stream */
			workStream.Write(input, 0, input.Length);

			/* Rewind stream caret */
			workStream.Seek(0, SeekOrigin.Begin);

			/* Write to the stream */
			return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(workStream);
		}

		public static T Deserialize(Stream s)
		{
			return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(s);
		}
	}
}
