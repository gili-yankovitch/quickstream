using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DAL;

namespace LogicServices
{
    public class UserEngine
    {
	    private static byte[] ComputeHash(byte[] key)
	    {
		    var hash = SHA256.Create();
		    hash.Initialize();

		    return hash.ComputeHash(key);
	    }

		public static int RegisterUser(byte[] key)
		{
			var u = new User { Key = ComputeHash(key), Queues = new List<MsgQueue>() };

		    using (var ctx = new MessagingContext())
		    {
			    ctx.Users.Add(u);
			    ctx.SaveChanges();
		    }

		    return u.Id;
	    }

	    public static bool Login(int id, string key)
	    {
		    using (var ctx = new MessagingContext())
		    {
			    var u = ctx.Users.Find(id);

			    return (u != null) && (u.Key.SequenceEqual(ComputeHash(Encoding.ASCII.GetBytes(key))));
		    }
	    }

		public static bool LoginBySessionId(int id, string sess)
		{
			using (var ctx = new MessagingContext())
			{
				var u = ctx.Users.Find(id);

				if (u?.SessionKey != sess)
					return false;

				return true;
			}
		}

		private const int SESSION_KEY_LEN = 32;

		private static string _generateSessionKey()
		{
			var map = "0123456789abcdefghijklmnopqrstuvwxyz";
			var sb = new StringBuilder();
			var r = new Random((int)DateTime.Now.Ticks);

			for (int i = 0; i < SESSION_KEY_LEN; ++i)
			{
				sb.Append(map[r.Next() % map.Length]);
			}

			return sb.ToString();
		}

		public static string generateSessionKey(int id)
		{
			var sess = _generateSessionKey();

			using (var ctx = new MessagingContext())
			{
				while (ctx.Users.FirstOrDefault(user => (user.SessionKey == sess)) != null)
				{
					sess = _generateSessionKey();
				}

				var u = ctx.Users.Find(id);

				u.SessionKey = sess;

				ctx.SaveChanges();
			}

			return sess;
		}
    }
}
