using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class AutoBattleSensor2D : MonoBehaviour
{
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private bool debugLogs;

    public AutoBattleUnit CurrentTarget { get; private set; }

    public void ClearTarget()
    {
        CurrentTarget = null;
    }

    private void Reset()
    {
        var trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Log($"Object detected: {other.name} on layer {LayerMask.LayerToName(other.gameObject.layer)}");

        if (!IsInTargetLayer(other.gameObject.layer))
        {
            Log($"{other.name} is not in Target Layer mask.");
            return;
        }

        var target = other.GetComponent<AutoBattleUnit>();
        if (target == null)
        {
            Log($"{other.name} does not have AutoBattleUnit component.");
            return;
        }

        CurrentTarget = target;
        Log($"Target SET to {target.UnitName}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (CurrentTarget == null)
        {
            return;
        }

        if (other.GetComponent<AutoBattleUnit>() != CurrentTarget)
        {
            return;
        }

        CurrentTarget = null;
    }

    private bool IsInTargetLayer(int layer)
    {
        return (targetLayers.value & (1 << layer)) != 0;
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log($"[Sensor] {message}");
        }
    }
}
