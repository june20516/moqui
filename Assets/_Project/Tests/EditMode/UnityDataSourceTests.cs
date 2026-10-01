using Moqui.Core.Data;
using Moqui.Unity.Data;
using NUnit.Framework;

namespace Moqui.Unity.Tests
{
    public class UnityDataSourceTests
    {
        [Test]
        public void Load_InEditor_ReadsRepoTuning()
        {
            var source = new UnityDataSource();

            Tuning tuning = TuningLoader.Load(source);

            Assert.That(source.Root, Is.EqualTo(UnityDataSource.RepoDataRoot));
            Assert.That(tuning.Contains("flight.speed"), Is.True);
        }
    }
}
