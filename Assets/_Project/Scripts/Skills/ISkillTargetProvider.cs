using System.Collections.Generic;
using UnityEngine;

public interface ISkillTargetProvider
{
    AutoBattleUnit Player { get; }
    AutoBattleUnit CurrentEnemy { get; }
    void GetEnemiesInRadius(Vector3 position, float radius, List<AutoBattleUnit> results);
}
