﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

public class AIController : MonoBehaviour , StateUser
{
    [Header("Flight-related")]
    public FlightModel plane;
    public EngineControl engineControl;
    [SerializeField] float steeringSpeed;
    public float minSpeed;
    public float maxSpeed;
    public float recoverSpeedMin;
    public float recoverSpeedMax;
    [SerializeField] LayerMask groundCollisionMask;
    [SerializeField] float groundCollisionDistance;
    [SerializeField] float groundAvoidanceAngle;
    [SerializeField] float groundAvoidanceMinSpeed;
    [SerializeField] float groundAvoidanceMaxSpeed;
    [SerializeField] float pitchUpThreshold;
    [SerializeField] float fineSteeringAngle;
    [SerializeField] float rollFactor;
    [SerializeField] float yawFactor;

    [Header("Navigation-related")]
    public StateBase currentState;

    [Header("Weaponry-related")]
    public GunsControl guns;
    [SerializeField] bool canUseCannon;
    [SerializeField] float bulletSpeed;
    public float cannonRange;
    [SerializeField] float cannonMaxFireAngle;
    [SerializeField] float cannonBurstLength;
    [SerializeField] float cannonBurstCooldown;
    [SerializeField] float reactionDelayMin;
    [SerializeField] float reactionDelayMax;
    [SerializeField] float reactionDelayDistance;

    Target selfTarget;
    FlightModel targetPlane;
    Vector3 lastInput;
    public bool isRecoveringSpeed;

    bool cannonFiring;
    float cannonBurstTimer;
    float cannonCooldownTimer;

    struct ControlInput
    {
        public float time;
        public Vector3 targetPosition;
    }

    Queue<ControlInput> inputQueue;

    public bool dodging;
    public Vector3 lastDodgePoint;
    public List<Vector3> dodgeOffsets;
    const float dodgeUpdateInterval = 0.25f;
    public float dodgeTimer;


    void Start()
    {
        engineControl = GetComponent<EngineControl>();

        selfTarget = plane.GetComponent<Target>();

        if (plane.target != null)
        {
            targetPlane = plane.target.GetComponent<FlightModel>();
        }

        dodgeOffsets = new List<Vector3>();
        inputQueue = new Queue<ControlInput>();
        currentState.OnStateStart(this);

        recoverSpeedMin = recoverSpeedMin * 3.6f;
        recoverSpeedMax = recoverSpeedMax * 3.6f;
    }

    Vector3 AvoidGround()
    {
        //roll level and pull up
        float roll = plane.rb.rotation.eulerAngles.z;
        if (roll > 180f)
        {
            roll -= 360f;
        }
        return new Vector3(-1, 0, Mathf.Clamp(-roll * rollFactor, -1, 1));
    }

    public Vector3 RecoverSpeed()
    {
        //roll and pitch level
        float roll = plane.rb.rotation.eulerAngles.z;
        float pitch = plane.rb.rotation.eulerAngles.x;
        if (roll > 180f)
        {
            roll -= 360f;
        }
        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        return new Vector3(Mathf.Clamp(-pitch, -1, 1), 0, Mathf.Clamp(-roll * rollFactor, -1, 1));
    }

    public Vector3 GetTargetPosition()
    {
        Vector3 targetPosition = plane.target.transform.position;

        if (Vector3.Distance(targetPosition, plane.rb.position) < cannonRange)
        {
            return Utilities.FirstOrderIntercept(plane.rb.position, plane.rb.linearVelocity, bulletSpeed, targetPosition, plane.target.GetComponent<Rigidbody>().linearVelocity);
        }
        return targetPosition;
    }

    Vector3 CalculateSteering(Vector3 targetPosition)
    {
        if (plane.target == null && targetPosition == Vector3.zero)
        {
            return new Vector3();
        }

        var error = targetPosition - plane.rb.position;
        error = Quaternion.Inverse(plane.rb.rotation) * error;   //transform into local space

        var errorDir = error.normalized;
        var pitchError = new Vector3(0, error.y, error.z).normalized;
        var rollError = new Vector3(error.x, error.y, 0).normalized;
        var yawError = new Vector3(error.x, 0, error.z).normalized;

        var targetInput = new Vector3();

        var pitch = Vector3.SignedAngle(Vector3.forward, pitchError, Vector3.right);
        if (-pitch < pitchUpThreshold)
        {
            pitch += 360f;
        }
        targetInput.x = pitch;

        if (Vector3.Angle(Vector3.forward, errorDir) < fineSteeringAngle)
        {
            var yaw = Vector3.SignedAngle(Vector3.forward, yawError, Vector3.up);
            targetInput.y = yaw * yawFactor;
        }
        else
        {
            var roll = Vector3.SignedAngle(Vector3.up, rollError, Vector3.forward);
            targetInput.z = roll * rollFactor;
        }

        targetInput.x = Mathf.Clamp(targetInput.x, -1, 1);
        targetInput.y = Mathf.Clamp(targetInput.y, -1, 1);
        targetInput.z = Mathf.Clamp(targetInput.z, -1, 1);

        var input = Vector3.MoveTowards(lastInput, targetInput, steeringSpeed * Time.deltaTime);
        lastInput = input;

        return input;
    }

