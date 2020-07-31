using System.Collections.Generic;
using System.Linq;

namespace ImageMosaic.Helpers
{
    public static class Helper
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IReadOnlyCollection<T> collection, int size)
        {
            for (int i = 0; i < collection.Count; i += size)
            {
                yield return collection.Skip(i).Take(size);
            }
        }
    }
}