using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableItem
    {
        public GameObject prefab;
        public float spawnProbability = 0.5f;
        public int maxItemsOnMap = 10;
        public float minRespawnTime = 30f;
        public float maxRespawnTime = 60f;
    }

    [Header("Spawn Settings")]
    public SpawnableItem medkit;
    public SpawnableItem food;
    public LayerMask spawnCheckLayer; // Layer to check for collisions during spawning
    public float spawnCheckRadius = 1f; // Radius to check for collisions

    [Header("Spawn Area")]
    public Vector2 spawnAreaMin = new Vector2(-50f, -50f);
    public Vector2 spawnAreaMax = new Vector2(50f, 50f);
    public float spawnHeight = 1f; // Height above ground to spawn items

    private List<GameObject> activeMedkits = new List<GameObject>();
    private List<GameObject> activeFood = new List<GameObject>();
    private float nextMedkitSpawnTime;
    private float nextFoodSpawnTime;

    void Start()
    {
        // Initial spawn
        SpawnInitialItems();

        // Schedule first respawns
        nextMedkitSpawnTime = Time.time + Random.Range(medkit.minRespawnTime, medkit.maxRespawnTime);
        nextFoodSpawnTime = Time.time + Random.Range(food.minRespawnTime, food.maxRespawnTime);
    }

    void Update()
    {
        // Check for medkit respawn
        if (Time.time >= nextMedkitSpawnTime && activeMedkits.Count < medkit.maxItemsOnMap)
        {
            TrySpawnItem(medkit, ref activeMedkits);
            nextMedkitSpawnTime = Time.time + Random.Range(medkit.minRespawnTime, medkit.maxRespawnTime);
        }

        // Check for food respawn
        if (Time.time >= nextFoodSpawnTime && activeFood.Count < food.maxItemsOnMap)
        {
            TrySpawnItem(food, ref activeFood);
            nextFoodSpawnTime = Time.time + Random.Range(food.minRespawnTime, food.maxRespawnTime);
        }
    }

    private void SpawnInitialItems()
    {
        // Spawn initial medkits
        for (int i = 0; i < medkit.maxItemsOnMap; i++)
        {
            if (Random.value <= medkit.spawnProbability)
            {
                TrySpawnItem(medkit, ref activeMedkits);
            }
        }

        // Spawn initial food
        for (int i = 0; i < food.maxItemsOnMap; i++)
        {
            if (Random.value <= food.spawnProbability)
            {
                TrySpawnItem(food, ref activeFood);
            }
        }
    }

    private void TrySpawnItem(SpawnableItem item, ref List<GameObject> activeList)
    {
        Vector3 spawnPosition = GetRandomSpawnPosition();

        // Check if position is valid (not colliding with other objects)
        if (!Physics.CheckSphere(spawnPosition, spawnCheckRadius, spawnCheckLayer))
        {
            GameObject newItem = Instantiate(item.prefab, spawnPosition, Quaternion.identity);
            activeList.Add(newItem);

            // Setup item to notify spawner when picked up
            CollectableItem collectable = newItem.GetComponent<CollectableItem>();
            if (collectable == null)
            {
                collectable = newItem.AddComponent<CollectableItem>();
            }
            collectable.SetSpawner(this, item == medkit);
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
        float z = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
        return new Vector3(x, spawnHeight, z);
    }

    public void ItemCollected(GameObject item, bool isMedkit)
    {
        if (isMedkit)
        {
            activeMedkits.Remove(item);
        }
        else
        {
            activeFood.Remove(item);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw spawn area in editor
        Gizmos.color = Color.green;
        Vector3 center = new Vector3(
            (spawnAreaMin.x + spawnAreaMax.x) / 2,
            spawnHeight,
            (spawnAreaMin.y + spawnAreaMax.y) / 2
        );
        Vector3 size = new Vector3(
            spawnAreaMax.x - spawnAreaMin.x,
            0.1f,
            spawnAreaMax.y - spawnAreaMin.y
        );
        Gizmos.DrawWireCube(center, size);
    }
}

// Helper component for collectable items
public class CollectableItem : MonoBehaviour
{
    private ItemSpawner spawner;
    private bool isMedkit;

    public void SetSpawner(ItemSpawner spawner, bool isMedkit)
    {
        this.spawner = spawner;
        this.isMedkit = isMedkit;
    }

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.ItemCollected(gameObject, isMedkit);
        }
    }
}