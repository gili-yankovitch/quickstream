using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicServices
{
	public class Config<T> : Singleton<Config<T>>
	{
		private const string DEFAULT_RULE = "DEFAULT_RULE";
		private const string CONFIG_FILE = "QuickStream.ini";

		private Ini config = null;

		Dictionary<string, Func<object, object>> rules;

		public Config()
		{
			this.rules = new Dictionary<string, Func<object, object>>();
			this.rules["QUEUE_GRACE_PERIOD"] = new Func<object, object>(target => { return int.Parse(target.ToString()); });
			this.rules["QUEUE_DATA_SIZE"] = new Func<object, object>(target => { return int.Parse(target.ToString()); });
			this.rules["QUEUE_MESSAGE_MAX_AGE"] = new Func<object, object>(target => { return int.Parse(target.ToString()); });
			this.rules["PARTNERS"] = new Func<object, object>(target => { return target.ToString().Split(','); });
			this.rules[DEFAULT_RULE] = new Func<object, object>(target => { return target.ToString(); });
		}

		public T this[string key]
		{
			get
			{
				if (this.config == null)
				{
					this.config = new Ini(CONFIG_FILE);
					this.config.Load();
				}

				/* Just return regular strings */
				if (!this.rules.Keys.Contains(key))
				{
					return (T)this.rules[DEFAULT_RULE](this.config.GetValue(key));
				}

				return (T)this.rules[key](this.config.GetValue(key));

			}

			set
			{
				if (this.config == null)
				{
					this.config = new Ini(CONFIG_FILE);
					this.config.Load();
				}

				this.config.WriteValue(key, value.ToString());

				this.config.Save();
			}
		}
	}
}
