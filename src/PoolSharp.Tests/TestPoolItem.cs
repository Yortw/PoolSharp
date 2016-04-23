using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace PoolSharp.Tests
{
	public class TestPoolItem
	{
		public static int InstanceCount;

		public TestPoolItem()
		{
			Interlocked.Increment(ref InstanceCount);
		}

		public Guid Id { get; set; } = Guid.NewGuid();
		public DateTime Date { get; set; } = DateTime.Now;
		public int ResetByThreadId { get; set; }
	}
}