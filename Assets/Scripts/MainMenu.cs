using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{

    public FadeInOut fadeinOut;
    public AudioSource BGM;
    bool loadMission;
    int scenetoLoad;

    [SerializeField] TMP_Text startText;
    [SerializeField] GameObject controlsButton;
	
	public Toggle inverseAxis;
	public Slider sensitivityS;
    public Slider bgmVolume;
	
	[SerializeField] float startAlpha;

    void Start()
    {
        fadeinOut.ActivateFadeIn = true;
    }

    float timerToLoad;
    void Update()
    {
		startAlpha = Mathf.Abs(Mathf.Sin(Time.time * 3f));
		
        if (loadMission == true)
        {
		    startAlpha = Mathf.Abs(Mathf.Sin(Time.time * 10f));
            BGM.volume -= Time.deltaTime;
            timerToLoad += Time.deltaTime;
            if(timerToLoad > 2f)
            {
                LoadScene(scenetoLoad);
            }
        }
		
		var Color = startText.color;
		Color.a = startAlpha;
		startText.color = Color;
    }
	
    public void LoadMissionButton(int sceneNo)
    {
        fadeinOut.ActivateFadeOut = true;
        loadMission = true;
        scenetoLoad = sceneNo;
    }

    public void LoadScene(int sceneNo)
    {
        SceneManager.LoadScene(sceneNo);
    }

    public void QuickPlay()
    {
        LoadMissionButton(1);
    }

    public void CloseGame()
    {
        fadeinOut.ActivateFadeOut = true;
        Invoke("Quit", 1.8f);
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
	}

    void Quit()
    {
        Application.Quit();
    }
}
