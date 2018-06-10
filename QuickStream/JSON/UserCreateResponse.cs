using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuickStream.JSON
{
	[DataContract]
	internal class UserCreateResponse : JSONApi
	{
		[DataMember] internal int Id;
	}
}
