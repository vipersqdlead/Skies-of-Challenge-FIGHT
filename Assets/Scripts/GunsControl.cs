using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GunsControl : MonoBehaviour
{
    [SerializeField] AircraftHub aircraftHub;

    public Gun[] guns;
    public bool isPlayer;
    public bool trigger;

    public bool useConvergence;
    public Vector3 convergencePoint;
    private void Awake()
    {
        if (aircraftHub != null)
        {
            aircraftHub = GetComponent<AircraftHub>();
        }
    }

    private void Update()
    {
        if (isPlayer)
        {
            if (Input.GetAxis("FireCannon") != 0 || Input.touchCount != 0)
            {
                trigger = true;
            }
            else
            {
                trigger = false;
            }
        }

        if(useConvergence)
        {
            ApplyConvergence();
        }

        if(trigger)
        {
            FireGuns();
        }
    }

    [SerializeField] FlightModel sightLockedTarget;
    void ApplyConvergence()
    {
        if (aircraftHub == null)
        {
            aircraftHub = GetComponent<AircraftHub>();
        }
        if (sightLockedTarget == null)
        {
            sightLockedTarget = aircraftHub.fm.target;
        }
        if (sightLockedTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, sightLockedTarget.transform.position);
            convergencePoint = transform.position + (transform.forward * distanceToTarget);
            foreach (Gun gun in guns)
            {
                gun.transform.LookAt(convergencePoint);
            }
        }
        else
        {
            convergencePoint = transform.position + (transform.forward * 600f);
            foreach (Gun gun in guns)
            {
                gun.transform.LookAt(convergencePoint);
            }
        }
    }

    void FireGuns()
    {
        for (int i = 0; i < guns.Length; i++)
        {
            guns[i].Fire();
        }
    }
}
