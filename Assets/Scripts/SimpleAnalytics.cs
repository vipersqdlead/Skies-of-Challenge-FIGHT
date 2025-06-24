using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class SimpleAnalytics : MonoBehaviour
{
	
	public class MiEvento : Unity.Services.Analytics.Event
	{
		public MiEvento() : base("mi_evento") { }
		
		public string accion { set { SetParameter("accion", value); } }
		public float tiempo { set { SetParameter("tiempo", value); } }
		public int nivel { set { SetParameter("nivel", value); } }
	}
	
	public class Event_DataRecollection : Unity.Services.Analytics.Event
	{
		public Event_DataRecollection() : base("Event_DataRecollection") { }
		
		public int AAtotalRoundsFired { set { SetParameter("AAtotalRoundsFired", value); } }
		public int AAautofireRoundsFired { set { SetParameter("AAautofireRoundsFired", value); } }
		public int AAmanuallyFiredRounds { set { SetParameter("AAmanuallyFiredRounds", value); } }
		public int AAroundsHit { set { SetParameter("AAroundsHit", value); } }
		public int AAenemiesKilled { set { SetParameter("AAenemiesKilled", value); } }
		public int AAtotalBattleTime { set { SetParameter("AAtotalBattleTime", value); } }
		public int AAtimeToFirstHighGTurn { set { SetParameter("AAtimeToFirstHighGTurn", value); } }
	}
	
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        try
		{
			await UnityServices.InitializeAsync();
			AnalyticsService.Instance.StartDataCollection();
			
			Debug.Log("Analytics initialized successfully.");		
		}
		
		catch (System.Exception e)
		{
			Debug.Log($"Analytics initialization error: {e.Message}");
		}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
	
	public void SendDataCollectionEvent(int totalRoundsFired, int autofireRoundsFired, int manualRoundsFired, int roundsHit, int enemiesKilled, int totalBattleTime, int timeToFirstHighGTurn)
	{
		try
		{
			var evento = new Event_DataRecollection
			{
				AAtotalRoundsFired = totalRoundsFired,
				AAautofireRoundsFired = autofireRoundsFired,
				AAmanuallyFiredRounds = manualRoundsFired,
				AAroundsHit = roundsHit,
				AAenemiesKilled = enemiesKilled,
				AAtotalBattleTime = totalBattleTime,
				AAtimeToFirstHighGTurn = timeToFirstHighGTurn
			};
			AnalyticsService.Instance.RecordEvent(evento);
			Debug.Log($"Event sent - Time: {Time.time:F1}s");
		}
		catch (System.Exception e)
		{
			Debug.Log($"Event send error: {e.Message}");
		}
	}
	
}
