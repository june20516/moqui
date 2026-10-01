using Moqui.Core.Data;
using NUnit.Framework;

namespace Moqui.Core.Tests.Data
{
    public class JsonReaderTests
    {
        [Test]
        public void Parse_NestedDocument_ReadsAllKinds()
        {
            var value = JsonReader.Parse("{\"a\": [1, -2.5e2, true, false, null], \"b\": {\"c\": \"x\\\"y\\u0041\"}}");

            Assert.That(value.Kind, Is.EqualTo(JsonKind.Object));
            var array = value.Members["a"].Items;
            Assert.That(array[0].NumberValue, Is.EqualTo(1));
            Assert.That(array[1].NumberValue, Is.EqualTo(-250));
            Assert.That(array[2].BoolValue, Is.True);
            Assert.That(array[3].BoolValue, Is.False);
            Assert.That(array[4].Kind, Is.EqualTo(JsonKind.Null));
            Assert.That(value.Members["b"].Members["c"].StringValue, Is.EqualTo("x\"yA"));
        }

        [Test]
        public void Parse_DecimalNumber_IsCultureInvariant()
        {
            Assert.That(JsonReader.Parse("0.15").NumberValue, Is.EqualTo(0.15));
        }

        [TestCase("{\"a\": 1,}")]
        [TestCase("{\"a\": 1} x")]
        [TestCase("[1, 2")]
        [TestCase("01")]
        [TestCase("{\"a\": 1, \"a\": 2}")]
        [TestCase("\"unterminated")]
        public void Parse_InvalidJson_ThrowsDataFormatException(string json)
        {
            Assert.Throws<DataFormatException>(() => JsonReader.Parse(json));
        }

        [Test]
        public void Parse_InvalidJson_ReportsLineAndColumn()
        {
            var exception = Assert.Throws<DataFormatException>(() => JsonReader.Parse("{\n  \"a\": x\n}"));

            Assert.That(exception.Message, Does.Contain("line 2"));
        }

        [Test]
        public void StructurallyEquals_SameContentDifferentOrder_IsTrue()
        {
            var a = JsonReader.Parse("{\"x\": [1, 2], \"y\": {\"min\": 1, \"max\": 2}}");
            var b = JsonReader.Parse("{\"y\": {\"max\": 2, \"min\": 1}, \"x\": [1, 2]}");

            Assert.That(a.StructurallyEquals(b), Is.True);
        }
    }
}
