using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

// TODO : Move in Wokhan.Core
namespace Wokhan.Data.Providers
{
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

        public DelayedEnumerableQuery(Expression expression, int minDelay, int maxDelay) : base(expression)
        {
            this.minDelay = minDelay;
            this.maxDelay = maxDelay;
        }


        IEnumerator IEnumerable.GetEnumerator() => GetEnumeratorInternal();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumeratorInternal();

        IEnumerator<T> GetEnumeratorInternal()
        {
            if (minDelay >= 0 && maxDelay > 0 && maxDelay > minDelay)
            {
                var rnd = new Random();
                return enumerable.Select(_ => { Thread.Sleep(rnd.Next(minDelay, maxDelay)); return _; }).GetEnumerator();
            }
            return enumerable.GetEnumerator();
        }
    }
}
