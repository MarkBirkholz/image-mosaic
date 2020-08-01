using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImageMosaic.Helpers
{
    public static class CollectionHelper
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IReadOnlyCollection<T> collection, int size)
        {
            for (int i = 0; i < collection.Count; i += size)
            {
                yield return collection.Skip(i).Take(size);
            }
        }
        
        public static IEnumerable<T> WithCancellation<T>(this IEnumerable<T> collection, CancellationToken token)
        {
            foreach (var item in collection)
            {
                if (token.IsCancellationRequested)
                {
                    break;
                }
                yield return item;
            }
        }
    }
}