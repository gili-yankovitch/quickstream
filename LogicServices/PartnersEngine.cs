using DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LogicServices
{
	public class PartnersEngine
	{
		public static List<string> Partners = new List<string>();

		public static bool IsMaster = false;

		public static void AddPartner(string Partner)
		{
			if (!Partners.Exists(partner => partner.Equals(Partner)))
			{
				Partners.Add(Partner);
			}
		}
	}
}
