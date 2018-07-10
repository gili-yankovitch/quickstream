using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON.PartnerRequests
{
	[DataContract]
	public class PartnerSyncQueueCreate
	{
		[DataMember] public int UID;

		[DataMember] public int NodeId;

		[DataMember] public string QueueName;

		[DataMember] public List<List<int>> Readers;
	}
}
