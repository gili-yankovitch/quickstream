using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogicServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL;
using System.IO;
using System.Data.SQLite;
using System.Data;

namespace LogicServices.Tests
{
	[TestClass()]
	public class UserEngineTests
	{
		[TestInitialize()]
		public void InitializeTest()
		{
			using (SQLiteConnection connect = new SQLiteConnection(@"Data Source=QuickStream.sqlite"))
			{
				connect.Open();

				foreach (var table in new string[] { "Users", "Readers", "Messages", "Queues", "QueueBuffers" })
				{
					using (SQLiteCommand fmd = connect.CreateCommand())
					{
						fmd.CommandText = @"DELETE FROM " + table;
						fmd.CommandType = CommandType.Text;
						try
						{
							fmd.ExecuteNonQuery();
						}
						catch (Exception e)
						{

						}
					}
				}
			}
		}

		[TestMethod()]
		public void RegisterUserTestOneUser()
		{
			var u = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			if (u != 0)
				Assert.Fail("Unexpected UID Registeration");
		}

		[TestMethod()]
		public void RegisterUserTestMultipleUsers()
		{
			new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));
			var u = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes("Aa123456"));

			if (u != 2)
				Assert.Fail("Unexpected UID Registeration");
		}

		[TestMethod()]
		public void LoginTest()
		{
			var pass = "Aa123456";

			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes(pass));

			if (!new UserEngine().Login(0, uid, pass))
			{
				Assert.Fail("Failed login");
			}
		}

		[TestMethod()]
		public void LoginTestNegative()
		{
			var pass = "Aa123456";

			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes(pass));

			if (new UserEngine().Login(0, uid, pass + "A"))
			{
				Assert.Fail("Successful login with erronous credentials");
			}
		}

		[TestMethod()]
		public void LoginTestNegativeNoUser()
		{
			var pass = "Aa123456";

			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes(pass));

			if (new UserEngine().Login(0, uid + 1, pass))
			{
				Assert.Fail("Successful login with erronous credentials");
			}
		}

		[TestMethod()]
		public void LoginBySessionIdTest()
		{
			var pass = "Aa123456";

			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes(pass));

			var session_id = new UserEngine().generateSessionKey(uid, 0);

			if (!new UserEngine().LoginBySessionId(0, uid, session_id))
			{
				Assert.Fail("Successful login with erronous credentials");
			}
		}

		[TestMethod()]
		public void LoginBySessionIdTestNegative()
		{
			var pass = "Aa123456";

			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes(pass));

			var session_id = new UserEngine().generateSessionKey(uid, 0);

			if (new UserEngine().LoginBySessionId(0, uid, session_id + "A"))
			{
				Assert.Fail("Successful login with erronous credentials");
			}
		}

		[TestMethod()]
		public void LoginBySessionIdTestNegativeNoUser()
		{
			var pass = "Aa123456";

			var uid = new UserEngine().RegisterUser(0, Encoding.ASCII.GetBytes(pass));

			var session_id = new UserEngine().generateSessionKey(uid, 0);

			if (new UserEngine().LoginBySessionId(0, uid + 1, session_id))
			{
				Assert.Fail("Successful login with erronous credentials");
			}
		}
	}
}