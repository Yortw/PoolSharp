using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;

namespace PoolSharp
{
	internal static class ReflectionUtils
	{
		internal static bool IsTypeWrapped(Type type)
		{
#if NETFX_CORE
			return type.GetTypeInfo().IsGenericType && (type.GetGenericTypeDefinition() == typeof(PooledObject<>));
#else
			return type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(PooledObject<>));
#endif
		}

#if NETFX_CORE
		internal static bool IsTypeDisposable(Type type, bool isTypeWrapped)
		{
			if (isTypeWrapped)
				type = type.GetTypeInfo().GenericTypeArguments.First();

			return (from i in type.GetTypeInfo().ImplementedInterfaces where i == typeof(IDisposable) select i).Any();
		}
#else
		internal static bool IsTypeDisposable(Type type, bool isTypeWrapped)
		{
			if (isTypeWrapped)
				type = type.GetGenericArguments().First();

			return (from i in type.GetInterfaces() where i == typeof(IDisposable) select i).Any();
		}
#endif

		internal static bool Contains<T>(this T[] items, T item)
		{
			if (items == null) return false;

			for (int cnt = 0; cnt < items.Length; cnt++)
			{
				if (EqualityComparer<T>.Default.Equals(items[cnt], item))
					return true;
			}

			return false;
		}
	}
}