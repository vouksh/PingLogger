using System;
using System.Collections.Generic;
using System.Text;

namespace PingLogger.Extensions
{
	public class FixedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
	{
		public FixedDictionary(int maxSize = 0)
		{
			this.MaxSize = maxSize;
		}

		public int MaxSize { get; set; }
		private Queue<TKey> orderedKeys = new Queue<TKey>();

		public new void Add(TKey key, TValue value)
		{
			orderedKeys.Enqueue(key);

			if(MaxSize != 0 && Count >= MaxSize)
			{
				Remove(orderedKeys.Dequeue());
			}

			base.Add(key, value);
		}
	}
}
