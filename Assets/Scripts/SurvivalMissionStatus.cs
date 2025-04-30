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
    public int points;
    int kills;
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
    public TMP_Text KillCountUI, PointCount, TimeLeft, newWaveText, enemiesLeftText, mapBoundaryWarning, mEnd_TimeBonus, mEnd_PointScore, mEnd_FinalScore;
    public AudioSource mapBoundaryWarningAS;
    public AudioClip mapBoundaryWarningLight, mapBoundaryWarningStrong;

    void Start()
    {
        Fade(true);
        playerAcHub = Player.GetComponent<AircraftHub>();
        MissionStart.GetComponent<AudioSource>().Play(); 
        bgmVolume.value = bgm.volume;
    }

    // Update is called once per frame
    void Update()
    {
        if (!missionEnd)
        {
            Fade(true);
            MissionTimer += Time.deltaTime;
            UpdateUI();
            Pause();
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
            points = KillCounter.Points;
            kills = KillCounter.Kills;
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

    void UpdateUI()
    {
        if (playerAcHub.planeCam.camShaking == true)
        {
            TimeLeft.color = Color.red;
            KillCountUI.color = Color.red;
            PointCount.color = Color.red;
            enemiesLeftText.color = Color.red;
        }
        else
        {
            TimeLeft.color = Color.white;
            KillCountUI.color = Color.white;
            PointCount.color = Color.white;
            enemiesLeftText.color = Color.red;
        }
        TimeLeft.text = "Time: " + (int)MissionTimer;
        KillCountUI.text = "Destroyed: " + KillCounter.Kills;
        PointCount.text = "Points: " + KillCounter.Points;
        enemiesLeftText.text = enemyFighters.Count + " Enemies Left";
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

    }

    IEnumerator StartNewWave()
    {
        print("Starting new wave");
        currentWave++;
        newWaveText.enabled = true;
        newWaveText.text = "Wave " + currentWave + " Inbound!";
        newWaveText.gameObject.GetComponent<AudioSource>().PlayOneShot(newWaveText.gameObject.GetComponent<AudioSource>().clip);

        {
            if (currentWave <= 2)
            {
                waveSpawner.PropSpawnWave(1);
            }
            else if (currentWave > 2 && currentWave < 5)
            {
                waveSpawner.PropSpawnWave(2);
            }
            else if (currentWave > 5 && currentWave <= 8)
            {
                waveSpawner.PropSpawnWave(3);
            }
            else if (currentWave > 8 && currentWave < 10)
            {
                waveSpawner.PropSpawnWave(4);
            }
            else
            {
                waveSpawner.PropSpawnWave(6);
            }
        }

        yield return new WaitForSeconds(5f);
        newWaveText.enabled = false;
        startingwave = false;
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
                SceneManager.LoadScene("SurvivalMenu");
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
            SceneManager.LoadScene("SurvivalMission");
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
    public List<FlightModel> enemyFighters;
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

        if(enemyFighters.Count == 0)
        {
            if(!startingwave)
            {
                StartCoroutine("StartNewWave");
                startingwave = true;
            }
        }
    }

    void Pause()
    {
        PauseUI.SetActive(isPaused);
        bgm.gameObject.SetActive(!isPaused);
        if (camListener != null)
        {
            camListener.enabled = !isPaused;
        }
        if (Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.Escape))
        {
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
        }

        if (isPaused)
        {
            if (Input.GetKeyDown(KeyCode.JoystickButton3))
            {
                buttonRetrying();
            }

            if (Input.GetKeyDown(KeyCode.Joystick1Button4))
            {
                buttonReturnToMenu();
            }
        }
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
}
