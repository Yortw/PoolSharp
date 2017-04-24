using System;
using System.Collections.Generic;
using System.Text;

namespace PoolSharp
{
	/// <summary>
	/// A wrapper for a pooled object that allows for easily retrieving and returning the item to the pool via the using statement.
	/// </summary>
	/// <remarks>
	/// <para>In order to make a pool more convenient to use, the pool can contain <see cref="PooledObject{T}"/> references instead of direct {T} references.</para>
	/// <para>When <see cref="Dispose"/> is called on a <see cref="PooledObject{T}"/> instance, it is returned to the associated pool automatically.</para>
	/// <code>
	/// using (var wrapper = _Pool.Take())
	/// {
	///		DoSomethingWithValue(wrapper.Value);
	/// } // Wrapper and it's value will be returned to the pool here.
	/// </code>
	/// </remarks>
	/// <typeparam name="T">The type of value being pooled.</typeparam>
	public sealed class PooledObject<T> : IDisposable
	{
		private readonly IPool<PooledObject<T>> _Pool;
		private readonly T _Value;

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="pool">A reference to the pool the wrapper should be returned to when <see cref="Dispose"/> is called.</param>
		/// <param name="value">The actual value of interest to the caller.</param>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
		public PooledObject(IPool<PooledObject<T>> pool, T value)
		{
			if (pool == null) throw new ArgumentNullException(nameof(pool));
			if (value == null) throw new ArgumentNullException(nameof(value));

			_Pool = pool;
			_Value = value;
		}

		/// <summary>
		/// The actual value of interest.
		/// </summary>
		public T Value { get { return _Value; } }

		/// <summary>
		/// Rather than disposing the wrapper or the <see cref="Value"/>, returns the wrapper to the pool specified in the wrapper's constructor.
		/// </summary>
		public void Dispose()
		{
			try
			{
				_Pool.Add(this);
			}
			catch (ObjectDisposedException)
			{
				(this.Value as IDisposable)?.Dispose();
			}
		}
	}
}