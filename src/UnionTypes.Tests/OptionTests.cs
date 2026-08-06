using System.Collections.Immutable;
using UnionTypes.Toolkit;

namespace Tests
{
    [TestClass]
    public class OptionTests
    {
        [TestMethod]
        public void Test_AssignedDefault()
        {
            Option<int> zed = default;
            Assert.IsTrue(zed is None);
        }

        [TestMethod]
        public void Test_AssignedNone()
        {
            Option<int> zed = new None();
            Assert.IsTrue(zed is None);
        }

        [TestMethod]
        public void Test_NoneFactory()
        {
            Option<int> zed = Option.None;
            Assert.IsTrue(zed is None);
        }

        [TestMethod]
        public void Test_AssignedValue()
        {
            Option<int> zed = 10;
            Assert.IsTrue(zed is Some<int> some && some.Value == 10);
        }

        [TestMethod]
        public void Test_SomeFactory()
        {
            var zed = Option.Some(10);
            Assert.IsTrue(zed is Some<int> some && some.Value == 10);
        }

        [TestMethod]
        public void Test_TryGetValue_Some()
        {
            Option<int> zed = 10;
            Assert.IsTrue(zed.TryGetValue(out Some<int> some) && some.Value == 10);
        }

        [TestMethod]
        public void Test_TryGetValue_None()
        {
            Option<int> zed = Option.None;
            Assert.IsTrue(zed.TryGetValue(out None _));
        }
   }
}