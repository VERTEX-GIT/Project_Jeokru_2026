using System;
using System.Reflection;

// Exercises the real clock implementation with engine stubs; not a Unity play-mode test.
class Program
{
    static void Main()
    {
        var clock = new GameTimeManager();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Tick(int count) { for (int i = 0; i < count; i++) typeof(GameTimeManager).GetMethod("Update", flags).Invoke(clock, null); }
        void Require(bool value, string message) { if (!value) throw new Exception(message); }
        typeof(GameTimeManager).GetMethod("Awake", flags).Invoke(clock, null);
        Tick(59);
        Require(clock.CurrentTime == 60 && clock.CurrentDay == 1, "Reach day end");
        RaidManager.Instance = new RaidManager { IsRaidActive = true };
        Tick(100);
        Require(clock.CurrentTime == 60 && clock.CurrentDay == 1 && clock.IsRunning, "Hold clock, keep gameplay running");
        RaidManager.Instance.IsRaidActive = false;
        Tick(1);
        Require(clock.CurrentTime == 1 && clock.CurrentDay == 2, "Release next day");
        typeof(GameTimeManager).GetField("startDay", flags).SetValue(clock, 30);
        clock.ResetTime();
        clock.NotifyRaidSucceeded();
        Require(!clock.IsGameOver, "No early victory");
        Tick(59);
        clock.NotifyRaidSucceeded();
        Require(clock.IsVictory && clock.IsGameOver && UnityEngine.Time.timeScale == 0, "Final victory");
        Tick(2);
        Require(clock.CurrentDay == 30 && clock.CurrentTime == 60, "Result locks clock");
        clock.ResetTime();
        clock.CheckFactoryDefeat();
        Require(!clock.IsGameOver, "Empty initial scene");
        var alive = new FactoryHealth { IsAlive = true };
        UnityEngine.Object.Factories = new[] { alive, new FactoryHealth() };
        clock.CheckFactoryDefeat();
        Require(!clock.IsGameOver, "Surviving factory");
        alive.IsAlive = false;
        clock.CheckFactoryDefeat();
        Require(clock.IsGameOver && !clock.IsVictory, "Factory defeat");
        clock.NotifyRaidSucceeded();
        Require(!clock.IsVictory, "Result cannot be overwritten");
        Console.WriteLine("PASS: raid wait, next day, final victory, factory defeat, result lock");
    }
}
namespace UnityEngine
{
    public class Object
    {
        public static FactoryHealth[] Factories = Array.Empty<FactoryHealth>();
        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) => (T[])(object)Factories;
        public static void Destroy(object target) { }
    }
    public class MonoBehaviour : Object { public object gameObject; }
    public enum FindObjectsSortMode { None }
    public class DisallowMultipleComponent : Attribute { }
    public class SerializeField : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string v) { } }
    public class MinAttribute : Attribute { public MinAttribute(float v) { } }
    public class RangeAttribute : Attribute { public RangeAttribute(int a, int b) { } }
    public static class Time { public static float deltaTime = 1, timeScale = 1; }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static int Clamp(int v, int a, int b) => Math.Clamp(v, a, b);
    }
}
public static class PauseMenu { public static bool IsPaused; }
public class RaidManager { public static RaidManager Instance; public bool IsRaidActive; }
public class FactoryHealth { public bool IsAlive; }
