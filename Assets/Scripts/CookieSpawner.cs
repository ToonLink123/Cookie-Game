// using System.Numerics;
using UnityEngine;

public class CookieSpawner : MonoBehaviour
{
    [SerializeField]private GameObject [] Cookie;

    [SerializeField] private float minY = -3f;
    [SerializeField] private float maxY = 3f;

    [SerializeField] private Transform cookieSpawn;
    [SerializeField] private float minspawnDelay = 3f;
    [SerializeField] private float maxspwanDelay = 6f;

    private float nextSpawnTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetNextSpawnTime();
    }

    // Update is called once per frame
    void Update()
    {
        if(Time.time >= nextSpawnTime)
        {
            SpawnRandomPattern();
            SetNextSpawnTime();
        }
    }

    void SpawnRandomPattern()
    {
        if(cookieSpawn.Length == 0f) return;

        int randomIndex = Random.Range(0, cookieSpawn.Length);
        GameObject chosenPattern = cookieSpawn [randomIndex];

        float randomY = Random.Range (minY,maxY);
        Vector3 spawnPosition = new Vector3(spawnPosition.position.X, randomY, 0f)
    }

}
