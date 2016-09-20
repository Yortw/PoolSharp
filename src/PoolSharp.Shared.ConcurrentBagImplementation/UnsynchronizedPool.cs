﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PoolSharp
{
	/// <summary>
	/// A pool implementation designed strictly for single-threaded access.
	/// </summary>
	/// <remarks>
	/// <para>As this implementation does not use any synchronisation methods it provides best performance but is only safe when accessed by a single thread. It is ideal for single threaded scenarios such as game loops where only one thread access the pool, but a pool is still helpful.</para>
	/// <para>This pool does not block when a new item is requested and the pool is empty, instead a new will be allocated and returned.</para>
	/// <para>By default the pool starts empty and items are allocated as needed. The <see cref="Expand()"/> method can be used to pre-load the pool if required.</para>
	/// <para>Objects returned to the pool are added on a first come first serve basis. If the pool is full when an object is returned, it is ignored (and will be garbage collected if there are no other references to it). In this case, if the item implements <see cref="IDisposable"/> the pool will ensure the item is disposed before being 'ignored'.</para>
	/// <para>Disposing the pool will also dispose all objects currently in the pool, if they support <see cref="IDisposable"/>.</para>
	/// <para>This pool does not support the <seealso cref="PooledItemInitialization.AsyncReturn"/> initialization policy option. If this option is specified the constructor will throw a <seealso cref="System.ArgumentException"/>.</para>
	/// <para>This pool requires an explicit (non-zero) maximum size. Items are not automatically pre-allocated unless the <see cref="Expand()"/> method is called, but an internal array of the maximum size is created at construction time to avoid resizing later.</para>
	/// <para>This pool is 'allocation light' compared to the default <see cref="Pool{T}"/> implementation, which while efficient for multi-threaded scenarios, 'allocates' on returning items to the pool.</para>
	/// </remarks>
	/// <typeparam name="T">The type of value being pooled. Must be a reference type.</typeparam>
	/// <seealso cref="PoolPolicy{T}"/>
	/// <seealso cref="IPool{T}"/>
	/// <seealso cref="PooledObject{T}"/>
	public class UnsynchronizedPool<T> : PoolBase<T> where T : class
	{

		#region Fields

		private T[] _PoolItems;
		private int _PoolItemIndex;

		#endregion

		#region Constructors

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="poolPolicy">A <seealso cref="PoolPolicy{T}"/> instance containing configuration information for the pool.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="poolPolicy"/> argument is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the <see cref="PoolPolicy{T}.Factory"/> property of the <paramref name="poolPolicy"/> argument is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the <see cref="PoolPolicy{T}.InitializationPolicy"/> property is set to <see cref="PooledItemInitialization.AsyncReturn"/>.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the <see cref="PoolPolicy{T}.MaximumPoolSize"/> property is less than or equal to zero.</exception>
		public UnsynchronizedPool(PoolPolicy<T> poolPolicy) : base(poolPolicy)
		{
			if (poolPolicy.InitializationPolicy == PooledItemInitialization.AsyncReturn) throw new ArgumentException("poolPolicy.Factory cannot be PooledItemInitialization.AsyncReturn.", nameof(poolPolicy));
			if (poolPolicy.MaximumPoolSize <= 0) throw new ArgumentException("A maximum pool size must be specified.", nameof(poolPolicy));

			_PoolItemIndex = -1;
			_PoolItems = new T[poolPolicy.MaximumPoolSize];
		}

		#endregion

		#region IDisposable & Related Implementation 

		/// <summary>
		/// Performs dispose logic, can be overridden by derivded types.
		/// </summary>
		/// <param name="disposing">True if the pool is being explicitly disposed, false if it is being disposed from a finalizer.</param>
		/// <seealso cref="PoolBase{T}.Dispose()"/>
		/// <seealso cref="PoolBase{T}.IsDisposed"/>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (IsPooledTypeDisposable)
				{
					while (_PoolItemIndex >= 0)
					{
						var item = _PoolItems[_PoolItemIndex];
						SafeDispose(item);
						_PoolItemIndex--;
					}

					_PoolItems = null;
				}
			}
		}

		#endregion

		#region Pool Methods

		/// <summary>
		/// Returns/adds an object to the pool so it can be reused.
		/// </summary>
		/// <param name="value"></param>
		/// <remarks>
		/// <para>Items will be returned to the pool if it is not full, otherwise no action is taken and no error is reported.</para>
		/// <para>If the item is NOT returned to the pool, and {T} implements <see cref="System.IDisposable"/>, the instance will be disposed before the method returns.</para>
		/// <para>Calling this method on a disposed pool will dispose the returned item if it supports <see cref="IDisposable"/>, but takes no other action and throws no error.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="value"/> is null.</exception>
		/// <exception cref="InvalidOperationException">Thrown if the <see cref="PoolPolicy{T}.ErrorOnIncorrectUsage"/> is true and the same instance already exists in the pool.</exception>
		public override void Add(T value)
		{
			if (value == null) throw new ArgumentNullException(nameof(value));

			if (!IsDisposed && ShouldReturnToPool(value))
			{
				if (PoolPolicy.InitializationPolicy == PooledItemInitialization.Return && PoolPolicy.ReinitializeObject != null)
					PoolPolicy.ReinitializeObject(value);

				AddCore(value);
			}
			else
				SafeDispose(value);
		}

		/// <summary>
		/// Fills the pool up to it's maximum size with pre-generated instances.
		/// </summary>
		public override void Expand()
		{
			Expand(PoolPolicy.MaximumPoolSize);
		}

		/// <summary>
		/// Expands the pool up by the <paramref name="increment"/> value, but not past it's maximum size, with pre-generated instances.
		/// </summary>
		/// <param name="increment"></param>
		public override void Expand(int increment)
		{
			CheckDisposed();

			if (increment <= 0) return;

			int createdCount = 0;
			while (createdCount < increment && !IsPoolFull())
			{
				AddCore(PoolPolicy.Factory(this));
				createdCount++;
			}
		}

		/// <summary>
		/// Gets an item from the pool.
		/// </summary>
		/// <remarks>
		/// <para>If the pool is empty when the request is made, a new item is instantiated and returned. Otherwise an instance from the pool will be used.</para>
		/// </remarks>
		/// <returns>Returns an instance of {T} from the pool, or a new instance if the pool is empty.</returns>
		/// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
		public override T Take()
		{
			CheckDisposed();

			T retVal = null;

			if (_PoolItemIndex >= 0)
			{
				retVal = _PoolItems[_PoolItemIndex];
				_PoolItems[_PoolItemIndex] = null;
				_PoolItemIndex--;

				if (retVal != null && PoolPolicy.InitializationPolicy == PooledItemInitialization.Take && PoolPolicy.ReinitializeObject != null)
					PoolPolicy.ReinitializeObject(retVal);
			}
			
			if (retVal == null)
				retVal = PoolPolicy.Factory(this);		

			return retVal;
		}

		#endregion

		#region Private Methods

		private bool ShouldReturnToPool(T pooledObject)
		{
			if (PoolPolicy.ErrorOnIncorrectUsage && _PoolItems.Contains(pooledObject))
				throw new InvalidOperationException("Object already exists in pool. Duplicate add detected.");

			return !IsPoolFull();
		}

		private bool IsPoolFull()
		{
			return (_PoolItems != null && _PoolItemIndex >= _PoolItems.Length -1);
		}

		private void AddCore(T value)
		{
			_PoolItemIndex++;
			_PoolItems[_PoolItemIndex] = value;
		}

		#endregion

	}
}