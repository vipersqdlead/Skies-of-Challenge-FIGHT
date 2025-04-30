using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightModel : MonoBehaviour
{
    [Header("Status")]
    public Vector3 localVelocity;
    public Vector3 localAngularVelocity;
    public float currentSpeed;
    Vector3 LocalGForce;
    [SerializeField] public float gForce;
    [SerializeField] public float angleOfAttack;
    [SerializeField] public float criticalAoA;
    public float maxAngleOfAttack;
    [SerializeField] public float rateOfClimb;
    public float machSpeed;


    [Header("Battle related")]
    public FlightModel target;
    public int side; // Use 0 for enemy side, 1 for allied side, 2 for neutral or neither
    public float SpawnSpeed;

    [Header("Flight-model related")]
    public Rigidbody rb; //Rigidbody of the aircraft
    [SerializeField] float wingSpan; // in Meters
    [SerializeField] public float wingArea; // in Square Meters
    public enum WingType { StraightWing, SweptWing, DeltaWing  };
    public WingType wingType;
    [SerializeField] float sweepAngle;
    [SerializeField] float aspectRatio;
    public float drag;
    public bool flaps, airbrake, stalling;
    public float stallSpeed;
    public float neverExceedSpeed;

    public AnimationCurve RollForce, PitchForce, YawForce;
    [SerializeField] TrailRenderer wing1, wing2;
    [SerializeField] Transform centerOfLift;

    public HealthPoints health;
    public ControlSurfaceAnimation anims;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anims = GetComponent<ControlSurfaceAnimation>();
        rb.linearDamping = Mathf.Epsilon;
        aspectRatio = (wingSpan * wingSpan) / wingArea;
        if(maxAngleOfAttack == 0)
        {
            CalculateMaxAoA();
        }
        criticalAoA = maxAngleOfAttack * 0.9f;
        rb.AddForce(transform.forward * SpawnSpeed, ForceMode.VelocityChange);
    }

    void CalculateMaxAoA()
    {
        switch (wingType)
        {
            case WingType.StraightWing:
                {
                    float CLMax = 1.1f + (aspectRatio / 20f);
                    maxAngleOfAttack = (CLMax / (Mathf.PI * 2)) * 57.3f;
                    if (anims != null)
                    {
                        maxAngleOfAttack += anims.slatExtensionDistance * 10f;
                    }
                    break;
                }
            case WingType.SweptWing:
                {
                    float CLMax = 1.1f + (aspectRatio / 20f) * Mathf.Cos(Mathf.Deg2Rad * sweepAngle);
                    maxAngleOfAttack = (CLMax / (Mathf.PI * 2)) * 57.3f;
                    if (anims != null)
                    {
                        maxAngleOfAttack += anims.slatExtensionDistance * 10f;
                    }
                    break;
                }
            case WingType.DeltaWing:
                {
                    float CLMax = 1.1f + (aspectRatio / 20f);
                    CLMax = CLMax * Mathf.Cos(Mathf.Deg2Rad * sweepAngle);
                    float CLMax2 = Mathf.Sin(Mathf.Deg2Rad * sweepAngle);
                    CLMax2 = Mathf.Pow(CLMax2, 2f);
                    CLMax2 = CLMax2 * 4f;
                    CLMax = CLMax + CLMax2;
                    maxAngleOfAttack = (CLMax / (Mathf.PI * 2f)) * 57.3f;
                    if (anims != null)
                    {
                        maxAngleOfAttack += anims.slatExtensionDistance * 10f;
                    }
                    break;
                }
        }
        criticalAoA = maxAngleOfAttack * 0.9f;
    }
    private void FixedUpdate()
    {
        calculateForces();
        calculateForcesLateral();
        CalculateState();
        CheckFlapsAndBrakes();

        currentSpeed = rb.linearVelocity.magnitude * 3.6f;
    }

    private void calculateForces()
    {
        // *flip sign(s) if necessary*
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        float _angleOfAttack = Mathf.Atan2(-localVelocity.y, localVelocity.z);

        // α * 2 * PI * (AR / AR + 2)
        float inducedLift = _angleOfAttack * (aspectRatio / (aspectRatio + 2f)) * 2f * Mathf.PI;

        // CL ^ 2 / (AR * PI)
        float inducedDrag = (inducedLift * inducedLift) / (aspectRatio * Mathf.PI);

        // V ^ 2 * R * 0.5 * A
        float pressure = (rb.linearVelocity.sqrMagnitude) * 1.2754f * 0.5f * wingArea;


        float extradrag = 1f;
        if (anims != null)
        {
                float extraLift = anims.flapExtensionValue * (anims.flapExtensionAngle / 100f) + 1f;
                inducedLift = inducedLift * Mathf.Clamp(extraLift, 1f, extraLift);
                inducedDrag = inducedDrag * Mathf.Clamp(extraLift * 2, 1f, extraLift * 2);
            
                extradrag = anims.brakeExtensionValue * (anims.airbrakeExtensionAngle / 100f) + 1f;
                inducedDrag = inducedDrag * Mathf.Clamp(extradrag * anims.airbrakes.Length - 1, 1f, extradrag * anims.airbrakes.Length - 1);
        }

        float stallLiftFactor = Mathf.Clamp01((currentSpeed - stallSpeed) / stallSpeed + 1f);
        float lift = inducedLift * pressure * stallLiftFactor;
        float _drag = ((drag + inducedDrag) * pressure) * extradrag;


        // *flip sign(s) if necessary*
        Vector3 dragDirection = rb.linearVelocity.normalized;
        Vector3 liftDirection = Vector3.Cross(dragDirection, transform.right);

        // Lift + Drag = Total Force

        stalling = angleOfAttack > maxAngleOfAttack || angleOfAttack < -maxAngleOfAttack;
        if (stalling)
        {
            Vector3 velocityDirection = rb.linearVelocity.normalized;
            Vector3 torqueDirection = Vector3.Cross(Vector3.up, velocityDirection);

            // Apply torque to align nose with velocity (nose-down effect)
            float torqueMultiplier = Mathf.Abs(angleOfAttack) * 1000f;
            rb.AddTorque(torqueDirection * torqueMultiplier * (Time.deltaTime * 60f), ForceMode.Force);
        }
        rb.AddForce(liftDirection * lift - dragDirection * _drag);
    }

    public void calculateControlForces(float pitch, float yaw, float roll)
    {
        // Pitch force
        float pitchOutput = pitch * (PitchForce.Evaluate(machSpeed) * 100f);
        if(anims != null)
        {
                pitchOutput -= anims.flapExtensionValue * (anims.flapExtensionAngle / 10f);
        }

        // Yaw force
        float yawOutput = yaw * (YawForce.Evaluate(machSpeed) * 100f);

        // Roll force
        float rollOutput = roll * (RollForce.Evaluate(machSpeed) * 100f);

        Quaternion rot = rb.rotation * Quaternion.Euler(pitchOutput * Time.deltaTime, yawOutput * Time.deltaTime, (-rollOutput + -yawOutput) * Time.deltaTime);
        rb.MoveRotation(rot);

        if (angleOfAttack >= 5f)
        {
            wing1.emitting = true;
            wing2.emitting = true;
        }
        else
        {
            wing1.emitting = false;
            wing2.emitting = false;
        }
    }

    private void calculateForcesLateral()
    {
        // *flip sign(s) if necessary*
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        // float _angleOfAttack = Mathf.Atan2(-localVelocity.x, localVelocity.z);
        float _angleOfAttack = Mathf.Atan2(-localVelocity.x, localVelocity.z);


        // α * 2 * PI * (AR / AR + 2)
        float inducedLift = _angleOfAttack * ((wingArea / 2) / ((wingArea / 2) + 2f)) * 2f * Mathf.PI;

        // CL ^ 2 / (AR * PI)
        float inducedDrag = (inducedLift * inducedLift) / (aspectRatio * Mathf.PI);

        // V ^ 2 * R * 0.5 * A
        float pressure = (rb.linearVelocity.sqrMagnitude) * 1.2754f * 0.5f * (wingArea / 2);

        float lift = inducedLift * pressure;
        float _drag = ((drag + inducedDrag) * pressure);
        //float _drag = (0.021f + inducedDrag) * pressure;

        // *flip sign(s) if necessary*
        Vector3 dragDirection = rb.linearVelocity.normalized;
        Vector3 liftDirection = Vector3.Cross(dragDirection, -transform.up);

        rb.AddForce(liftDirection * lift - dragDirection * _drag);
        if(_angleOfAttack > 5f || _angleOfAttack < -5f)
        {
            Vector3 velocityDirection = rb.linearVelocity.normalized;
            Vector3 torqueDirection = Vector3.Cross(Vector3.up, velocityDirection);

            // Apply torque to align nose with velocity (nose-down effect)
            float torqueMultiplier = Mathf.Abs(_angleOfAttack * Mathf.Rad2Deg) * 1000f;
            rb.AddTorque(torqueDirection * torqueMultiplier * (Time.deltaTime * 60f), ForceMode.Force);
        }
    }

    public Vector3 controlInput;
    public void SetControlInput(Vector3 input)
    {
        float AoADampFactor = Mathf.Clamp01(maxAngleOfAttack - Mathf.Abs(angleOfAttack) / maxAngleOfAttack - criticalAoA);
        float dampedElevator = input.x * AoADampFactor;
        controlInput = new Vector3(dampedElevator, input.y, input.z);
        calculateControlForces(controlInput.x, controlInput.y, -controlInput.z);
    }

    void CalculateState()
    {
        var invRotation = Quaternion.Inverse(rb.rotation);
        localVelocity = invRotation * rb.linearVelocity;  //transform world velocity into local space
        localAngularVelocity = invRotation * rb.angularVelocity;  //transform into local space
        rateOfClimb = rb.linearVelocity.y;
        machSpeed = currentSpeed / 1234f;
        CalculateGForce();
        angleOfAttack = Mathf.Atan2(-localVelocity.y, localVelocity.z) * Mathf.Rad2Deg;
    }

    Vector3 lastVelocity;
    void CalculateGForce()
    {
        var invRotation = Quaternion.Inverse(rb.rotation);
        var acceleration = (rb.linearVelocity - lastVelocity) / Time.fixedDeltaTime;
        LocalGForce = invRotation * acceleration;
        lastVelocity = rb.linearVelocity;
        gForce = LocalGForce.y / 9.81f;
    }
    public void UpdateAspectRatio(float newWingspan)
    {
        wingSpan = newWingspan;
        aspectRatio = (newWingspan * newWingspan) / wingArea;
        CalculateMaxAoA();
    }



    public bool brakeInput;
    void CheckFlapsAndBrakes()
    {
        if(brakeInput)
        {
            flaps = true;
            airbrake = true;
            return;
        }
        else if(currentSpeed < stallSpeed * 2f)
        {
            flaps = true;
            airbrake = false;
            return;
        }
        flaps = false;
        airbrake = false;
    }
}
