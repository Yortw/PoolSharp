using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoolSharp.Tests
{
	public sealed class DisposableTestPoolItem : TestPoolItem, IDisposable
	{
		public bool IsDisposed { get; set; }

		public void Dispose()
		{
			IsDisposed = true;
		}
	}
}