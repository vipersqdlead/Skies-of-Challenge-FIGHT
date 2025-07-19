using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SurvivalSettings;

public class SurvivalMissionStatus : MonoBehaviour
{
    public GameObject BattleUI, MissionSuccess, Retry, GameOverCamera, MissionStart, PauseUI;
    public KillCounter KillCounter;
    public int points, kills, totalRoundsFired, autofireRoundsFired, manualRoundsFired, roundsHit, highestCombo, totalBattleTime, timeToFirstHighGTurn;
    public GameObject Player;
    public string aircraftName;
    [SerializeField] AircraftHub playerAcHub;
    public float MissionTimer;
    public bool missionEnd = false;
    public Camera overCam, extCam;
    [SerializeField] float timerToMenu;
    public Image BlackBG;
    bool reloadingMission, returningToMenu;
    public AudioSource bgm;
    public Slider bgmVolume;
    public DeathCamera deathCam;
    public bool isPaused = false;
    public int currentWave = 1;
    public WaveSpawner waveSpawner;
    public EnemyMarkers markers;
    public AudioListener camListener;

    [SerializeField] GameObject currentLockedTarget;
    public TMP_Text KillCountUI, PointCount, TimeLeft, newWaveText, mapBoundaryWarning, mEnd_TimeBonus, mEnd_PointScore, mEnd_FinalScore;
	public Toggle inverseAxis;
	public Slider sensitivityS;
    public AudioSource mapBoundaryWarningAS;
    public AudioClip mapBoundaryWarningLight, mapBoundaryWarningStrong;
	public SimpleAnalytics analytics;

    void Start()
    {
        Fade(true);
        playerAcHub = Player.GetComponent<AircraftHub>();
        MissionStart.GetComponent<AudioSource>().Play();
		bgmVolume.value = PlayerPrefs.GetFloat("Volume", 0.7f);
        waveSpawner.PropSpawnWave(3);
        CheckForRemainingFighters();
		int _inverse = PlayerPrefs.GetInt("InverseY");
		if(_inverse == 1)
		{
			inverseAxis.isOn = true;
		}
		else
		{
			inverseAxis.isOn = false;
		}
		sensitivityS.value = PlayerPrefs.GetFloat("Sensitivity", 0.7f);
		playerAcHub.playerInputs.inverseYAxis = inverseAxis.isOn;
		playerAcHub.playerInputs.tiltSensitivity = Mathf.Lerp(10f, 40f, sensitivityS.value);
		analytics = GetComponent<SimpleAnalytics>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!missionEnd)
        {
            Fade(true);
            MissionTimer += Time.deltaTime;
			if(timeToFirstHighGTurn == 0f)
			{
				if(playerAcHub.fm.gForce > 4.5f)
				{
					timeToFirstHighGTurn = (int)MissionTimer;
				}
			}
            UpdateUI();
            //Pause();
            bgm.volume = bgmVolume.value;
        }
		


        if (reloadingMission)
        {
            MissionRetry();
        }
        if (returningToMenu)
        {
            ReturnToMenu();
        }

        if (MissionTimer > 5f)
        {
            MissionStart.SetActive(false);
        }

        if(Player != null)
        {
			DataCollection();
        }
        if (Player == null)
        {
            MissionEnd();
        }

        CheckForRemainingFighters();

