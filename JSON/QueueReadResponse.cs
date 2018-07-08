using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace JSON
{
	[DataContract]
	public class QueueReadResponse : BooleanResponse
	{
		[DataMember]
		public List<string> Messages;
	}
}
