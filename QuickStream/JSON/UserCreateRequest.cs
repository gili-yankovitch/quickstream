using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream.JSON
{
	[DataContract]
	internal class UserCreateRequest : JSONApi
	{
		[DataMember] internal byte[] Key;
	}
}
