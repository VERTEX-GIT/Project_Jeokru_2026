using UnityEngine;

public enum GameStartMode
{
    NewGame,
    Continue
}

public static class GameSession
{
    public static GameStartMode StartMode
    {
        get;
        private set;
    } = GameStartMode.NewGame;

    public static void StartNewGame()
    {
        StartMode =
            GameStartMode.NewGame;
    }

    public static void ContinueGame()
    {
        StartMode =
            GameStartMode.Continue;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        StartMode =
            GameStartMode.NewGame;
    }
}
