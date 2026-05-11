using UnityEngine;

[CreateAssetMenu(fileName = "NewProceduralMapSettings", menuName = "Dead Rain/Roguelite/Procedural Map Settings")]
public class ProceduralMapSettings : ScriptableObject
{
    [Header("Graph")]
    public int seed;
    public bool randomizeSeed = true;
    public int mainPathLength = 8;
    public int minBranchCount = 2;
    public int maxBranchCount = 4;
    public int maxBranchLength = 3;

    [Header("Required Rooms")]
    public bool requireTreasureRoom = true;
    public bool requireChallengeRoom = true;
    public bool requireKeyAndLockedRoom = true;
    public bool requireBossAnteRoom = true;
    public bool allowSecretRooms = true;

    [Header("Safety")]
    public float roomSpacingX = 34f;
    public float roomSpacingY = 20f;
    public float maxJumpGap = 4.5f;
    public float maxVerticalStep = 3.2f;
    public bool runConnectivityCheck = true;
    public bool drawDebugGraph = true;
}
