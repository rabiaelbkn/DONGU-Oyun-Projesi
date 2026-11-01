using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hikâye sahnelerini (StoryScenario) Rabiş'in diyaloğunda kullanılan aynı DialogueUI kutusunda açar.
/// Böylece tüm konuşmalar aynı görünür: daktilo yazı, tıklanabilir seçenek butonları, 1-4 tuşları, [E] Devam.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public bool IsOpen => DialogueUI.IsOpen;

    private readonly List<DialogueData> runtimeAssets = new List<DialogueData>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateAtStartup()
    {
        if (Instance != null) return;
        GameObject manager = new GameObject("DialogueManager");
        manager.AddComponent<DialogueManager>();
        DontDestroyOnLoad(manager);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Open(StoryScenario scenario) => Open(scenario, null, null);

    public void Open(StoryScenario scenario, Sprite portrait, System.Action onClosed)
    {
        if (scenario == null || IsOpen) return;
        DialogueUI.Get().Open(Build(scenario), portrait, onClosed);
    }

    private DialogueData Build(StoryScenario scenario)
    {
        foreach (var old in runtimeAssets) if (old != null) Destroy(old);
        runtimeAssets.Clear();

        var data = ScriptableObject.CreateInstance<DialogueData>();
        data.name = "Story_" + scenario.Key;
        data.speakerName = scenario.Speaker;
        runtimeAssets.Add(data);

        var start = new DialogueData.Node { id = "start", text = scenario.Line };
        data.nodes.Add(start);

        for (int i = 0; i < scenario.Choices.Length; i++)
        {
            StoryChoice choice = scenario.Choices[i];
            string resultId = "result_" + i;
            start.choices.Add(new DialogueData.Choice
            {
                text = choice.RequiresWater
                    ? choice.Text + $"  <color=#7EC8FF>(Su: -1, kalan {StoryProgress.CleanWater})</color>"
                    : choice.Text,
                next = resultId,
                onPick = () =>
                {
                    if (choice.RequiresWater && !StoryProgress.SpendWater())
                    {
                        DialogueUI.Toast("Mataranda temiz su kalmadı; başka bir seçenek seç.", 3f);
                        return false;
                    }
                    choice.Apply?.Invoke();
                    StoryProgress.Set("Resolved_" + scenario.Key);
                    return true;
                }
            });
            data.nodes.Add(new DialogueData.Node { id = resultId, speaker = scenario.Speaker, text = choice.Result });
        }
        return data;
    }
}
