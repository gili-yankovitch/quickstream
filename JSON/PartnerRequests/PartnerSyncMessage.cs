using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON
{
	[DataContract]
	public class PartnerSyncRequest
	{
		[DataMember] public byte[] cert;
		[DataMember] public int certId;
		[DataMember] public byte[] key;
		[DataMember] public byte[] data;
		[DataMember] public byte[] signature;
	}
}
