using System.Linq;
using Xunit;

namespace Wokhan.Data.Providers.Tests
{
    public class RandomDataProviderTests
    {
        [Fact]
        public void AddressBookDataInit()
        {
            var randy = new RandomDataProvider();
            randy.ItemsCount = 100;
            randy.MinDelay = 0;
            randy.MaxDelay = 100;
            
            var data = randy.GetQueryable<RandomDataProvider.AddressBookData>(RandomDataProvider.ADDRESS_BOOK);
            
            var count = data.Count();
            Assert.Equal(randy.ItemsCount, count);
            Assert.Equal(randy.ItemsCount, data.ToList().Count());

            var orderedCount = data.OrderBy(x => x.Age).Count();
        }
    }
}
