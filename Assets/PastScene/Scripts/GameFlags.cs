using System.Collections.Generic;

/// <summary>
/// Oyun boyunca (sahneler arasında da) tutulan basit bayraklar.
/// Örn: "gorev_alindi", "ornek_alindi", "kanit_teslim".
/// </summary>
public static class GameFlags
{
    static readonly HashSet<string> flags = new HashSet<string>();

    public static event System.Action<string> FlagSet;

    public static bool Has(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);

    public static void Set(string flag)
    {
        if (string.IsNullOrEmpty(flag) || flags.Contains(flag)) return;
        flags.Add(flag);
        FlagSet?.Invoke(flag);
    }

    public static void Clear(string flag) => flags.Remove(flag);
    public static void ClearAll() => flags.Clear();

    // Play'e her basışta temiz başla (Domain Reload kapalı olsa bile)
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetOnPlay() { flags.Clear(); FlagSet = null; }
}
