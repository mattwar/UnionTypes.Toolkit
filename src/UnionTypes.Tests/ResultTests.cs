using System.Collections.Immutable;
using UnionTypes.Toolkit;

namespace Tests
{
    [TestClass]
    public class ResultTests
    {
        [TestMethod]
        public void Test_AssignableFromValue()
        {
            Result<int, Exception> result = 10;
            Assert.IsTrue(result is Success<int> s && s.Value == 10);
        }

        [TestMethod]
        public void Test_AssignableFromError()
        {
            var whoops = new Exception("Whoops");
            Result<int, Exception> result = whoops;
            Assert.IsTrue(result is Failure<Exception> f && f.Error == whoops);
        }

        [TestMethod]
        public void Test_Default_HasNoValue()
        {
            Result<int, Exception> result = default;
            Assert.IsFalse(result.HasValue);
            Assert.IsTrue(result.Value is null);
        }

        [TestMethod]
        public void Test_TryGetValue_Success()
        {
            Result<int, Exception> result = 10;
            Assert.IsTrue(result.TryGetValue(out Success<int> success) && success.Value == 10);
        }

        [TestMethod]
        public void Test_TryGetValue_Failure()
        {
            var whoops = new Exception("Whoops");
            Result<int, Exception> result = whoops;
            Assert.IsTrue(result.TryGetValue(out Failure<Exception> failure) && failure.Error == whoops);
        }

        [TestMethod]
        public void Test_Ambiguous_Value()
        {
            Result<string, string> result = Result.Success("Success");
            Assert.IsTrue(result is Success<string> s && s.Value == "Success");
        }

        [TestMethod]
        public void Test_Ambiguous_Error()
        {
            Result<string, string> result = Result.Failure("Whoops");
            Assert.IsTrue(result is Failure<string> f && f.Error == "Whoops");
        }
    }
}