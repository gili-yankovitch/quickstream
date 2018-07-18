using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON
{
	public enum PartnerSyncMessageType
	{
		PARTNER_JOIN,
		DB_DUMP_RSP,
		USER_CREATE,
		QUEUE_CREATE,
		QUEUE_WRITE,
		QUEUE_COMMIT,
		GENERIC_RSP
	};

	[DataContract]
	public class PartnerSyncMessageData
	{
		[DataMember] public PartnerSyncMessageType MessageType;

		[DataMember] public byte[] Data;
	}
}
