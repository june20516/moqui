using Moqui.Core.Tests.Support;
using NUnit.Framework;

namespace Moqui.Core.Tests.Data
{
    /// <summary>M14에 추가한 설정 값들의 관계 (리뷰): 수치를 바꿔도 규칙이 뒤틀리지 않게 지킨다.</summary>
    public class TuningRelationTests
    {
        [Test]
        public void M14Settings_KeepTheirRelations()
        {
            var settings = TestSimulations.Settings;

            Assert.That(settings.Attach.SnapRange, Is.GreaterThanOrEqualTo(settings.Attach.AttachRange), "F reaches at least the touch range");
            Assert.That(settings.Attach.AutoLandAlign, Is.InRange(-1f, 1f), "a cosine");
            Assert.That(settings.Attach.AutoLandDelay, Is.GreaterThan(0f));
            Assert.That(settings.Awareness.CauseMemory, Is.GreaterThan(0f), "zero memory would erase every cause at once");
            Assert.That(settings.Head.TurnAccelTime, Is.GreaterThanOrEqualTo(0f));
            Assert.That(settings.Walk.AccelTime, Is.GreaterThanOrEqualTo(0f));
            Assert.That(settings.Suck.YankItch, Is.GreaterThanOrEqualTo(0f));
        }
    }
}
