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
		/// <para>Items are re-initialised asynchronously before being returned to the pool. A single long running thread is used to re-initialise items before returning them to the pool. The use of a single thread prevents thread pool overloading when many/very busy pools are used, though can mean re-initialisation and returning items to the pool can take longer as the re-initialisation is effectively serialised.</para>
		/// <para>This minimises time spent waiting on the pool either taking or returning items, but risks more allocations if items are requested from the pool while returned items are still being re-initialised. Recommended if re-initalisation is time consuming.</para>
		/// <para>Note: On platforms (such as WinRT/UWP) where threads are not exposed by .Net runtime and only <see cref="System.Threading.Tasks.Task"/> is available, this effectively consumes a threadpool thread for the life of each pool (or slighlty longer). On those platforms we recommend using another option unless you have specific reason to use this one, particularly if you have many different pools in use.</para>
		/// </summary>
		AsyncReturn,
		/// <summary>
		/// <para>Items are re-initialised as they are retrieved from the pool. This can impose a performance penalty retrieving an item from the pool, but reduces the chance of a new allocation due to the pool being empty. Recommended if re-initialisation is fast.</para>
		/// </summary>
		Take
	}
}