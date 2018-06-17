using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicServices
{
	public abstract class Singleton<T> where T : class, new()
	{
		private static T _instance = null;

		public static T GetInstance()
		{
			if (_instance == null)
			{
				_instance = new T();
			}

			return _instance;
		}

		protected Singleton() { }
	}
}
