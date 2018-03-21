using NUnit.Framework;

namespace TestLibrary.Test
{
    [TestFixture]
    public class TestLibraryTests
    {
        [Test]
        public void Two_plus_two_should_be_four()
        {
            var subject = new TestLibrary();
            Assert.That(subject.Add(2, 2), Is.EqualTo(4));
        }
    }
}
