using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON
{
	public enum PartnerSyncMessage
	{
		PARTNER_JOIN,
		DB_DUMP_RSP,
		USER_CREATE,
		QUEUE_CREATE,
		QUEUE_WRITE,
		QUEUE_COMMIT
	};

	[DataContract]
	public class PartnerSyncRequestData
	{
		[DataMember] public PartnerSyncMessage MessageType;

		[DataMember] public byte[] Data;
	}
}
