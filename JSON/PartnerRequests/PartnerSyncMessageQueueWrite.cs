using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON
{
	[DataContract]
	public class PartnerSyncRequestQueueWrite
	{
		[DataMember] public int UID;

		[DataMember] public string QueueID;

		[DataMember] public string Data;

		[DataMember] public DateTime Timestamp;
	}
}
