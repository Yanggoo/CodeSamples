using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyChaseState : EnemyState
{
    public EnemyChaseState(EnemyNew enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {
    }

    public override void AnimationTriggerEvent(EnemyNew.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.enemyChaseBaseInstace.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();
        enemy.enemyChaseBaseInstace.DoEnterLogic();
    }

    public override void ExistState()
    {
        base.ExistState();
        enemy.enemyChaseBaseInstace.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        enemy.enemyChaseBaseInstace.DoFrameUpdateLogic();

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.enemyChaseBaseInstace.DoPhysicsLogic();
    }
}
