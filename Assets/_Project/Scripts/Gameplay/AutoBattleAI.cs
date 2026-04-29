using UnityEngine;

public sealed class AutoBattleAI : MonoBehaviour
{
    [SerializeField] private AutoBattleSensor2D sensor;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopRange = 1.5f;

    private AutoBattleUnit unit;

    private void Awake()
    {
        unit = GetComponent<AutoBattleUnit>();
        if (sensor == null) sensor = GetComponentInChildren<AutoBattleSensor2D>();
    }

    private void Update()
    {
        if (unit == null || unit.IsDead)
        {
            return;
        }

        var target = sensor?.CurrentTarget;
        if (target == null || target.IsDead)
        {
            unit.PlayIdle();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.transform.position);
        if (distance > stopRange)
        {
            unit.PlayRun();
            transform.position = Vector3.MoveTowards(
                transform.position, target.transform.position, moveSpeed * Time.deltaTime);
        }
        else
        {
            unit.PlayIdle();
        }
    }
}
