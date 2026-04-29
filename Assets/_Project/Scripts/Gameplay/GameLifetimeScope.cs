using UnityEngine;

public sealed class GameLifetimeScope : MonoBehaviour
{
    [SerializeField] private AutoBattleUnit player;
    [SerializeField] private AutoBattleController battleController;
    [SerializeField] private AutoBattleSensor2D playerSensor;
    [SerializeField] private BackgroundScroller backgroundScroller;

    private void Awake()
    {
        // 인스펙터 연결이 누락된 경우 씬에서 자동으로 찾기
        if (player == null) player = GameObject.FindWithTag("Player")?.GetComponent<AutoBattleUnit>();
        if (player == null) player = Object.FindFirstObjectByType<AutoBattleUnit>();
        
        if (backgroundScroller == null) backgroundScroller = Object.FindFirstObjectByType<BackgroundScroller>();
        if (playerSensor == null) playerSensor = Object.FindFirstObjectByType<AutoBattleSensor2D>();
        if (battleController == null) battleController = Object.FindFirstObjectByType<AutoBattleController>();

        // 의존성 등록
        if (player != null) DIContainer.Global.Register(player);
        if (playerSensor != null) DIContainer.Global.Register(playerSensor);
        if (backgroundScroller != null) DIContainer.Global.Register(backgroundScroller);

        InjectDependencies();
    }

    private void InjectDependencies()
    {
        if (battleController == null) return;

        var p = DIContainer.Global.Resolve<AutoBattleUnit>();
        var bs = DIContainer.Global.Resolve<BackgroundScroller>();
        var ps = DIContainer.Global.Resolve<AutoBattleSensor2D>();
        var gm = DIContainer.Global.Resolve<GameManager>();

        battleController.Construct(p, bs, ps, gm);
        Debug.Log($"[GameDI] Injection Result - P:{p!=null}, BS:{bs!=null}, PS:{ps!=null}, GM:{gm!=null}");
    }
}
