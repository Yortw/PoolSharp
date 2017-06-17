using System;
using System.Collections.Generic;
using System.Text;

namespace PoolSharp
{
	/// <summary>
	/// Interface for pools that contain items of varying 'sizes', such as arrays, string builders, streams etc. where items are requested based on a named size rather than a specific measurement.
	/// </summary>
	/// <typeparam name="T">The type of value to be pooled.</typeparam>
	public interface IRelativeSizedItemPool<T> : ISizedItemPool<T>
	{
		/// <summary>
		/// Returns an item from the pool, or a new instance if the pool is empty, that is sized according to the relative size specified by <paramref name="size"/>.
		/// </summary>
		/// <param name="size">A value from the <see cref="RelativeSize"/> enum specifying the relative/estimate size of the pooled item required.</param>
		/// <returns>An instance of {T}.</returns>		
		T Take(RelativeSize size);

		/// <summary>
		/// Returns the maximum size of a pooled item of the given <paramref name="size"/>.
		/// </summary>
		/// <param name="size">A value from the <see cref="RelativeSize"/> enum specifying the relative/estimate size of the pooled item required.</param>
		/// <returns>An integer specifying the actual size used in this pool for the <see cref="RelativeSize"/> specified, in the unit of measurement typically used for {T} (bytes for most object types, but may vary).</returns>
		int ActualSize(RelativeSize size);
	}

	/// <summary>
	/// An enum whose members describe a relative rather than specific size, i.e small vs large.
	/// </summary>
	public enum RelativeSize
	{
		/// <summary>
		/// The smallest possible relative size.
		/// </summary>
		Micro = 0,
		/// <summary>
		/// The second smallest possible relative size.
		/// </summary>
		Small,
		/// <summary>
		/// An average or normal size, neither small nor big.
		/// </summary>
		Medium,
		/// <summary>
		/// The second largest possible size.
		/// </summary>
		Large,
		/// <summary>
		/// The largest possible size.
		/// </summary>
		Huge
	}
}