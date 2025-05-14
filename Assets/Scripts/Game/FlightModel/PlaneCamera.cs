using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.VisualScripting;
using UnityEngine;

public class PlaneCamera : MonoBehaviour
{
    [SerializeField] public Camera cam;
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
    bool dead;

    Vector2 look, lookInput;
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
        cameraTransform = cam.GetComponent<Transform>();
        plane = GetComponent<FlightModel>();
        inputs = GetComponent<PlayerInputs>();
        hp = plane.health;
        cameraParent = cam.transform.parent.gameObject;
    }

    void LateUpdate()
    {
        var cameraOffset = this.cameraOffset;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, CalculateFoV(), Time.deltaTime * 10f);

        UpdateCameraTargetRotation();

        //cameraParent.transform.rotation = targetRotation;

        cameraParent.transform.localRotation = Quaternion.Lerp(cameraParent.transform.localRotation, targetRotation, 1f);

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
    void UpdateCameraTargetRotation()
    {
                    // Get world-space LookAt rotation
                    Quaternion worldLookRotation = Quaternion.LookRotation(hub.playerInputs.targetCursorTransform.position - cameraParent.transform.position);

                    // Convert to local space relative to the player's aircraft
                    targetRotation = Quaternion.Inverse(hub.transform.rotation) * worldLookRotation;
        

    }

    float CalculateFoV()
    {
        float baseFoV = 50f;
        float maxFoV = 80f;

        float speedPercent = 0f;
        speedPercent = (hub.fm.currentSpeed / hub.fm.neverExceedSpeed) / 100f;

        float currentFoV = Mathf.Lerp(baseFoV, maxFoV, speedPercent);
        return currentFoV;
    }
}
