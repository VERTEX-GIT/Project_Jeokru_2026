using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitAnimationSet", menuName = "Project Jeokru/Unit Animation Set")]
public sealed class UnitAnimationSet : ScriptableObject
{
    [Tooltip("0번은 정면 Idle, 1번부터는 오른쪽 이동 프레임")]
    public Sprite[] walk;
    public Sprite[] attack;
    public Sprite[] down;

    [Min(1f)] public float framesPerSecond = 8f;
}
