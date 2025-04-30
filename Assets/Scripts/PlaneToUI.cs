using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class PlaneToUI : MonoBehaviour
{
    public GameObject UI;
    Vector3 screenCenter;

    [Header("General Settings")]
    [SerializeField] AircraftHub hub;

    [SerializeField] Camera cam;
    [SerializeField] Rigidbody planeRb;
    [SerializeField] FlightModel playerControls;
    [SerializeField] EngineControl engineControl;
    [SerializeField] Transform planeTransform;

    [Header("Speedometer")]
    public float maxSpeed = 900f; // The maximum speed of the target ** IN KM/H **
    float minSpeedArrowAngle = 125;
    float maxSpeedArrowAngle = -125;
    public TMP_Text speedLabel; // The label that displays the speed;
    public RectTransform speedArrow; // The arrow in the speedometer
    private float speed = 0.0f;
    public AudioSource stallAlarm;

    [Header("Altitude")]
    public RectTransform smallAltArrow;
    public RectTransform altBigArrow;

    [Header("Velocity")]
    public Transform velocityMarker;
    public Transform crosshair;
    public Transform AimCursor;

    [Header("Health")]
    public RawImage healthIcon;
    public TMP_Text Health, killsCombo;


    // Start is called before the first frame update
    void Awake()
    {
        hub = GetComponent<AircraftHub>();
        cam = hub.planeCam.camera;
        engineControl = hub.engineControl;
        screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
    }

    // Update is called once per frame
    void Update()
    {
        ShowDials();
        ShowHealth();
        CountKillsCombo();
        UpdateVelocityMarker();
        UpdateCrosshair();
        AimCursorUI();
    }

    void ShowHealth()
    {
        if(hub.hp.extraLives == 0)
        {
            Health.text = (int)hub.hp.hpPercent + "%";
        }
        else
        {
            Health.text = (int)hub.hp.hpPercent + "% (" + (int)hub.hp.extraLives + ")";
        }

        if(hub.hp.invulnerable == true)
        {
            Health.color = Color.green;
        }
        else
        {
            Color newColor = Color.Lerp(Color.red, Color.white, (Mathf.Clamp(hub.hp.hpPercent, 0, 100)) / 100f);
            healthIcon.color = newColor;
            if (hub.hp.hpPercent >= 40 && hub.planeCam.camShaking == false)
            {
                Health.color = Color.white;
            }
            if (hub.hp.hpPercent < 40 && hub.planeCam.camShaking == false)
            {
                Health.color = Color.yellow;
            }
            if (hub.hp.hpPercent < 25 || hub.planeCam.camShaking == true)
            {
                Health.color = Color.red;
            }
        }
    }

    void ShowSpeed()
    {
        // 3.6f to convert in kilometers
        // ** The speed must be clamped by the car controller **
        speed = hub.fm.currentSpeed;

        if (speedLabel != null)
        {
            if (playerControls.currentSpeed < playerControls.stallSpeed + 30f || playerControls.stalling == true|| hub.planeCam.camShaking == true || playerControls.currentSpeed > playerControls.neverExceedSpeed - 60f)
            {
                speedLabel.color = Color.red;
            }
            if (playerControls.stalling)
            {
                stallAlarm.enabled = true;
            }
            else
            {
                speedLabel.color = Color.white;
                stallAlarm.enabled = false;
            }
            speedLabel.text = ((int)speed).ToString();

            // stallAlarm.enabled = (-playerControls.maxAngleOfAttack / 2 - 1f) < playerControls.angleOfAttack && playerControls.angleOfAttack < playerControls.maxAngleOfAttack + 3f;

        }
        if (speedArrow != null)
            speedArrow.localEulerAngles =
                new Vector3(0, 0, Mathf.Lerp(minSpeedArrowAngle, maxSpeedArrowAngle, speed / maxSpeed));
    }

    void ShowAltitude()
    {
        float alt = hub.rb.position.y / 1000;
        float hundredMeters = hub.rb.position.y % 1000f;

        // Calculate arrow rotation (360 degrees for full rotation)
        // For the hundred-meter arrow: 100 meters -> 36 degrees (360 degrees / 10 increments)
        float hundredMeterRotation = (hundredMeters / 1000f) * 360f;

        // For the kilometer arrow: 1 kilometer -> 36 degrees (10 kilometers -> full rotation)
        float kilometerRotation = (alt / 10) * 360f;

        // Rotate the arrows
        smallAltArrow.localRotation = Quaternion.Euler(0, 0, -hundredMeterRotation);
        altBigArrow.localRotation = Quaternion.Euler(0, 0, -kilometerRotation);
    }

    void ShowDials()
    {
        ShowSpeed();
        ShowAltitude();
    }

    void CountKillsCombo()
    {
        if (hub.killcounter.comboCounting)
        {

            if(hub.killcounter.currentCombo == 1)
            {
                killsCombo.enabled = true;
                killsCombo.text = "Splash 1!";
            }
            else if(hub.killcounter.currentCombo  > 1)
            {
                killsCombo.text = "Splash " + hub.killcounter.currentCombo + "!";
            }
            if (hub.planeCam.camShaking || hub.killcounter.currentCombo > 1)
            {
                killsCombo.color = Color.red;
            }
            else if(hub.planeCam.camShaking == false)
            {
                killsCombo.color = Color.white;
            }
        }
        else
        {
            killsCombo.text = "Splash 1!";
            killsCombo.enabled = false;
        }
    }

    void UpdateVelocityMarker()
    {
        var velocity = planeTransform.forward;


        if (hub.rb.linearVelocity.sqrMagnitude > 1)
        {
            velocity = hub.rb.linearVelocity;
        }

        var screenSpace = cam.WorldToScreenPoint(cam.transform.position + velocity);
        var hudPos = screenSpace - new Vector3(cam.pixelWidth / 2, cam.pixelHeight / 2);

        velocityMarker.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
    }

    void UpdateCrosshair()
    {
        var forward = planeTransform.forward;

        var screenSpace = cam.WorldToScreenPoint(cam.transform.position + forward);
        var hudPos = screenSpace - new Vector3(cam.pixelWidth / 2, cam.pixelHeight / 2);

        crosshair.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
    }

    void AimCursorUI()
    {
        var aimCursorPoint = hub.playerInputs.targetCursorTransform.position;

        var screenSpace = cam.WorldToScreenPoint(aimCursorPoint);
        var hudPos = screenSpace - new Vector3(cam.pixelWidth / 2, cam.pixelHeight / 2);

        AimCursor.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
    }
}
