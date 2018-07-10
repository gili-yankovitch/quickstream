using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON
{
	[DataContract]
	public class UserCreateResponse : JSONResponse
	{
		[DataMember] public int Id;
		[DataMember] public int NodeId;
	}
}
