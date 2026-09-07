using UnityEngine;

[CreateAssetMenu(
    fileName = "UnitStressSettings",
    menuName = "Project Jeokru/Units/Stress Settings")]
public sealed class UnitStressSettings :
    ScriptableObject
{
    [Header("Low Health Stress")]
    [SerializeField]
    [Min(0f)]
    private float lowHealthThreshold = 30f;

    [SerializeField]
    [Min(0.1f)]
    private float lowHealthStressInterval = 10f;

    [Header("Work Stress")]
    [SerializeField]
    [Min(0.1f)]
    private float workStressInterval = 10f;

    public float LowHealthThreshold =>
        lowHealthThreshold;

    public float LowHealthStressInterval =>
        lowHealthStressInterval;

    public float WorkStressInterval =>
        workStressInterval;
}