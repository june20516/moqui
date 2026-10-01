using System.Collections;
using Moqui.Core.Data;
using Moqui.Unity.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moqui.Unity.Tests
{
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator Tuning_LoadedInPlayMode_SurvivesFrame()
        {
            Tuning tuning = TuningLoader.Load(new UnityDataSource());
            var probe = new GameObject("PlayModeProbe");

            yield return null;

            Assert.That(Application.isPlaying, Is.True);
            Assert.That(probe != null, Is.True);
            Assert.That(tuning.GetFloat("flight.speed"), Is.GreaterThan(0f));
            Object.Destroy(probe);
        }
    }
}
