using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

// TODO : Move in Wokhan.Core
namespace Wokhan.Data.Providers
{
    /// <summary>
    /// A class to wrap an IEnumerable&lt;<typeparamref name="T"/>&gt; and add some delay when enumerating.
    /// Useful for testing purpose, but should not be that useful in real-world scenarios!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DelayedEnumerableQuery<T> : EnumerableQuery<T>, IEnumerable, IEnumerable<T>
    {
        private int minDelay;
        private int maxDelay;
        private IEnumerable<T> enumerable;

        public DelayedEnumerableQuery(IEnumerable<T> enumerable, int minDelay, int maxDelay) : base(enumerable)
        {
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
            this.enumerable = enumerable;
        }

        //public DelayedEnumerableQuery(Expression expression, int minDelay, int maxDelay) : base(expression)
        //{
        //    this.minDelay = minDelay;
        //    this.maxDelay = maxDelay;
        //}


        IEnumerator IEnumerable.GetEnumerator()
        {
            if (minDelay >= 0 && maxDelay > 0 && maxDelay > minDelay)
            {
                return GetEnumeratorInternal();
            }
            return enumerable.GetEnumerator();
        }

        //TODO: remove duplicated code
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (minDelay >= 0 && maxDelay > 0 && maxDelay > minDelay)
            {
                return GetEnumeratorInternal();
            }
            return enumerable.GetEnumerator();
        }

        IEnumerator<T> GetEnumeratorInternal()
        {
            var rnd = new Random();
            foreach (var item in enumerable)
            {
                Thread.Sleep(rnd.Next(minDelay, maxDelay));
                yield return item;
            }
        }
    }
}
