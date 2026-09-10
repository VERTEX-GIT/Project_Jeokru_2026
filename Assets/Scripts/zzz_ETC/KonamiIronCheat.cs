using UnityEngine;
using UnityEngine.InputSystem;

public sealed class KonamiIronCheat : MonoBehaviour
{
    private const string Command = "UUDDLRLRBA";
    private string input = "";

    // 씬이나 기존 스크립트를 수정하지 않고 자동으로 실행합니다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (FindAnyObjectByType<KonamiIronCheat>() != null) return;
        var cheat = new GameObject(nameof(KonamiIronCheat));
        cheat.AddComponent<KonamiIronCheat>();
        DontDestroyOnLoad(cheat);
    }

    private void Update()
    {
        if (Keyboard.current == null || ResourceInventory.Inventory == null)
        {
            input = "";
            return;
        }

        foreach (var key in Keyboard.current.allKeys)
        {
            if (!key.wasPressedThisFrame) continue;
            char code = key.keyCode switch
            {
                Key.UpArrow => 'U', Key.DownArrow => 'D',
                Key.LeftArrow => 'L', Key.RightArrow => 'R',
                Key.B => 'B', Key.A => 'A', _ => '?'
            };
            if (Accept(code)) ResourceInventory.Inventory.Add(ResourceType.Iron, 999);
        }
    }

    private bool Accept(char code)
    {
        input += code;
        if (input.Length > Command.Length) input = input.Substring(1);
        if (input != Command) return false;
        input = "";
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("Check Konami Command")]
    private void CheckCommand()
    {
        string saved = input;
        input = "";
        int matches = 0;
        // 잘못된 순서, 중간 오입력, 겹치는 시작 입력, 연속 두 번 성공.
        foreach (char code in "UUDDLRLRABUUDD?LRLRBAUUUDDLRLRBAUUDDLRLRBA")
            if (Accept(code)) matches++;
        input = saved;
        Debug.Assert(matches == 2, "Konami command check failed");
    }
#endif
}
