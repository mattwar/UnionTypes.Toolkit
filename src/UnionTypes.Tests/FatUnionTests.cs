using System.Collections.Immutable;
using UnionTypes.Toolkit;

namespace Tests
{
    [TestClass]
    public class FatUnionTests
    {
        [TestMethod]
        public void Test_Union2()
        {
            TestUnion2<int, string>(1, 1);
            TestUnion2<int, string>("one", "one");
            TestUnion2<int, string>(default, null);
        }

        [TestMethod]
        public void Test_Union3()
        {
            TestUnion3<int, string, double>(1, 1);
            TestUnion3<int, string, double>("one", "one");
            TestUnion3<int, string, double>(3.0, 3.0);
        }

        private void TestUnion2<T1, T2>(
            FatUnion<T1, T2> union, object? expectedValue)
        {
            // check TryGetValue API's
            Assert.IsTrue(
                union.HasValue == false
                || union.TryGetValue(out T1? value1) && value1.Equals(expectedValue)
                || union.TryGetValue(out T2? value2) && value2.Equals(expectedValue)
                );

            // check basic Value property
            TestUnion(union, expectedValue); 
        }

        private void TestUnion3<T1, T2, T3>(
            FatUnion<T1, T2, T3> union, object? expectedValue)
        {
            // check TryGetValue API's
            Assert.IsTrue(
                union.HasValue == false
                || union.TryGetValue(out T1? value1) && value1.Equals(expectedValue)
                || union.TryGetValue(out T2? value2) && value2.Equals(expectedValue)
                || union.TryGetValue(out T3? value3) && value3.Equals(expectedValue)
                );

            // check basic Value property
            TestUnion(union, expectedValue);
        }

        private void TestUnion<TUnion>(TUnion union, object? expectedValue)
            where TUnion : System.Runtime.CompilerServices.IUnion
        {
            Assert.AreEqual(union.Value, expectedValue);
        }
    }
}