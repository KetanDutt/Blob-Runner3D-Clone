using BlobRunner.Core;
using NUnit.Framework;

namespace BlobRunner.Tests
{
    /// <summary>The enum values are serialized in Player.prefab, so they must never change.</summary>
    public class BodyPartStateTests
    {
        [TestCase(BodyPartState.None, 0)]
        [TestCase(BodyPartState.LeftLegLower, 1)]
        [TestCase(BodyPartState.LeftLegUpper, 2)]
        [TestCase(BodyPartState.RightLegLower, 3)]
        [TestCase(BodyPartState.RightLegUpper, 4)]
        [TestCase(BodyPartState.LeftArmUpper, 5)]
        [TestCase(BodyPartState.LeftArmLower, 6)]
        [TestCase(BodyPartState.RightArmUpper, 7)]
        [TestCase(BodyPartState.RightArmLower, 8)]
        [TestCase(BodyPartState.Head, 9)]
        [TestCase(BodyPartState.TorsoUpper, 10)]
        [TestCase(BodyPartState.TorsoLower, 11)]
        public void SerializedValuesAreStable(BodyPartState state, int expected)
        {
            Assert.AreEqual(expected, (int)state);
        }
    }
}
