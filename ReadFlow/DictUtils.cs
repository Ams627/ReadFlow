namespace ReadFlow
{
    using System.Collections.Generic;

    internal static class DictUtils
    {
        public static void AddEntryToList<T, U>(this Dictionary<T, List<U>> d, T key, U listEntry)
        {
            if (!d.TryGetValue(key, out var list))
            {
                list = new List<U>();
                d.Add(key, list);
            }
            list.Add(listEntry);
        }

    }
}