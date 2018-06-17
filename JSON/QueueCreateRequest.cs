using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON
{
	[DataContract]
	public class QueueCreateRequest
	{
		[DataMember] public int Id;

		[DataMember] public byte[] Key;

		[DataMember] public string QueueName;
	}
}
