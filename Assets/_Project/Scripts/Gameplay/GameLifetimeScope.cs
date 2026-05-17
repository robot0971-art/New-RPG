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
    [SerializeField] private PlayerResources playerResources;
    [SerializeField] private bool debugLogs;

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

        ResolveSceneReference(ref player);
        ResolveSceneReference(ref backgroundScroller);
        ResolveSceneReference(ref playerSensor);
        ResolveSceneReference(ref battleController);
        ResolveSceneReference(ref statUpgradeManager);
        ResolveSceneReference(ref saveManager);
        ResolveSceneReference(ref skillManager);
        ResolveSceneReference(ref skillVfxPlayer);
        ResolveSceneReference(ref playerResources);
    }

    private void RegisterSceneReferences()
    {
        Register(player);

        if (battleController != null)
        {
            Register(battleController);
            Register<ISkillTargetProvider>(battleController);
            Register<IEnemyDefeatHandler>(battleController);
            Register<IAttackHitNotifier>(battleController);
        }

        Register(playerSensor);
        Register(backgroundScroller);
        Register(statUpgradeManager);
        Register(saveManager);
        Register(skillManager);
        Register(skillVfxPlayer);
        Register<ISkillVfxPlayer>(skillVfxPlayer);
        Register(playerResources);
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

        DIContainer.Global.TryResolve(out AutoBattleUnit resolvedPlayer);
        DIContainer.Global.TryResolve(out BackgroundScroller resolvedBackgroundScroller);
        DIContainer.Global.TryResolve(out AutoBattleSensor2D resolvedPlayerSensor);
        DIContainer.Global.TryResolve(out GameManager resolvedGameManager);

        battleController.Construct(
            resolvedPlayer,
            resolvedBackgroundScroller,
            resolvedPlayerSensor,
            resolvedGameManager);

        if (debugLogs)
        {
            Debug.Log($"[GameDI] Battle injection - P:{resolvedPlayer != null}, BS:{resolvedBackgroundScroller != null}, PS:{resolvedPlayerSensor != null}, GM:{resolvedGameManager != null}");
        }
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

        DIContainer.Global.TryResolve(out ISkillTargetProvider targetProvider);
        DIContainer.Global.TryResolve(out IEnemyDefeatHandler enemyDefeatHandler);
        DIContainer.Global.TryResolve(out IAttackHitNotifier attackHitNotifier);
        DIContainer.Global.TryResolve(out ISkillVfxPlayer resolvedVfxPlayer);

        skillManager.Construct(targetProvider, enemyDefeatHandler, attackHitNotifier, resolvedVfxPlayer, playerResources);
    }

    private static void ResolveSceneReference<T>(ref T reference) where T : Object
    {
        if (reference == null)
        {
            reference = Object.FindFirstObjectByType<T>();
        }
    }

    private static void Register<T>(T instance)
    {
        if (instance != null)
        {
            DIContainer.Global.Register(instance);
        }
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
