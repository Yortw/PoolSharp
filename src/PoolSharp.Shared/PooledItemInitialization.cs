using System;
using System.Collections.Generic;
using System.Text;

namespace PoolSharp
{
	/// <summary>
	/// Provides options for when and how pooled items are re-initialised before reuse.
	/// </summary>
	/// <seealso cref="PoolPolicy{T}"/>
	public enum PooledItemInitialization
	{
		/// <summary>
		/// Default. Items are re-initialised synchronously when returned to the pool. Reduces latency on operations requesting items from the pool, while ensuring items in the pool are not holding references that would be cleared by re-initialisation. Additionally this may return items to the pool faster than <see cref="AsyncReturn"/> as more than one thread can perform re-initialisation of independent values at a time, and this in turn may reduce allocations in busy pools.
		/// </summary>
		Return = 0,
		/// <summary>
		/// <para>Items are re-initialised asynchronously before being returned to the pool.</para>
		/// <para>This minimises time spent waiting on the pool either taking or returning items, but risks more allocations if items are requested from the pool while returned items are still being re-initialised. Recommended if re-initalisation is time consuming.</para>
		/// </summary>
		AsyncReturn,
		/// <summary>
		/// <para>Items are re-initialised as they are retrieved from the pool. This can impose a performance penalty retrieving an item from the pool, but reduces the chance of a new allocation due to the pool being empty. Recommended if re-initialisation is fast.</para>
		/// </summary>
		Take
	}
}