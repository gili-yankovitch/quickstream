using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON
{
	public enum e_action
	{
		E_READ,
		E_WRITE
	};
	

	[DataContract]
	public class QueueRequest : CredentialRequest
	{
		[DataMember] public e_action Action;

		[DataMember(IsRequired = false)] public string Data;

		[DataMember(IsRequired = false)] public bool Commit;

	}
}
