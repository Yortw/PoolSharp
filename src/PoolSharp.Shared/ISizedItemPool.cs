using System;
using System.Collections.Generic;
using System.Text;

namespace PoolSharp
{
	/// <summary>
	/// Interface for pools that contain items of varying 'sizes', such as arrays, string builders, streams etc.
	/// </summary>
	/// <typeparam name="T">The type of item to be pooled.</typeparam>
	public interface ISizedItemPool<T> : IPool<T>
	{
		/// <summary>
		/// Returns an item from the pool, or a new instance if the pool is empty, that is *at least* the size of <paramref name="minimumSize"/>.
		/// </summary>
		/// <param name="minimumSize">The minimum 'size' of the item to return, in whatever units are appropriate (usually bytes).</param>
		/// <returns>An instance of {T}.</returns>
		T Take(int minimumSize);
	}
}