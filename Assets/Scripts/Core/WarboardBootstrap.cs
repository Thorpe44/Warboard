using UnityEngine;

public static class WarboardBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterFactionRules()
    {
        AeldariFactionModule.Register();
        NecronFactionModule.Register();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartGame()
    {
        if (Object.FindAnyObjectByType<GameController>() != null)
            return;

        GameObject go = new GameObject("WarboardGame");
        go.AddComponent<GameController>();
    }
}