    public float CalculateThrottle(float minSpeed, float maxSpeed)
    {
        float input = 0;

        if (plane.localVelocity.z < minSpeed)
        {
            input = 1f;
        }
        else if (plane.localVelocity.z > maxSpeed)
        {
            input = -1f;
        }
        else
        {
            input = 0f;
        }

        plane.brakeInput = plane.currentSpeed >= (plane.neverExceedSpeed - 15f);

        return input;
    }

    public void CalculateWeapons(float dt)
    {
        if (plane.target == null) return;

        if (canUseCannon)
        {
            CalculateCannon(dt);
        }
    }

    void CalculateCannon(float dt)
    {
        if (targetPlane == null)
        {
            cannonFiring = false;
            return;
        }

        if (cannonFiring)
        {
            cannonBurstTimer = Mathf.Max(0, cannonBurstTimer - dt);

            if (cannonBurstTimer == 0)
            {
                cannonFiring = false;
                cannonCooldownTimer = cannonBurstCooldown;
                guns.trigger = false;
                //plane.SetCannonInput(false);
            }
        }
        else
        {
            cannonCooldownTimer = Mathf.Max(0, cannonCooldownTimer - dt);

            var targetPosition = Utilities.FirstOrderIntercept(plane.rb.position, plane.rb.linearVelocity, bulletSpeed, plane.target.transform.position, plane.target.rb.linearVelocity);

            var error = targetPosition - plane.rb.position;
            var range = error.magnitude;
            var targetDir = error.normalized;
            var targetAngle = Vector3.Angle(targetDir, plane.rb.rotation * Vector3.forward);

            if (range < cannonRange && targetAngle < cannonMaxFireAngle && cannonCooldownTimer == 0)
            {
                cannonFiring = true;
                cannonBurstTimer = cannonBurstLength;
                guns.trigger = true;
                //plane.SetCannonInput(true);
            }
        }
    }

    public void SteerToTarget(Vector3 planePosition)
    {
        bool foundTarget = false;
        Vector3 steering = Vector3.zero;
        Vector3 targetPosition = Vector3.zero;

        var delay = reactionDelayMax;

        if (Vector3.Distance(planePosition, plane.rb.position) < reactionDelayDistance)
        {
            delay = reactionDelayMin;
        }

        while (inputQueue.Count > 0)
        {
            var input = inputQueue.Peek();

            if (input.time + delay <= Time.time)
            {
                targetPosition = input.targetPosition;
                inputQueue.Dequeue();
                foundTarget = true;
            }
            else
            {
                break;
            }
        }

        if (foundTarget)
        {
            steering = CalculateSteering(targetPosition);
        }
        plane.SetControlInput(steering);
    }


    public Vector3 steering;
    public float throttle;
    public bool emergency;
    public Vector3 targetPosition;
    void FixedUpdate()
    {
        var velocityRot = Quaternion.LookRotation(plane.rb.linearVelocity.normalized);
        var ray = new Ray(plane.rb.position, velocityRot * Quaternion.Euler(groundAvoidanceAngle, 0, 0) * Vector3.forward);

        ExecuteStateOnUpdate();

        if (Physics.Raycast(ray, groundCollisionDistance + plane.localVelocity.z, groundCollisionMask.value))
        {
            steering = AvoidGround();
            plane.SetControlInput(steering);
            throttle = CalculateThrottle(groundAvoidanceMinSpeed, groundAvoidanceMaxSpeed);
            emergency = true;
        }
        else
        {
            if(targetPlane == null)
            {
                if(plane.target != null)
                {
                    targetPlane = plane.target;
                }
            }
            inputQueue.Enqueue(new ControlInput { time = Time.time, targetPosition = targetPosition, });
            ExecuteStateOnUpdate();
        }
    }

    public void ChangeState(StateBase newState)
    {
        currentState.OnStateEnd();
        currentState = newState;
        currentState.OnStateStart(this);
    }

    public void ExecuteStateOnUpdate()
    {
        if(currentState != null)
        {
            currentState.OnStateStay();
        }
    }

    public void ExecuteStateOnStart()
    {
        if (currentState != null)
        {
            currentState.OnStateStart(this);
        }
    }
}
