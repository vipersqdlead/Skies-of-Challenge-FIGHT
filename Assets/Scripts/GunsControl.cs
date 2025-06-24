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
	bool manualFire;

    public bool useConvergence;
    public Vector3 convergencePoint;
	
	public int mainGunroundsFired, manualRoundsFired, autoRoundsFired;
	
	float autofireTime, totalFireTime;
	
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
            if (Input.GetAxis("FireCannon") != 0 || Input.touchCount != 0 || AutoFire() == true)
            {
				manualFire = true;
                trigger = true;
            }
            else
            {
				manualFire = false;
                trigger = false;
            }
			
			GetTotalFiredBullets();
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
	
	void GetTotalFiredBullets()
	{
		if(AutoFire() == true)
		{
			autofireTime += Time.deltaTime;
		}
		if(trigger)
		{
			totalFireTime += Time.deltaTime;
		}
		
		int _tempShotsFiredAuto = 0;
		int _tempShotsFiredMan = 0;
		foreach (Gun gun in guns)
		{
			int rpm = (int)gun.rateOfFireRPM;
			float timeBetweenShots = 1 / (rpm / 60f);
			
			float notAutofireFiretime = totalFireTime - autofireTime;

			int _manualRoundsFired = (int)(notAutofireFiretime / timeBetweenShots);
			int _autoRoundsFired = (int)(autofireTime / timeBetweenShots);
			_tempShotsFiredAuto += _autoRoundsFired;
			_tempShotsFiredMan += _manualRoundsFired;
		}
		manualRoundsFired = _tempShotsFiredMan;
		autoRoundsFired = _tempShotsFiredAuto;
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
		int _tempShotsFiredMG = 0;
		foreach(Gun gun in guns)
		{
			if(gun == null)
					continue;
			//gun.baseVelocity = baseVelocity;
            gun.Fire();
			//mainGunroundsFired = gun.shotsFired - mainGunroundsFired;
			_tempShotsFiredMG += gun.shotsFired;
		}
		mainGunroundsFired = _tempShotsFiredMG;
    }

	bool AutoFire()
	{
		float thickness = 150f; //<-- Desired thickness here
		RaycastHit[] hits = Physics.SphereCastAll(transform.position, thickness, transform.forward, 1000f);
		foreach (var hit in hits)
		{
			if(hit.collider.CompareTag("Bullet") || hit.collider.gameObject == gameObject)
			{
				continue;
			}
					
			if (hit.collider.CompareTag("Fighter") || hit.collider.CompareTag("Bomber"))
			{
				float angleToPlayer = Vector3.Angle(transform.forward, hit.collider.gameObject.transform.position - transform.position);
				if (angleToPlayer < 20f)
				{
					Vector3 targetPosition = Utilities.FirstOrderIntercept(transform.position, aircraftHub.rb.linearVelocity, 600f, hit.collider.gameObject.transform.position, hit.collider.attachedRigidbody.linearVelocity);
					float angleToLead = Vector3.Angle(transform.forward, targetPosition - transform.position);
					if(angleToLead < 5f)
					{
						return true;
					}
				}
				else if (angleToPlayer > 20f)
				{
					continue;
				}
			}
		}
		return false;
	}
}
