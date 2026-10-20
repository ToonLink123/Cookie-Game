// using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;

public class CookieSpawner : MonoBehaviour
{
    [SerializeField]private GameObject Cookie;

    [SerializeField] private float minY = -3f;
    [SerializeField] private float maxY = 3f;

    [SerializeField] private float minspawnDelay = 3f;
    [SerializeField] private float maxspwanDelay = 6f;

    [SerializeField] private float spawnTimer = 0f;
    [SerializeField] private float timeNextSpawn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        timeNextSpawn = Random.Range(minspawnDelay, maxspwanDelay);
    }

    // Update is called once per frame
    void Update()
    {
      spawnTimer += Time.deltaTime;

      if( spawnTimer > timeNextSpawn)
        {
            Vector3 spawnPos = new Vector3(gameObject.transform.position.x, Random.Range(minY, maxY));
            Instantiate(Cookie, spawnPos, Quaternion.identity);

            spawnTimer = 0f;

            timeNextSpawn = Random.Range(minspawnDelay, maxspwanDelay);

        }

    }

    

}
