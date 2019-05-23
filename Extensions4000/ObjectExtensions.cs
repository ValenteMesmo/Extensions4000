using System.Collections.Generic;

namespace System.Linq
{
    public static class ObjectExtensions
    {
        public static IEnumerable<T> Yield<T>(this T value)
        {
            if (value != null)
                yield return value;
        }
    }
}