using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class StageManager : MonoBehaviour, ISaveable
{
    [Header("Stage")]
    [SerializeField] private StageBalanceData stageBalance;
    [SerializeField] private int currentStage = 1;
    [Min(0)]
    [SerializeField] private int normalKillsRequiredForBoss = 10;

    [Header("Boss")]
    [SerializeField] private float bossHealthMultiplier = 12f;
    [SerializeField] private float bossAttackMultiplier = 2f;
    [SerializeField] private float bossAttackIntervalMultiplier = 1f;
    [SerializeField] private float bossScaleMultiplier = 1.8f;

    [Header("UI")]
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private Button bossButton;
    [SerializeField] private GameObject bossLockedVisual;
    [SerializeField] private StageTransitionUI stageTransitionUI;
    [SerializeField, Min(0f)] private float bossClearRewardDelay = 1.25f;

    private AutoBattleController battleController;
    private int normalKillCount;
    private bool bossAvailable;
    private bool bossBattleActive;
    private bool isTransitioning;
    private Coroutine transitionRoutine;

    public int CurrentStage => Mathf.Max(1, currentStage);
    public bool BossAvailable => !bossBattleActive && (bossAvailable || NormalKillsRequiredForBoss <= 0);
    public bool BossBattleActive => bossBattleActive;
    public bool IsTransitioning => isTransitioning;
    public float BossHealthMultiplier => stageBalance != null ? stageBalance.BossHealthMultiplier : bossHealthMultiplier;
    public float BossAttackMultiplier => stageBalance != null ? stageBalance.BossAttackMultiplier : bossAttackMultiplier;
    public float BossAttackIntervalMultiplier => stageBalance != null ? stageBalance.BossAttackIntervalMultiplier : bossAttackIntervalMultiplier;
    public float BossScaleMultiplier => stageBalance != null ? stageBalance.BossScaleMultiplier : bossScaleMultiplier;
    private int NormalKillsRequiredForBoss => stageBalance != null ? stageBalance.NormalKillsRequiredForBoss : normalKillsRequiredForBoss;
    private float BossClearRewardDelay => stageBalance != null ? stageBalance.BossClearRewardDelay : bossClearRewardDelay;

    private void Awake()
    {
        currentStage = Mathf.Max(1, currentStage);
        normalKillsRequiredForBoss = Mathf.Max(0, normalKillsRequiredForBoss);
        DIContainer.Global.Register(this);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (bossButton != null)
        {
            bossButton.onClick.RemoveListener(TryStartBossBattle);
            bossButton.onClick.AddListener(TryStartBossBattle);
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (bossButton != null)
        {
            bossButton.onClick.RemoveListener(TryStartBossBattle);
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            isTransitioning = false;
        }
    }

    public int GetUnlockedMonsterCount(int totalMonsterCount)
    {
        return Mathf.Clamp(CurrentStage, 1, Mathf.Max(1, totalMonsterCount));
    }

    public int GetBossMonsterIndex(int totalMonsterCount)
    {
        return Mathf.Clamp(CurrentStage - 1, 0, Mathf.Max(0, totalMonsterCount - 1));
    }

    public void NotifyEnemyDefeated(bool wasBoss)
    {
        if (wasBoss)
        {
            CompleteBossBattle();
            return;
        }

        if (bossBattleActive)
        {
            return;
        }

        normalKillCount++;
        if (normalKillCount >= NormalKillsRequiredForBoss)
        {
            bossAvailable = true;
        }

        RefreshUI();
        SaveEvents.RequestSave();
    }

    public void NotifyBossBattleStarted()
    {
        bossBattleActive = true;
        bossAvailable = false;
        RefreshUI();
    }

    public void CaptureSaveData(SaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.currentStage = CurrentStage;
        saveData.normalKillCount = normalKillCount;
        saveData.bossAvailable = bossAvailable;
    }

    public void RestoreSaveData(SaveData saveData)
    {
        if (saveData == null)
        {
            return;
        }

        currentStage = Mathf.Max(1, saveData.currentStage);
        normalKillCount = Mathf.Max(0, saveData.normalKillCount);
        bossAvailable = saveData.bossAvailable;
        bossBattleActive = false;
        RefreshUI();
    }

    private void TryStartBossBattle()
    {
        ResolveReferences();
        if (!BossAvailable || battleController == null)
        {
            RefreshUI();
            return;
        }

        if (battleController.StartBossBattle())
        {
            NotifyBossBattleStarted();
        }
    }

    private void CompleteBossBattle()
    {
        if (transitionRoutine != null)
        {
            return;
        }

        ApplyBossBattleClearState();

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            isTransitioning = false;
            SaveEvents.RequestSave();
            return;
        }

        transitionRoutine = StartCoroutine(BossBattleClearPresentationRoutine());
    }

    private void ApplyBossBattleClearState()
    {
        isTransitioning = true;
        bossBattleActive = false;
        bossAvailable = false;
        normalKillCount = 0;
        currentStage = CurrentStage + 1;
        RefreshUI();
        ResolveReferences();
        battleController?.SetPlayerIdle();
    }

    private IEnumerator BossBattleClearPresentationRoutine()
    {
        if (BossClearRewardDelay > 0f)
        {
            yield return new WaitForSeconds(BossClearRewardDelay);
        }

        if (stageTransitionUI != null)
        {
            yield return stageTransitionUI.PlayStageTransition(CurrentStage);
        }

        RefreshUI();
        SaveEvents.RequestSave();

        isTransitioning = false;
        transitionRoutine = null;
    }

    private void RefreshUI()
    {
        if (stageText != null)
        {
            stageText.text = bossBattleActive ? $"Stage {CurrentStage} BOSS" : $"Stage {CurrentStage}";
        }

        if (bossButton != null)
        {
            bossButton.gameObject.SetActive(BossAvailable);
            bossButton.interactable = BossAvailable;
        }

        if (bossLockedVisual != null)
        {
            bossLockedVisual.SetActive(!BossAvailable);
        }
    }

    private void ResolveReferences()
    {
        if (battleController == null)
        {
            battleController = DIContainer.Global.Resolve<AutoBattleController>();
        }

        if (battleController == null)
        {
            battleController = Object.FindFirstObjectByType<AutoBattleController>();
        }
    }
}
