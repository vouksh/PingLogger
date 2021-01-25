using System.Collections.Generic;

namespace PingLogger.Extensions
{
	public class FixedList<T> : List<T>
	{
		public int MaxSize { get; set; }
		public FixedList(int maxSize = 0)
		{
			MaxSize = maxSize;
		}
		private readonly Queue<T> _orderedQueue = new ();

		public new void Add(T obj)
		{
			_orderedQueue.Enqueue(obj);

			if (MaxSize > 0 && _orderedQueue.Count >= MaxSize)
			{
				Remove(_orderedQueue.Dequeue());
			}

			base.Add(obj);
		}

		public new void Clear()
		{
			_orderedQueue.Clear();
			base.Clear();
		}
	}
}
