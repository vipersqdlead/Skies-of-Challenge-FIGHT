using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class DogfightingState : StateBase
{
    public AIController controller;
    public float targetLookRange = 12000f;
    bool missileCooldown;
    public float missileCooldownTime;
    public float missileCooldownTimer;

    public void OnStateStart(AIController userController)
    {
        controller = userController;
        if (controller.plane.target == null)
        {
            controller.plane.target = Utilities.GetNearestTarget(gameObject, controller.plane.side, targetLookRange);
            print(controller.plane.target.ToString());
        }

        missileCooldownTimer = missileCooldownTime;
    }

    public override void OnStateStay()
    {   if(controller == null)
        {
            print("Controller is null!");
            return;
        }
        
        if(controller.plane.target == null)
        {
            LookingForTargets();
            controller.guns.trigger = false;
            return;
        }

        controller.dodging = false;
        controller.targetPosition = controller.GetTargetPosition();

        if ((controller.plane.currentSpeed < controller.recoverSpeedMin || controller.isRecoveringSpeed))
        {
            controller.isRecoveringSpeed = controller.plane.currentSpeed < controller.recoverSpeedMax;

            controller.steering = controller.RecoverSpeed();
            controller.throttle = 1;
            controller.emergency = true;
        }
        else
        {
            controller.emergency = false;
            controller.throttle = controller.CalculateThrottle(controller.minSpeed, controller.maxSpeed);
        }

        controller.engineControl.SetThrottle(controller.throttle);

        if (controller.emergency)
        {
            controller.guns.trigger = false;
            controller.plane.SetControlInput(controller.steering);
            return;
        }
        else if(controller.plane.target != null)
        {
            controller.SteerToTarget(controller.plane.target.transform.position);
            controller.CalculateWeapons(Time.fixedDeltaTime);
        }
    }

    public override void OnStateEnd()
    {
        throw new System.NotImplementedException();
    }

    float lookTimer;
    void LookingForTargets()
    {
        lookTimer += Time.deltaTime;
        if(lookTimer > 5f)
        {
            controller.plane.target = Utilities.GetNearestTarget(gameObject, controller.plane.side, targetLookRange);
            lookTimer = 0f;
        }
    }
}
