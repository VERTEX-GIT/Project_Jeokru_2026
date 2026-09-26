using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitAnimationSet", menuName = "Project Jeokru/Unit Animation Set")]
public sealed class UnitAnimationSet : ScriptableObject
{
    [Tooltip("별도 Idle 시트가 있으면 지정합니다. 비어 있으면 walk 0번을 Idle로 사용합니다.")]
    public Sprite idle;

    [Tooltip("별도 Idle이 없으면 0번이 정면 Idle, 1번부터 이동 프레임입니다.")]
    public Sprite[] walk;
    public Sprite[] attack;
    public Sprite[] down;

    [Min(1f)] public float framesPerSecond = 8f;
}
