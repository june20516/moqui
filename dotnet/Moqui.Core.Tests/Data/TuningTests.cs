using System.Collections.Generic;
using System.Numerics;
using Moqui.Core.Data;
using NUnit.Framework;

namespace Moqui.Core.Tests.Data
{
    public class TuningTests
    {
        private const string SampleJson = @"{
  ""formatVersion"": 1,
  ""values"": {
    ""dash.cooldown"": 0.5,
    ""stamina.max"": 100,
    ""camera.defaultView"": ""ThirdPerson"",
    ""human.actionInterval"": { ""min"": 4, ""max"": 9 },
    ""skill.cost.A"": [60, 120, 200],
    ""camera.fp.eyeOffset"": [0, 0.15, 0.1]
  }
}";

        [Test]
        public void Getters_SampleJson_ReturnTypedValues()
        {
            var tuning = TuningLoader.Parse(SampleJson);

            Assert.That(tuning.GetFloat("dash.cooldown"), Is.EqualTo(0.5f));
            Assert.That(tuning.GetInt("stamina.max"), Is.EqualTo(100));
            Assert.That(tuning.GetString("camera.defaultView"), Is.EqualTo("ThirdPerson"));
            Assert.That(tuning.GetRange("human.actionInterval").Min, Is.EqualTo(4f));
            Assert.That(tuning.GetRange("human.actionInterval").Max, Is.EqualTo(9f));
            Assert.That(tuning.GetFloats("skill.cost.A"), Is.EqualTo(new[] { 60f, 120f, 200f }));
            Assert.That(tuning.GetVector3("camera.fp.eyeOffset"), Is.EqualTo(new Vector3(0f, 0.15f, 0.1f)));
        }

        [Test]
        public void GetFloat_MissingKey_ThrowsWithKeyName()
        {
            var tuning = TuningLoader.Parse(SampleJson);

            var exception = Assert.Throws<KeyNotFoundException>(() => tuning.GetFloat("dash.missing"));

            Assert.That(exception.Message, Does.Contain("dash.missing"));
        }

        [Test]
        public void GetFloat_StringValue_ThrowsDataFormatException()
        {
            var tuning = TuningLoader.Parse(SampleJson);

            Assert.Throws<DataFormatException>(() => tuning.GetFloat("camera.defaultView"));
        }

        [Test]
        public void GetInt_FractionalValue_ThrowsDataFormatException()
        {
            var tuning = TuningLoader.Parse(SampleJson);

            Assert.Throws<DataFormatException>(() => tuning.GetInt("dash.cooldown"));
        }

        [Test]
        public void Parse_UnsupportedFormatVersion_ThrowsDataFormatException()
        {
            Assert.Throws<DataFormatException>(() => TuningLoader.Parse("{\"formatVersion\": 2, \"values\": {}}"));
        }

        [Test]
        public void Parse_MissingValues_ThrowsDataFormatException()
        {
            Assert.Throws<DataFormatException>(() => TuningLoader.Parse("{\"formatVersion\": 1}"));
        }
    }
}
