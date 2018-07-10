using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON.PartnerRequests
{
	[DataContract]
	public class PartnerSyncUserCreate
	{
		[DataMember] public int Id;
		[DataMember] public string Key;
	}
}
