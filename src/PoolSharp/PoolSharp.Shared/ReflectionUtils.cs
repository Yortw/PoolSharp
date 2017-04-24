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
            return type.GetTypeInfo().IsGenericType && (type.GetGenericTypeDefinition() == typeof(PooledObject<>));

        }

        internal static bool IsTypeDisposable(Type type, bool isTypeWrapped)
        {
            if (isTypeWrapped)
                type = type.GetTypeInfo().GenericTypeArguments.First();

            return (from i in type.GetTypeInfo().ImplementedInterfaces where i == typeof(IDisposable) select i).Any();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
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