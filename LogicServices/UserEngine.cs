using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DAL;

namespace LogicServices
{
    public class UserEngine : Singleton<UserEngine>
    {
	    private byte[] ComputeHash(byte[] key)
	    {
		    var hash = SHA256.Create();
		    hash.Initialize();

		    return hash.ComputeHash(key);
	    }

	    public int RegisterUser(byte[] key)
	    {
		    var u = new User { Key =  this.ComputeHash(key) };

		    using (var ctx = new MessagingContext())
		    {
			    ctx.Users.Add(u);
			    ctx.SaveChanges();
		    }

		    return u.Id;
	    }

	    public bool Login(int id, byte[] key)
	    {
		    using (var ctx = new MessagingContext())
		    {
			    var u = ctx.Users.Find(id);

			    return (u != null) && (this.ComputeHash(key) == u.Key);
		    }
	    }
    }
}
