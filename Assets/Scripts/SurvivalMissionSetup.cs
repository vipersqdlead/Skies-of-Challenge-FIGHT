using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SurvivalMissionSetup : MonoBehaviour
{
    [Header("Mission Settings")]
    [SerializeField] GameObject mapPrefab;
    [SerializeField] GameObject[] playerAircraftPrefabs;
    [SerializeField] GameObject player, AimCursor;
    AircraftHub hub;

    [SerializeField] SurvivalMissionStatus status;
    [SerializeField] WaveSpawner waveSpawner;

    private void Awake()
    {
        player = Instantiate(playerAircraftPrefabs[PlayerPrefs.GetInt("Survival Aircraft")], new Vector3(transform.position.x, 4000f, transform.position.z), transform.rotation);
        Instantiate(mapPrefab);
        status.Player = player;
        hub = player.GetComponent<AircraftHub>();
        hub.playerInputs.targetCursorTransform = AimCursor.transform;
        status.KillCounter = hub.killcounter;
        status.deathCam.Player = player;
        status.waveSpawner = waveSpawner;
        status.camListener = hub.planeCam.cameraTransform.GetComponent<AudioListener>();
        waveSpawner.player = hub.fm;
        UISetup();
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Header("UI Settings")]
    public TMP_Text UI_speedLabel; // The label that displays the speed;
    public RectTransform UI_speedArrow; // The arrow in the speedometer
    public RectTransform UI_smallAltArrow;
    public RectTransform UI_altBigArrow;

    [SerializeField] TMP_Text UI_HP, UI_Combo;
    [SerializeField] GameObject UI_Crosshair, UI_LeadMarker, UI_FPM, UI_AimCursor, UI_Center;
    [SerializeField] AudioSource UI_stallWarning_SFX;
    [SerializeField] RawImage UI_healthIcon;
    [SerializeField] EnemyMarkers markers;
    [SerializeField] RadarMinimap minimap;
    void UISetup()
    {
        AircraftHub hub = player.GetComponent<AircraftHub>();
        hub.planeToUI.speedLabel = UI_speedLabel;
        hub.planeToUI.speedArrow = UI_speedArrow;
        hub.planeToUI.stallAlarm = UI_stallWarning_SFX;
        hub.planeToUI.smallAltArrow = UI_smallAltArrow;
        hub.planeToUI.altBigArrow = UI_altBigArrow;
        hub.planeToUI.Health = UI_HP;
        hub.planeToUI.healthIcon = UI_healthIcon;
        hub.planeToUI.killsCombo = UI_Combo;


        hub.planeToUI.velocityMarker = UI_FPM.transform;
        hub.planeToUI.crosshair = UI_Crosshair.transform;
        hub.planeToUI.AimCursor = UI_AimCursor.transform;

        waveSpawner.markers = markers;
        minimap.player = player.transform;
    }
}
