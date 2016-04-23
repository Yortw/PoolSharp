using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace PoolSharp
{
	/// <summary>
	/// A simple non-blocking object pool.
	/// </summary>
	/// <remarks>
	/// <para>This pool does not block when a new item is requested and the pool is empty, instead a new will be allocated and returned.</para>
	/// <para>By default the pool starts empty and items are allocated as needed. The <see cref="Expand()"/> method can be used to pre-load the pool if required./para>
	/// <para>Objects returned to the pool are added on a first come first serve basis. If the pool is full when an object is returned, it is ignored (and will be garbage collected if there are no other references to it). In this case, if the item implements <see cref="IDisposable"/> the pool will ensure the item is disposed before being 'ignored'.</para>
	/// <para>The pool makes a best effort attempt to avoid going over the specified <see cref="PoolPolicy{T}.MaximumPoolSize"/>, but does not strictly enforce it. Under certain multi-threaded scenarios it's possible for a few items more than the maximum to be kept in the pool.</para>
	/// <para>Disposing the pool will also dispose all objects currently in the pool, if they support <see cref="IDisposable"/>.</para>
	/// </remarks>
	/// <typeparam name="T">The type of value being pooled.</typeparam>
	/// <seealso cref="PoolPolicy{T}"/>
	/// <seealso cref="IPool{T}"/>
	/// <seealso cref="PooledObject{T}"/>
	public class Pool<T> : IPool<T>
	{

		#region Fields

		private readonly System.Collections.Concurrent.ConcurrentBag<T> _Pool;
		private readonly PoolPolicy<T> _PoolPolicy;
		private readonly System.Collections.Concurrent.BlockingCollection<T> _ItemsToInitialise;
		private readonly bool _IsPooledTypeDisposable;
		private readonly bool _IsPooledTypeWrapped;
		private bool _IsDisposed;

		private PropertyInfo _PooledObjectValueProperty;

#if SUPPORTS_THREADS
				System.Threading.Thread _ReinitialiseThread; 
#endif

		#endregion

		#region Constructors

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="poolPolicy">A <seealso cref="PoolPolicy{T}"/> instance containing configuration information for the pool.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="poolPolicy"/> argument is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the <see cref="PoolPolicy{T}.Factory"/> property of the <paramref name="poolPolicy"/> argument is null.</exception>
		public Pool(PoolPolicy<T> poolPolicy)
		{
			if (poolPolicy == null) throw new ArgumentNullException(nameof(poolPolicy));
			if (poolPolicy.Factory == null) throw new ArgumentException("poolPolicy.Factory cannot be null");

			_IsPooledTypeWrapped = IsTypeWrapped(typeof(T));
			_IsPooledTypeDisposable = IsTypeDisposable(typeof(T));

			_PoolPolicy = poolPolicy;
			_Pool = new System.Collections.Concurrent.ConcurrentBag<T>();

			if (_PoolPolicy.InitializationPolicy == PooledItemInitialization.AsyncReturn)
			{
				_ItemsToInitialise = new System.Collections.Concurrent.BlockingCollection<T>();
#if SUPPORTS_THREADS
				_ReinitialiseThread = new System.Threading.Thread(this.BackgroundReinitialise);
				_ReinitialiseThread.Name = this.GetType().FullName + " Background Reinitialise";
				_ReinitialiseThread.IsBackground = true;
				_ReinitialiseThread.Start();
#else
				System.Threading.Tasks.Task.Factory.StartNew(this.BackgroundReinitialise, System.Threading.Tasks.TaskCreationOptions.LongRunning);
#endif
			}
		}

		#endregion

		#region IPool<T> Members

		/// <summary>
		/// Gets an item from the pool.
		/// </summary>
		/// <remarks>
		/// <para>If the pool is empty when the request is made, a new item is instantiated and returned. Otherwise an instance from the pool will be used.</para>
		/// <para>This method is thread safe.</para>
		/// </remarks>
		/// <returns>Returns an instance of {T} from the pool, or a new instance if the pool is empty.</returns>
		/// <exception cref="ObjectDisposedException">Thrown if the pool has been disposed.</exception>
		public T Take()
		{
			CheckDisposed();

			T retVal;

			if (_Pool.TryTake(out retVal))
			{
				if (_PoolPolicy.InitializationPolicy == PooledItemInitialization.Take && _PoolPolicy.ReinitializeObject != null)
					_PoolPolicy.ReinitializeObject(retVal);
			}
			else
				retVal = _PoolPolicy.Factory(this);

			return retVal;
		}

		/// <summary>
		/// Returns/adds an object to the pool so it can be reused.
		/// </summary>
		/// <param name="value"></param>
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
			if (value == null) throw new ArgumentNullException(nameof(value));

			if (IsDisposed)
			{
				SafeDispose(value);
				return;
			}

			if (ShouldReturnToPool(value))
			{
				if (_PoolPolicy.InitializationPolicy == PooledItemInitialization.Take || _PoolPolicy.ReinitializeObject == null)
					_Pool.Add(value);
				else if (_PoolPolicy.InitializationPolicy == PooledItemInitialization.AsyncReturn)
					SafeAddToReinitialiseQueue(value);
				else if (_PoolPolicy.InitializationPolicy == PooledItemInitialization.Return)
				{
					_PoolPolicy.ReinitializeObject(value);
					_Pool.Add(value);
				}
			}
			else
				SafeDispose(value);
		}

		/// <summary>
		/// Creates new items and adds them to the pool up to it's maximum capacity.
		/// </summary>
		/// <remarks>
		/// <para>This method is 'thread safe', though it is possible under certain race conditons for the pool to go beyond it's configured maximum size by a few items.</para>
		/// <para>If the maximum pool size is set to zero or less (meaning no limit) then the method returns without doing anything, no instances are added to the pool.</para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">Thrown if this method is called on a disposed pool.</exception>
		public void Expand()
		{
			Expand(_PoolPolicy.MaximumPoolSize);
		}

		/// <summary>
		/// Creates as many new items as specified by <paramref name="increment"/> and adds them to the pool, but not over it's maximum capacity.
		/// </summary>
		/// <param name="increment">The maximum number of items to pre-allocate and add to the pool.</param>
		/// <remarks>
		/// <para>This method is 'thread safe', though it is possible under certain race conditons for the pool to go beyond it's configured maximum size by a few items.</para>
		/// <para>If <paramref name="incremenet"/> is zero or less the method returns without doing anything</para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">Thrown if this method is called on a disposed pool.</exception>
		public void Expand(int increment)
		{
			CheckDisposed();

			if (increment <= 0) return;

			int createdCount = 0;
			while (createdCount < increment && (_Pool.Count < _PoolPolicy.MaximumPoolSize || _PoolPolicy.MaximumPoolSize <= 0))
			{
				_Pool.Add(_PoolPolicy.Factory(this));
				createdCount++;
			}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Throws a <see cref="ObjectDisposedException"/> if the <see cref="Dispose"/> method has been called.
		/// </summary>
		protected void CheckDisposed()
		{
			if (_IsDisposed) throw new ObjectDisposedException(this.GetType().FullName);
		}

		#endregion

		#region IDisposable & Related Implementation 

		/// <summary>
		/// Returns a boolean indicating if this pool is disposed or not.
		/// </summary>
		/// <seealso cref="Dispose"/>
		/// <seealso cref="Dispose(bool)"/>
		public bool IsDisposed
		{
			get { return _IsDisposed; }
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
			if (_IsDisposed) return;

			try
			{
				_IsDisposed = true;

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
		/// <seealso cref="Dispose"/>
		/// <seealso cref="IsDisposed"/>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_ItemsToInitialise?.CompleteAdding();

				if (_IsPooledTypeDisposable)
				{
					T item;

					while (!_Pool.IsEmpty)
					{
						_Pool.TryTake(out item);
						SafeDispose(item);
					}
				}
			}
		}

		#endregion

		#region Private Methods

		private void BackgroundReinitialise()
		{
			T item = default(T);
			while (!_ItemsToInitialise.IsCompleted)
			{
				try
				{
					item = _ItemsToInitialise.Take();
				}
				catch (InvalidOperationException)
				{
					if (_ItemsToInitialise.IsCompleted) return;
				}

				if (item != null)
				{
					if (_IsDisposed)
						SafeDispose(item);
					else
					{
						if (_PoolPolicy.ReinitializeObject != null)
							_PoolPolicy.ReinitializeObject(item);

						if (_PoolPolicy.MaximumPoolSize <= 0 || _Pool.Count < _PoolPolicy.MaximumPoolSize)
							_Pool.Add(item);
					}
				}
			}
		}

		private void SafeAddToReinitialiseQueue(T pooledObject)
		{
			try
			{
				if (!_ItemsToInitialise.IsAddingCompleted)
					_ItemsToInitialise.Add(pooledObject);
			}
			catch (InvalidOperationException) { } //Handle race condition on above if condition.
		}

		private void ProcessReturnedItems()
		{
			//Only bother reinitialising items while we're alive.
			//If we're shutdown, even with items left to process, then just ignore them.
			//We're not going to use them anyway.
			while (!_ItemsToInitialise.IsAddingCompleted)
			{
				ReinitialiseAndReturnToPoolOrDispose(_ItemsToInitialise.Take());
			}

			//If we're done but the there are disposable items in the queue,
			//dispose each one.
			if (!_ItemsToInitialise.IsCompleted && _IsPooledTypeDisposable)
			{
				while (!_ItemsToInitialise.IsCompleted)
				{
					SafeDispose(_ItemsToInitialise.Take());
				}
			}
		}

		private void ReinitialiseAndReturnToPoolOrDispose(T value)
		{
			_PoolPolicy.ReinitializeObject(value);
			if (ShouldReturnToPool(value))
				_Pool.Add(value);
			else
				SafeDispose(value);
		}


#if NETFX_CORE
		private void SafeDispose(object pooledObject)
		{
			if (_IsPooledTypeWrapped)
			{
				if (_PooledObjectValueProperty == null)
					_PooledObjectValueProperty = typeof(T).GetTypeInfo().DeclaredProperties.Where((p) => p.Name == "Value").First();

				pooledObject = _PooledObjectValueProperty.GetValue(pooledObject, null);
			}

			(pooledObject as IDisposable)?.Dispose();
		}
#else
		private void SafeDispose(object pooledObject)
		{
			if (_IsPooledTypeWrapped)
			{				
				if (_PooledObjectValueProperty == null)
					_PooledObjectValueProperty = typeof(T).GetProperty("Value"); 
				
				pooledObject = _PooledObjectValueProperty.GetValue(pooledObject, null);
			}

			(pooledObject as IDisposable)?.Dispose();
		}
#endif

		private bool ShouldReturnToPool(T pooledObject)
		{
			return (_PoolPolicy.MaximumPoolSize == 0 || _Pool.Count < _PoolPolicy.MaximumPoolSize) && !_Pool.Contains(pooledObject);
		}

		private static bool IsTypeWrapped(Type type)
		{
#if NETFX_CORE
			return type.GetTypeInfo().IsGenericType && (type.GetGenericTypeDefinition() == typeof(PooledObject<>));
#else
			return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(PooledObject<>));
#endif
		}

#if NETFX_CORE
		private bool IsTypeDisposable(Type type)
		{
			if (_IsPooledTypeWrapped)
				type = type.GetTypeInfo().GenericTypeArguments.First();

			return (from i in type.GetTypeInfo().ImplementedInterfaces where i == typeof(IDisposable) select i).Any();
		}
#else
		private bool IsTypeDisposable(Type type)
		{
			if (_IsPooledTypeWrapped)
				type = type.GetGenericArguments().First();

			return (from i in type.GetInterfaces() where i == typeof(IDisposable) select i).Any();
		}
#endif

		#endregion
	}
}