using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicServices
{
	public class Config
	{
		public const long QUEUE_GRACE_PERIOD = 100;

		public const string PUBLIC_ADDRESS = "0.0.0.0";

		public static string[] PARTNERS = { "https://quickstream.io" };

		public const string DB_Filename = "QuickStream.sqlite";
	}
}
