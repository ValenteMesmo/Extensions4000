using System.Collections.Generic;

namespace System.Linq
{
    public static class IEnumerableExtensions
    {
        public static bool Empty<T>(this IEnumerable<T> value)
        {
            if (value == null)
                return true;

            return !value.Any();
        }
    }
}
