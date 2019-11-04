using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Wokhan.Data.Providers.Tests
{
    [TestClass]
    public class RandomDataProviderTests
    {
        [TestMethod]
        public void AddressBookDataInit()
        {
            var randy = new RandomDataProvider();
            var data = randy.GetQueryable<RandomDataProvider.AddressBookData>(RandomDataProvider.ADDRESS_BOOK);
            
            Assert.AreEqual(data.Count(), randy.ItemsCount);
        }
    }
}
