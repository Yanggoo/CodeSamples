using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyState
{
    protected EnemyNew enemy;
    protected EnemyStateMachine enemyStateMachine;

    public EnemyState(EnemyNew enemy, EnemyStateMachine enemyStateMachine)
    {
        this.enemy = enemy;
        this.enemyStateMachine = enemyStateMachine;
    }

    public virtual void EnterState()
    {

    }

    public virtual void ExistState()
    {

    }

    public virtual void FrameUpdate()
    {

    }

    public virtual void PhysicsUpdate()
    {

    }

    public virtual void AnimationTriggerEvent(EnemyNew.AnimationTriggerType triggerType)
    {
    }
}
