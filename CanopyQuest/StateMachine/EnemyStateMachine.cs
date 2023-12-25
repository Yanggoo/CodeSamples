using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStateMachine
{
    public EnemyState currentEnemyState { get; set; }

    public void Initialize(EnemyState startState)
    {
        currentEnemyState = startState;
        currentEnemyState.EnterState();
    }

    public void ChangeState(EnemyState newState)
    {
        currentEnemyState.ExistState();
        currentEnemyState = newState;
        currentEnemyState.EnterState();
    }

}
