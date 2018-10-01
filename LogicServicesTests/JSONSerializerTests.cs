using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSON;

namespace LogicServices.Tests
{
	[TestClass()]
	public class JSONSerializerTests
	{
		[TestMethod()]
		public void SerializeTestOmitRequired()
		{
			try
			{
				new JSONSerializer<JSONResponse>().Deserialize(Encoding.ASCII.GetBytes("{}"));

				Assert.Fail();
			}
			catch { }
		}

		[TestMethod()]
		public void SerializeTestDeserialize()
		{
			var data = "Hello, world!";

			if (new JSONSerializer<QueueRequest>().Deserialize(new JSONSerializer<QueueRequest>().Serialize(new QueueRequest { NodeId = 0, Id = 0, Action = e_action.E_READ, Commit = true, Data = data, SessionKey = "asdfsdf" })).Data != data)
				Assert.Fail();
		}
	}
}