using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON.PartnerRequests
{
	[DataContract]
	public class PartnerSyncResponseDBDump : BooleanResponse
	{
		[DataMember] public byte[] DBDump;
		[DataMember] public string[] Partners;
	}
}
