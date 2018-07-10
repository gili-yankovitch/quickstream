using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON.PartnerRequests
{
	[DataContract]
	public class PartnerSyncQueueWrite
	{
		[DataMember] public int UID { get; set; }

		[DataMember] public int NodeId { get; set; }

		[DataMember] public string QueueName { get; set; }

		[DataMember] public string Data { get; set; }

		[DataMember] public DateTime Timestamp { get; set; }
	}
}