        if (SetRetry == true)
        {
            MissionRetry();
        }
    }
	
	void DataCollection()
	{
		points = KillCounter.Points;
        kills = KillCounter.Kills;
		totalRoundsFired = playerAcHub.gunsControl.mainGunroundsFired;
		autofireRoundsFired = playerAcHub.gunsControl.autoRoundsFired;
		manualRoundsFired = playerAcHub.gunsControl.manualRoundsFired;
		roundsHit = KillCounter.hits;
		totalBattleTime = (int)MissionTimer;
		if(KillCounter.currentCombo > highestCombo) { highestCombo = KillCounter.currentCombo; }
	}

    void UpdateUI()
    {
        if (playerAcHub.planeCam.camShaking == true)
        {
            TimeLeft.color = Color.red;
            KillCountUI.color = Color.red;
            PointCount.color = Color.red;
        }
        else
        {
            TimeLeft.color = Color.white;
            KillCountUI.color = Color.white;
            PointCount.color = Color.white;
        }
        TimeLeft.text = "Time: " + (int)MissionTimer;
        KillCountUI.text = "Destroyed: " + KillCounter.Kills;
        PointCount.text = "Points: " + KillCounter.Points;
        BlackBG.fillClockwise = true;

        if (currentLockedTarget != playerAcHub.planeCam.camLockedTarget)
        {
            if(playerAcHub.planeCam.camLockedTarget.GetComponent<AircraftHub>().meshRenderer != null)
            {
                markers.targetLocked = playerAcHub.planeCam.camLockedTarget.GetComponent<AircraftHub>().meshRenderer;
            }
            else
            {
                markers.targetLocked = playerAcHub.planeCam.camLockedTarget.GetComponentInChildren<MeshRenderer>();
            }
            currentLockedTarget = playerAcHub.planeCam.camLockedTarget;
        }
        MapBoundaries();
		
		if(startingHercwave)
		{	
			float alpha = Mathf.Abs(Mathf.Sin(Time.time * 3f));
			var Color = newWaveText.color;
			Color.a = alpha;
			newWaveText.color = Color;
		}

    }

    IEnumerator StartNewWave()
    {
        currentWave++;

        waveSpawner.PropSpawnWave(1);

        yield return new WaitForSeconds(5f);
        startingwave = false;
        yield return null;
    }
	
	IEnumerator StartHerculesWave()
    {
        print("Starting Herc wave");
        currentWave++;
		
		newWaveText.gameObject.SetActive(true);

        waveSpawner.HercSpawnWave();

        yield return new WaitForSeconds(6f);
        newWaveText.gameObject.SetActive(false);
		yield return new WaitForSeconds(45f);
        startingHercwave = false;
        yield return null;
    }

    void MapBoundaries()
    {
        float giveWarningDistance = 5000f;
        float destroyDistance = 6000f;

        if(playerAcHub == null)
        {
            mapBoundaryWarning.enabled = false;
            mapBoundaryWarningAS.enabled = false;
            return;
        }

        if (playerAcHub.transform.position.x < -destroyDistance || playerAcHub.transform.position.x > destroyDistance || playerAcHub.transform.position.z < -destroyDistance || playerAcHub.transform.position.z > destroyDistance)
        {
            mapBoundaryWarningAS.enabled = true;
            mapBoundaryWarning.color = Color.red;
            playerAcHub.hp.DealExternalDamagePerSecond();
            mapBoundaryWarningAS.clip = mapBoundaryWarningStrong;
            if (mapBoundaryWarningAS.isPlaying == false)
            {
                mapBoundaryWarningAS.Play();
            }
        }
        else if (playerAcHub.transform.position.x < -giveWarningDistance || playerAcHub.transform.position.x > giveWarningDistance || playerAcHub.transform.position.z < -giveWarningDistance || playerAcHub.transform.position.z > giveWarningDistance)
        {
            mapBoundaryWarning.enabled = true;
            mapBoundaryWarning.color = Color.white;
            mapBoundaryWarningAS.enabled = true;
            mapBoundaryWarningAS.clip = mapBoundaryWarningLight;
            if(mapBoundaryWarningAS.isPlaying == false)
            {
                mapBoundaryWarningAS.Play();
            }
        }
        else
        {
            mapBoundaryWarning.enabled = false;
            mapBoundaryWarningAS.enabled = false;
        }

    }

    bool SetRetry = false;
    void MissionEnd()
    {
        if(bgm != null)
        {
            bgm.enabled = false; bgm = null;
        }
        BattleUI.SetActive(false);
        markers.gameObject.SetActive(false);
        MissionSuccess.SetActive(true);
        KillCounter.Points = 0;
        missionEnd = true;
        if (SetRetry == false)
        {
            timerToMenu += Time.deltaTime;
            if (timerToMenu > 1f)
            {
                CalculateFinalScore();
                Retry.SetActive(true);
                if (Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetAxis("FireCannon") != 0)
                {
                    SetRetry = true;
                    Retry.SetActive(false);
                    timerToMenu = 0f;
                }
            }
            if (timerToMenu >= 8f)
            {
                Retry.SetActive(false);
                Fade(false);
            }
            if (timerToMenu >= 10f)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(0);
            }
        }
    }

    void MissionRetry()
    {
        Fade(false);
        timerToMenu += Time.unscaledDeltaTime;
        if (timerToMenu >= 2f)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(1);
        }
    }

    int timeBonus;
    int finalScore;
    bool finalScoreCalculated;
    void CalculateFinalScore()
    {
        if (!finalScoreCalculated)
        {
            timeBonus = (int)MissionTimer * 10;
            mEnd_TimeBonus.text = "Time bonus: " + (int)MissionTimer + "s = " + timeBonus + " pts.";
            mEnd_PointScore.text = "Kills: " + points + " pts.";
            finalScore = points + timeBonus;
            mEnd_FinalScore.text = "Final Score: " + finalScore + " pts.";
            print(finalScore);
            SaveScore();
            finalScoreCalculated = true;
        }
    }

    void SaveScore()
    {
        int highestScore = PlayerPrefs.GetInt("Survival High Score");
        if(finalScore >  highestScore)
        {
            PlayerPrefs.SetInt("Survival High Score", finalScore);
        }

        int highestKills = PlayerPrefs.GetInt("Survival Highest Kills");
        if (kills > highestKills)
        {
            PlayerPrefs.SetInt("Survival Highest Kills", kills);
        }

        int highestRound = PlayerPrefs.GetInt("Survival Highest Round");
        if(currentWave > highestRound)
        {
            PlayerPrefs.SetInt("Survival Highest Round", currentWave);
        }

        int longestAlive = PlayerPrefs.GetInt("Survival Longest Alive");
        if((int)MissionTimer > longestAlive)
        {
            PlayerPrefs.SetInt("Survival Longest Alive", (int)MissionTimer);
        }
        PlayerPrefs.SetInt("Survival Mission Score", finalScore);
        RegisterKillStats();
        PlayerPrefs.Save();
    }

    void Fade(bool fadeInOrOut)
    {
        if (fadeInOrOut)
        {
            BlackBG.fillOrigin = 1;
            BlackBG.fillAmount -= Time.deltaTime * 2f;

        }

        if (!fadeInOrOut)
        {
            BlackBG.fillOrigin = 2;
            BlackBG.fillAmount += Time.deltaTime * 2f;
        }
    }

    bool startingwave = false;
	bool startingHercwave = false;
    public List<FlightModel> enemyFighters;
	public List<FlightModel> hercules;
    public int targetEnemyQuantity;
    void CheckForRemainingFighters()
    {
        for (int i = 0; i < enemyFighters.Count; i++)
        {
            if(enemyFighters[i] == null)
            {
                enemyFighters.RemoveAt(i);
                return;
            }
        }

        targetEnemyQuantity = (int)GetTargetEnemies(Time.timeSinceLevelLoad);

        if(enemyFighters.Count < targetEnemyQuantity)
        {
            if(!startingwave)
            {
                StartCoroutine("StartNewWave");
                startingwave = true;
            }
        }
		
		for (int i = 0; i < hercules.Count; i++)
        {
            if(hercules[i] == null)
            {
                hercules.RemoveAt(i);
                return;
            }
        }
		
		float targetHercsQuantity = (int)GetTargetHercs(Time.timeSinceLevelLoad);
		
		if(hercules.Count < targetHercsQuantity)
        {
            if(!startingHercwave)
            {
				StartCoroutine("StartHerculesWave");
                startingHercwave = true;
            }
        }

    }

    float GetTargetEnemies(float timeElapsed)
    {
        float baseCount = 3f;
        float growthRate = 0.03f;
        float maxEnemies = 10f;

        float target = baseCount + timeElapsed * growthRate;
        return Mathf.Min(target, maxEnemies);
    }
	
	float GetTargetHercs(float timeElapsed)
	{
		float baseCount = 0f;
        float growthRate = 0.0175f;
        float maxEnemies = 1f;

        float target = baseCount + timeElapsed * growthRate;
        return Mathf.Min(target, maxEnemies);
	}

    public void Pause()
    {
        print("Trying to pause");
        if (camListener != null)
        {
            camListener.enabled = !isPaused;
        }
		
		playerAcHub.playerInputs.inverseYAxis = inverseAxis.isOn;
		
		playerAcHub.playerInputs.tiltSensitivity = Mathf.Lerp(10f, 40f, sensitivityS.value);
		
            if (!isPaused)
            {
                Time.timeScale = 0f;
                BattleUI.SetActive(false);
                isPaused = true;
            }
            else if (isPaused)
            {
                Time.timeScale = 1f;
                BattleUI.SetActive(true);
                bgm.UnPause();
                isPaused = false;
            }
        PauseUI.SetActive(isPaused);
        bgm.gameObject.SetActive(!isPaused);
    }

    public void UnPause()
    {
        isPaused = false;
        Time.timeScale = 1f;
        bgm.UnPause();
    }

    public void buttonReturnToMenu()
    {
        UnPause();
        missionEnd = true;
        returningToMenu = true;
    }

    public void buttonRetrying()
    {
        print("Retrying");
        UnPause();
        missionEnd = true;
        reloadingMission = true;
    }

    void ReturnToMenu()
    {
        Fade(false);
        timerToMenu += Time.unscaledDeltaTime;
        if (timerToMenu >= 2f)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }

    void RegisterKillStats()
    {
        PlayerPrefs.SetInt(aircraftName + " Total Kill Count", PlayerPrefs.GetInt(aircraftName + " Highest Kill Count") + kills);
        if(kills > PlayerPrefs.GetInt(aircraftName + " Highest Kill Count"))
        {
            PlayerPrefs.SetInt(aircraftName + " Highest Kill Count", kills);
        }

        PlayerPrefs.SetInt(aircraftName + " Total Time Alive", PlayerPrefs.GetInt(aircraftName + " Total Time Alive") + (int)MissionTimer);
        if (MissionTimer > PlayerPrefs.GetInt(aircraftName + " Highest Time Alive"))
        {
            PlayerPrefs.SetInt(aircraftName + " Highest Time Alive", (int)MissionTimer);
        }

        PlayerPrefs.SetInt(aircraftName + " Total Score", PlayerPrefs.GetInt(aircraftName + " Total Score") + finalScore);
        if (finalScore > PlayerPrefs.GetInt(aircraftName + " Highest Score"))
        {
            PlayerPrefs.SetInt(aircraftName + " Highest Score", finalScore);
        }

        PlayerPrefs.SetInt("General Total Score", PlayerPrefs.GetInt("General Total Score") + finalScore);
        PlayerPrefs.SetInt("General Total Kills", PlayerPrefs.GetInt("General Total Kills") + kills);
        PlayerPrefs.SetInt("General Total Fly Time", PlayerPrefs.GetInt("General Total Fly Time") + (int)MissionTimer);
    }
	
	void OnDisable()
	{
		PlayerPrefs.SetFloat("Sensitivity", sensitivityS.value);
		if(inverseAxis.isOn == true)
		{
			PlayerPrefs.SetInt("InverseY", 1);
		}
		else
		{
			PlayerPrefs.SetInt("InverseY", 1);
		}
		PlayerPrefs.SetFloat("Volume", bgmVolume.value);
		analytics.SendDataCollectionEvent(totalRoundsFired, autofireRoundsFired, manualRoundsFired, roundsHit, kills, totalBattleTime, timeToFirstHighGTurn, highestCombo);
	}
}
