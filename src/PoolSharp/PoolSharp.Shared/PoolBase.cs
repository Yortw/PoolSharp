using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

namespace PoolSharp
{
    /// <summary>
    /// Base class providing code re-use among multiple pool implementations. Should not be used directly by calling code, instead use <see cref="IPool{T}"/> for references.
    /// </summary>
    /// <typeparam name="T">The type of value being pooled.</typeparam>
    public abstract class PoolBase<T> : IPool<T>
    {

        #region Fields

        private readonly PoolPolicy<T> _PoolPolicy;
        private readonly bool _IsPooledTypeDisposable;
        private readonly bool _IsPooledTypeWrapped;
        private PropertyInfo _PooledObjectValueProperty;

        private bool _IsDisposed;

        #endregion

        #region Constructors

        /// <summary>
        /// Full constructor.
        /// </summary>
        /// <param name="poolPolicy">A <seealso cref="PoolPolicy{T}"/> instance containing configuration information for the pool.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="poolPolicy"/> argument is null.</exception>
        /// <exception cref="System.ArgumentException">Thrown if the <see cref="PoolPolicy{T}.Factory"/> property of the <paramref name="poolPolicy"/> argument is null.</exception>
        protected PoolBase(PoolPolicy<T> poolPolicy)
        {
            if (poolPolicy == null) throw new ArgumentNullException(nameof(poolPolicy));
            if (poolPolicy.Factory == null) throw new ArgumentException("poolPolicy.Factory cannot be null");

            _IsPooledTypeWrapped = ReflectionUtils.IsTypeWrapped(typeof(T));
            _IsPooledTypeDisposable = ReflectionUtils.IsTypeDisposable(typeof(T), _IsPooledTypeWrapped);

            _PoolPolicy = poolPolicy;
        }

        #endregion

        #region IDisposable & Related Implementation 

        /// <summary>
        /// Disposes this pool and all contained objects (if they are disposable).
        /// </summary>
        /// <remarks>
        /// <para>A pool can only be disposed once, calling this method multiple times will have no effect after the first invocation.</para>
        /// </remarks>
        /// <seealso cref="IsDisposed"/>
        /// <seealso cref="Dispose(bool)"/>
        public bool IsDisposed
        {
            get
            {
                return _IsDisposed;
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
        /// <seealso cref="Dispose()"/>
        /// <seealso cref="IsDisposed"/>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Throws a <see cref="ObjectDisposedException"/> if the <see cref="Dispose()"/> method has been called.
        /// </summary>
        protected void CheckDisposed()
        {
            if (_IsDisposed) throw new ObjectDisposedException(this.GetType().FullName);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Provides access to the <see cref="PoolPolicy"/> passed in the constructor.
        /// </summary>
        /// <seealso cref="PoolBase{T}"/>
        protected PoolPolicy<T> PoolPolicy
        {
            get
            {
                return _PoolPolicy;
            }
        }

        /// <summary>
        /// Returns a boolean indicating if {T} is disposale.
        /// </summary>
        protected bool IsPooledTypeDisposable
        {
            get
            {
                return _IsPooledTypeDisposable;
            }
        }

        /// <summary>
        /// Returns a boolean indicating if {T} is actually a <seealso cref="PooledObject{T}"/>.
        /// </summary>
        protected bool IsPooledTypeWrapped
        {
            get
            {
                return _IsPooledTypeWrapped;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Disposes <paramref name="pooledObject"/> if it is not null and supports <see cref="IDisposable"/>, otherwise does nothing. If <paramref name="pooledObject"/> is actually a <see cref="PooledObject{T}"/> instance, then disposes the <see cref="PooledObject{T}.Value"/> property instead.
        /// </summary>
        /// <param name="pooledObject"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "object")]
        protected void SafeDispose(object pooledObject)
        {
            if (IsPooledTypeWrapped)
            {
                if (_PooledObjectValueProperty == null)
                    _PooledObjectValueProperty = typeof(T).GetTypeInfo().DeclaredProperties.Where((p) => p.Name == "Value").First();

                pooledObject = _PooledObjectValueProperty.GetValue(pooledObject, null);
            }

            (pooledObject as IDisposable)?.Dispose();
        }


        #endregion

        #region Abstract IPool<T> Members

        /// <summary>
        /// Abstract method for adding or returning an instance to the pool.
        /// </summary>
        /// <param name="value">The instance to add or return to the pool.</param>
        public abstract void Add(T value);

        /// <summary>
        /// Abstract method for filling the pool up to it's maximum size with pre-pooled instances.
        /// </summary>
        public abstract void Expand();

        /// <summary>
        /// Abstract method for adding a specified number of pre-pooled instances.
        /// </summary>
        /// <param name="increment">The number of instances to add to the pool.</param>
        public abstract void Expand(int increment);

        /// <summary>
        /// Abstract method for retrieving an item from the pool. 
        /// </summary>
        /// <returns>An instance of {T}.</returns>
        public abstract T Take();

        #endregion

    }
}