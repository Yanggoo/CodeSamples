using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyJumpState : EnemyState
{
    public EnemyJumpState(EnemyNew enemy, EnemyStateMachine enemyStateMachine) : base(enemy, enemyStateMachine)
    {
    }

    public override void AnimationTriggerEvent(EnemyNew.AnimationTriggerType triggerType)
    {
        base.AnimationTriggerEvent(triggerType);
        enemy.enemyJumpeBaseInstace.DoAnimationTriggerEventLogic(triggerType);
    }

    public override void EnterState()
    {
        base.EnterState();
        enemy.enemyJumpeBaseInstace.DoEnterLogic();
    }

    public override void ExistState()
    {
        base.ExistState();
        enemy.enemyJumpeBaseInstace.DoExitLogic();
    }

    public override void FrameUpdate()
    {
        base.FrameUpdate();
        enemy.enemyJumpeBaseInstace.DoFrameUpdateLogic();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        enemy.enemyJumpeBaseInstace.DoPhysicsLogic();
    }
    
}
