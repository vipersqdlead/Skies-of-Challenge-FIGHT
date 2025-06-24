using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public FlightModel player;
    [SerializeField] List<GameObject> propWavePrefabs;
	public GameObject herculesPrefab;
    [SerializeField] List<Transform> SpawnPositions;
    public EnemyMarkers markers;
    public SurvivalMissionStatus status;

    public void PropSpawnWave(int numberOfEnemies)
    {
        List<Transform> auxSpawnPositions = SpawnPositions;

        for (int i = 0; i < numberOfEnemies; i++)
        {
            int spawnRand = Random.Range(0, auxSpawnPositions.Count);
            GameObject newWave = Instantiate(propWavePrefabs[Random.Range(0, propWavePrefabs.Count)], auxSpawnPositions[spawnRand].position, auxSpawnPositions[spawnRand].rotation);
            //auxSpawnPositions.Remove(auxSpawnPositions[spawnRand]);
            newWave.GetComponent<Wave>().AddRenderersToMarker(markers, status, player);
        }
    }
	
	public void HercSpawnWave()
    {
        List<Transform> auxSpawnPositions = SpawnPositions;
		
		int spawnRand = Random.Range(0, auxSpawnPositions.Count);
        AircraftHub herc = Instantiate(herculesPrefab, auxSpawnPositions[spawnRand].position, auxSpawnPositions[spawnRand].rotation).GetComponent<AircraftHub>();
        //auxSpawnPositions.Remove(auxSpawnPositions[spawnRand]);
        	markers.AddMarker(herc);
			herc.fm.target = player;
			status.hercules.Add(herc.fm);
    }

    public void PropAlliedSpawnWave()
    {
        GameObject newWave = Instantiate(propWavePrefabs[Random.Range(0, propWavePrefabs.Count)], new Vector3(0, 4000f, 0), transform.rotation);
        //auxSpawnPositions.Remove(auxSpawnPositions[spawnRand]);
        newWave.GetComponent<Wave>().AddAllyRenderersToMarker(markers, status);
    }
}
