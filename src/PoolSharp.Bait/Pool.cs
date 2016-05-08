using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoolSharp
{
	/// <summary>
	/// A simple non-blocking object pool.
	/// </summary>
	/// <remarks>
	/// <para>This pool does not block when a new item is requested and the pool is empty, instead a new will be allocated and returned.</para>
	/// </remarks>
	/// <para>By default the pool starts empty and items are allocated as needed. The <see cref="Expand()"/> method can be used to pre-load the pool if required.</para>
	/// <para>Objects returned to the pool are taken on a first come first serve basis. If the pool is full when an object is returned, it is ignored (and will be garbage collected if there are no other references to it). In this case, if the item implements <see cref="IDisposable"/> the pool will ensure it is disposed before being 'ignored'.</para>
	/// <para>The pool makes a best effort attempt to avoid going over the specified <see cref="PoolPolicy{T}.MaximumPoolSize"/>, but does not strictly enforce it. Under certain multi-threaded scenarios it's possible for a few items more than the maximum to be kept in the pool.</para>
	/// <para>Disposing the pool will also dispose all objects currently in the pool, if they support <see cref="IDisposable"/>.</para>
	/// <typeparam name="T">The type of value being pooled.</typeparam>
	/// <seealso cref="PoolPolicy{T}"/>
	/// <seealso cref="IPool{T}"/>
	/// <seealso cref="PooledObject{T}"/>
	public class Pool<T> : IPool<T>, IDisposable
	{

		#region Constructors

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="poolPolicy">A <seealso cref="PoolPolicy{T}"/> instance containing configuration information for the pool.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="poolPolicy"/> argument is null.</exception>
		/// <exception cref="ArgumentException">Thrown if the <see cref="PoolPolicy{T}.Factory"/> property of the <paramref name="poolPolicy"/> argument is null.</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "poolPolicy")]
		public Pool(PoolPolicy<T> poolPolicy)
		{
			ExceptionHelper.ThrowYoureDoingItWrong();
		}

		#endregion

		#region IPool<T> Members

		/// <summary>
		/// Returns an item from the pool.
		/// </summary>
		/// <remarks>
		/// <para>If the pool is empty when the request is made, a new item is instantiated and returned. Otherwise an instance from the pool will be used.</para>
		/// <para>This method is thread safe.</para>
		/// </remarks>
		/// <returns>Returns an instance of {T} from the pool, or a new instance if the pool is empty.</returns>
		/// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
		public T Take()
		{
			ExceptionHelper.ThrowYoureDoingItWrong();
			return default(T);
		}

		/// <summary>
		/// Returns/adds an object to the pool so it can be reused.
		/// </summary>
		/// <param name="value">The instance to return to the pool.</param>
		/// <remarks>
		/// <para>Items will be returned to the pool if it is not full and the item is not already in the pool, otherwise no action is taken and no error is reported.</para>
		/// <para>If the policy for the pool specifies <see cref="PooledItemInitialization.AsyncReturn"/> the item will be queued for re-intialisation on a background thread before being returned to the pool, control will return to the caller once the item has been queued even if it has not yet been fully re-initialised and returned to the pool.</para>
		/// <para>If the item is NOT returned to the pool, and {T} implements <see cref="System.IDisposable"/>, the instance will be disposed before the method returns.</para>
		/// <para>Calling this method in a disposed pool will dispose the returned item if it supports <see cref="IDisposable"/>, but takes no other action and throws no error.</para>
		/// <para>This method is 'thread safe', though it is possible for multiple threads returning items at the same time to add items beyond the maximum pool size. This should be rare and have few ill effects. Over time the pool will likely return to it's normal size.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="value"/> is null.</exception>
		public void Add(T value)
		{
			ExceptionHelper.ThrowYoureDoingItWrong();
		}

		/// <summary>
		/// Creates new items and adds them to the pool up to it's maximum capacity.
		/// </summary>
		/// <remarks>
		/// <para>This method is 'thread safe', though it is possible under certain race conditons for the pool to go beyond it's configured maximum size by a few items.</para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">Thrown if this method is called on a disposed pool.</exception>
		public void Expand()
		{
			ExceptionHelper.ThrowYoureDoingItWrong();
		}

		/// <summary>
		/// Creates as many new items as specified by <paramref name="increment"/> and adds them to the pool, but not over it's maximum capacity.
		/// </summary>
		/// <param name="increment">The maximum number of items to pre-allocate and add to the pool.</param>
		/// <remarks>
		/// <para>This method is 'thread safe', though it is possible under certain race conditons for the pool to go beyond it's configured maximum size by a few items.</para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">Thrown if this method is called on a disposed pool.</exception>
		public void Expand(int increment)
		{
			ExceptionHelper.ThrowYoureDoingItWrong();
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Throws a <see cref="ObjectDisposedException"/> if the <see cref="Dispose()"/> method has been called.
		/// </summary>
		protected void CheckDisposed()
		{
			ExceptionHelper.ThrowYoureDoingItWrong();
		}

		#endregion

		#region IDisposable & Related Implementation 

		/// <summary>
		/// Returns a boolean indicating if this pool is disposed or not.
		/// </summary>
		/// <seealso cref="Dispose()"/>
		/// <seealso cref="Dispose(bool)"/>
		public bool IsDisposed
		{
			get
			{
				ExceptionHelper.ThrowYoureDoingItWrong();
				return false;
			}
		}

		/// <summary>
		/// Disposes this pool and all contained objects (if they are disposable).
		/// </summary>
		/// <remarks>
		/// <para>A pool can only be disposed once, calling this method multiple times will have no effect after the first invocation.</para>
		/// </remarks>
		/// <seealso cref="IsDisposed"/>
		/// <seealso cref="Dispose(bool)"/>
		public void Dispose()
		{
			try
			{
				Dispose(true);
			}
			finally
			{
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Performs dispose logic, can be overridden by derivded types.
		/// </summary>
		/// <param name="disposing">True if the pool is being explicitly disposed, false if it is being disposed from a finalizer.</param>
		/// <seealso cref="Dispose()"/>
		/// <seealso cref="IsDisposed"/>
		protected virtual void Dispose(bool disposing)
		{
			ExceptionHelper.ThrowYoureDoingItWrong();
		}

		#endregion

	}
}