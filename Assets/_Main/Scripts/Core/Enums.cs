namespace BlobRunner.Core
{
    /// <summary>Locomotion style, derived from which leg parts are still attached.</summary>
    public enum PlayerState
    {
        OnStandRun,
        OnLeftRun,
        OnRightRun,
        OnCrawlRun
    }

    /// <summary>
    /// Identifies a body part. The numeric values are serialized in <c>Player.prefab</c>
    /// (<c>BodyPart.bodyState</c>) and therefore MUST NOT be reordered.
    /// </summary>
    public enum BodyPartState
    {
        None = 0,
        LeftLegLower = 1,
        LeftLegUpper = 2,
        RightLegLower = 3,
        RightLegUpper = 4,
        LeftArmUpper = 5,
        LeftArmLower = 6,
        RightArmUpper = 7,
        RightArmLower = 8,
        Head = 9,
        TorsoUpper = 10,
        TorsoLower = 11
    }
}
