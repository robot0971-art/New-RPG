using UnityEngine;

public sealed class GameLifetimeScope : MonoBehaviour
{
    [SerializeField] private AutoBattleUnit player;
    [SerializeField] private AutoBattleController battleController;
    [SerializeField] private AutoBattleSensor2D playerSensor;
    [SerializeField] private BackgroundScroller backgroundScroller;
    [SerializeField] private StatUpgradeManager statUpgradeManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private SkillManager skillManager;
    [SerializeField] private SkillVfxPlayer skillVfxPlayer;

    private void Awake()
    {
        ResolveSceneReferences();
        RegisterSceneReferences();
        InjectDependencies();
    }

    private void ResolveSceneReferences()
    {
        if (player == null)
        {
            player = GameObject.FindWithTag("Player")?.GetComponent<AutoBattleUnit>();
        }

        if (player == null)
        {
            player = Object.FindFirstObjectByType<AutoBattleUnit>();
        }

        if (backgroundScroller == null)
        {
            backgroundScroller = Object.FindFirstObjectByType<BackgroundScroller>();
        }

        if (playerSensor == null)
        {
            playerSensor = Object.FindFirstObjectByType<AutoBattleSensor2D>();
        }

        if (battleController == null)
        {
            battleController = Object.FindFirstObjectByType<AutoBattleController>();
        }

        if (statUpgradeManager == null)
        {
            statUpgradeManager = Object.FindFirstObjectByType<StatUpgradeManager>();
        }

        if (saveManager == null)
        {
            saveManager = Object.FindFirstObjectByType<SaveManager>();
        }

        if (skillManager == null)
        {
            skillManager = Object.FindFirstObjectByType<SkillManager>();
        }

        if (skillVfxPlayer == null)
        {
            skillVfxPlayer = Object.FindFirstObjectByType<SkillVfxPlayer>();
        }
    }

    private void RegisterSceneReferences()
    {
        if (player != null)
        {
            DIContainer.Global.Register(player);
        }

        if (playerSensor != null)
        {
            DIContainer.Global.Register(playerSensor);
        }

        if (backgroundScroller != null)
        {
            DIContainer.Global.Register(backgroundScroller);
        }

        if (statUpgradeManager != null)
        {
            DIContainer.Global.Register(statUpgradeManager);
        }

        if (saveManager != null)
        {
            DIContainer.Global.Register(saveManager);
        }

        if (skillManager != null)
        {
            DIContainer.Global.Register(skillManager);
        }

        if (skillVfxPlayer != null)
        {
            DIContainer.Global.Register(skillVfxPlayer);
        }
    }

    private void InjectDependencies()
    {
        InjectBattleController();
        InjectSkillManager();
        InjectSaveManager();
    }

    private void InjectBattleController()
    {
        if (battleController == null)
        {
            return;
        }

        var resolvedPlayer = DIContainer.Global.Resolve<AutoBattleUnit>();
        var resolvedBackgroundScroller = DIContainer.Global.Resolve<BackgroundScroller>();
        var resolvedPlayerSensor = DIContainer.Global.Resolve<AutoBattleSensor2D>();
        var resolvedGameManager = DIContainer.Global.Resolve<GameManager>();

        battleController.Construct(
            resolvedPlayer,
            resolvedBackgroundScroller,
            resolvedPlayerSensor,
            resolvedGameManager);

        Debug.Log($"[GameDI] Battle injection - P:{resolvedPlayer != null}, BS:{resolvedBackgroundScroller != null}, PS:{resolvedPlayerSensor != null}, GM:{resolvedGameManager != null}");
    }

    private void InjectSaveManager()
    {
        if (saveManager == null)
        {
            return;
        }

        saveManager.Construct(
            new JsonFileSaveStorage("save.json"),
            FindSaveablesInScene());
    }

    private void InjectSkillManager()
    {
        if (skillManager == null)
        {
            return;
        }

        skillManager.Construct(battleController, skillVfxPlayer);
    }

    private static ISaveable[] FindSaveablesInScene()
    {
        var behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        var saveables = new System.Collections.Generic.List<ISaveable>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is ISaveable saveable)
            {
                saveables.Add(saveable);
            }
        }

        return saveables.ToArray();
    }
}
