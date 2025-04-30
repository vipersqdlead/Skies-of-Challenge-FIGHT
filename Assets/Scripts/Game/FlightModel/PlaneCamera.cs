using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;

public class PlaneCamera : MonoBehaviour
{
    [SerializeField] public new Camera camera;
    [SerializeField] Vector3 cameraOffset;
    [SerializeField] Vector2 lookAngle;
    [SerializeField] float movementScale;
    [SerializeField] float lookAlpha;
    [SerializeField] float movementAlpha;
    [SerializeField] Vector3 deathOffset;
    [SerializeField] float deathSensitivity;
    [SerializeField] PlayerInputs inputs;
    [SerializeField] AircraftHub hub;
    HealthPoints hp;

    public Transform cameraTransform;
    FlightModel plane;
    Transform planeTransform;
    Vector2 lookInput;
    bool dead;

    Vector2 look;
    Vector2 lookAverage;
    Vector3 avAverage;

    public GameObject camLockedTarget;



    public enum CameraMode { FreeLook, Tracking }
    private CameraMode currentMode = CameraMode.Tracking;
    private Quaternion targetRotation;
    float camTransitionSpeed = 20f; // Adjust this for smoother transitions

    void Awake()
    {
        hub = GetComponent<AircraftHub>();
        cameraTransform = camera.GetComponent<Transform>();
        plane = GetComponent<FlightModel>();
        inputs = GetComponent<PlayerInputs>();
        hp = plane.health;
        cameraParent = camera.transform.parent.gameObject;
    }

    void LateUpdate()
    {
        lookInput.x = hub.fm.controlInput.z + hub.fm.controlInput.y;
        lookInput.y = hub.fm.controlInput.x;

        var cameraOffset = this.cameraOffset;

        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView, CalculateFoV(), Time.deltaTime * 10f);

        HandleCameraMode();

        cameraParent.transform.localRotation = Quaternion.Slerp(
            cameraParent.transform.localRotation,
            targetRotation,
            Time.fixedDeltaTime * camTransitionSpeed
        );

        lookAngle = Vector2.one;
        {
            var targetLookAngle = Vector2.Scale(lookInput, lookAngle);
            lookAverage = (lookAverage * (1 - lookAlpha)) + (targetLookAngle * lookAlpha);

            var angularVelocity = plane.localAngularVelocity;
            angularVelocity.z = -angularVelocity.z;
            avAverage = (avAverage * (1 - movementAlpha)) + (angularVelocity * movementAlpha);
        }

        var rotation = Quaternion.Euler(-lookAverage.y, lookAverage.x, 0);  //get rotation from camera input
        var turningRotation = Quaternion.Euler(new Vector3(-avAverage.x, -avAverage.y, avAverage.z) * movementScale);   //get rotation from plane's AV

        cameraTransform.localPosition = rotation * turningRotation * cameraOffset;  //calculate camera position;
        cameraTransform.localRotation = rotation * turningRotation;                 //calculate camera rotation

        CamShake();
    }


    public GameObject cameraParent;

    float sensitivity = 0.10f;
    Vector3 currentInputVector;
    Vector2 smoothVelocity;
    void DampInputs()
    {
        Vector2 inputs = new Vector2(Input.GetAxis("HorizontalCameraRotation"), Input.GetAxis("VerticalCameraRotation"));
        currentInputVector = Vector2.SmoothDamp(currentInputVector, inputs, ref smoothVelocity, sensitivity);
    }

    float lastHP;
    void CamShake()
    {
        if (hp.HP != lastHP)
        {
            IEnumerator coroutine = Shake(0.3f, 0.2f);
            StartCoroutine(coroutine);
            lastHP = hp.HP;
        }
    }
    [HideInInspector] public bool camShaking;
    IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPosition = Vector3.zero;
        float elapsedTime = 0.0f;
        while (elapsedTime < duration)
        {
            camShaking = true;
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            cameraParent.transform.localPosition = originalPosition + new Vector3(x, y, 0);
            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }
        cameraParent.transform.localPosition = originalPosition;
        camShaking = false;
        yield return null;
    }

    void HandleCameraMode()
    {
        if (Input.GetKey(KeyCode.C)) // Replace with your actual input check
        {
            currentMode = CameraMode.FreeLook;
        }
        else
        {
            currentMode = CameraMode.Tracking;  
        }
        
        UpdateCameraTargetRotation();
    }

    void UpdateCameraTargetRotation()
    {
        switch (currentMode)
        {
            case CameraMode.FreeLook:
                DampInputs();
                targetRotation = Quaternion.Euler(new Vector3(currentInputVector.y * 90f, currentInputVector.x * 180f, 0));
                break;

            case CameraMode.Tracking:

                    // Get world-space LookAt rotation
                    Quaternion worldLookRotation = Quaternion.LookRotation(hub.playerInputs.targetCursorTransform.position - cameraParent.transform.position);

                    // Convert to local space relative to the player's aircraft
                    targetRotation = Quaternion.Inverse(hub.transform.rotation) * worldLookRotation;
                
                break;
        }

    }

    float CalculateFoV()
    {
        float baseFoV = 50f;
        float maxFoV = 85f;

        float speedPercent = 0f;
        speedPercent = (hub.fm.currentSpeed * 100 / hub.fm.neverExceedSpeed) / 100f;

        float currentFoV = Mathf.Lerp(baseFoV, maxFoV, speedPercent);
        return currentFoV;
    }
}
