using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyActionSpriteSet", menuName = "Dead Rain/Roguelite/Enemy Action Sprite Set")]
public class EnemyActionSpriteSet : ScriptableObject
{
    public string id;
    public Sprite idle;
    public Sprite move;
    public Sprite attack;
    public Sprite cast;
    public Sprite charge;
    public Sprite hurt;
}
