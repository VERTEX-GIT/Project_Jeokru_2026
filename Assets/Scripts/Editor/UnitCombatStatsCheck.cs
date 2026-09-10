using System;
using UnityEditor;
using UnityEngine;

public static class UnitCombatStatsCheck
{
    [MenuItem("Tools/Project Jeokru/Check Unit Combat Stats (Play Mode)")]
    public static void Run()
    {
        Require(Application.isPlaying, "Run in InGame play mode");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Unit/Unit_Ally.prefab");
        GameObject first = null, second = null;
        try
        {
            first = UnityEngine.Object.Instantiate(prefab);
            second = UnityEngine.Object.Instantiate(prefab);
            var a = first.GetComponent<UnitCore>();
            var b = second.GetComponent<UnitCore>();
            var data = a.Data;
            var original = new Vector3(data.AttackPower, data.Defense, data.AttackCooldown);
            Require(Stats(a) == original && Stats(b) == original, "Awake initialization");
            a.SetCombatStats(37f, 7f, 0.4f);
            Require(Stats(a) == new Vector3(37f, 7f, 0.4f), "Runtime update");
            Require(Stats(b) == original &&
                new Vector3(data.AttackPower, data.Defense, data.AttackCooldown) == original,
                "Other unit or shared SO changed");
            var health = first.GetComponent<UnitHealth>();
            float before = health.CurrentHp;
            health.TakeDamage(8f, null);
            Require(Mathf.Approximately(health.CurrentHp, before - 1f), "Runtime defense not used");
            a.SetCombatStats(-1f, -1f, -1f);
            Require(Stats(a) == Vector3.zero, "Negative stats");
            a.SetData(data);
            Require(Stats(a) == original, "SetData initialization");
            Debug.Log("UNIT_STATS_CHECK PASS: initialization, isolated stats, shared SO preserved, defense and reset");
        }
        finally
        {
            if (first != null) UnityEngine.Object.DestroyImmediate(first);
            if (second != null) UnityEngine.Object.DestroyImmediate(second);
        }
    }

    private static Vector3 Stats(UnitCore unit) => new(unit.AttackPower, unit.Defense, unit.AttackCooldown);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception("UNIT_STATS_CHECK: " + message);
    }
}
