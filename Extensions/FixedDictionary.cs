using System.Collections.Generic;

namespace PingLogger.Extensions
{
	public class FixedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
	{
		public FixedDictionary(int maxSize = 0)
		{
			MaxSize = maxSize;
		}

		public int MaxSize { get; set; }
		private readonly Queue<TKey> _orderedKeys = new ();

		public new void Add(TKey key, TValue value)
		{
			_orderedKeys.Enqueue(key);

			if (MaxSize != 0 && Count >= MaxSize)
			{
				Remove(_orderedKeys.Dequeue());
			}

			base.Add(key, value);
		}

		public new void Clear()
		{
			_orderedKeys.Clear();
			base.Clear();
		}
	}
}
