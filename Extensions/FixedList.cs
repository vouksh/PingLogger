using System;
using System.Collections.Generic;
using System.Text;

namespace PingLogger.Extensions
{
	public class FixedList<T> : List<T>
	{
		public int MaxSize { get; set; }
		public FixedList(int maxSize = 0)
		{
			MaxSize = maxSize;
		}
		private readonly Queue<T> orderedQueue = new Queue<T>();

		public new void Add(T obj)
		{
			orderedQueue.Enqueue(obj);

			if(MaxSize > 0 && orderedQueue.Count >= MaxSize)
			{
				Remove(orderedQueue.Dequeue());
			}

			base.Add(obj);
		}

		public new void Clear()
		{
			orderedQueue.Clear();
			base.Clear();
		}
	}
}
