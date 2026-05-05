using System;

public interface IAttackHitNotifier
{
    event Action<AutoBattleUnit, AutoBattleUnit> PlayerAttackResolved;
}
