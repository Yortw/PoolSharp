using System;
using System.Collections.Generic;
using System.Text;

namespace PoolSharp
{
	/// <summary>
	/// Event arguments for the <see cref="PoolBase{T}.ReinitialiseError"/> event.
	/// </summary>
	/// <typeparam name="T">The type of value stored in the pool and being reinitialised.</typeparam>
	public class ReinitialiseErrorEventArgs<T> : EventArgs
	{
		private readonly Exception _Exception;
		private readonly T _Item;

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="exception">The exception that was thrown from the reinitialisation callback.</param>
		/// <param name="item">The item that was being reinitialised when the exception were thrown.</param>
		public ReinitialiseErrorEventArgs(Exception exception, T item)
		{
			_Exception = exception;
			_Item = item;
		}

		/// <summary>
		/// The exception that was thrown from the reinitialisation callback.
		/// </summary>
		public Exception Exception => _Exception;

		/// <summary>
		/// The item that was being reinitialised when the exception were thrown.
		/// </summary>
		public T Item => _Item;
	}
}
