using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Wokhan.Data.Providers.Tests
{
    [TestClass]
    public class RandomDataProviderTests
    {
        [TestMethod]
        public void AddressBookDataInit()
        {
            new RandomDataProvider.AddressBookData(0);
        }
    }
}
