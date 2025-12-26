using Assets.Scripts.Libraries;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    public class CoinManager : MonoBehaviour
    {
        private GameObject CoinPrefab;

        public void Awake()
        {
            CoinPrefab = PrefabLibrary.Prefabs["CoinPrefab"];
        }

        public void Spawn(Vector3 position)
        {
            var go = Instantiate(CoinPrefab, Vector2.zero, Quaternion.identity);
            var instance = go.GetComponent<CoinInstance>();
            instance.name = $"Coin_{Guid.NewGuid():N}";
            instance.Spawn(position);
        }

        // Spawns a burst of coins at a position.
        public void SpawnBurst(Vector3 worldPosition, int amount)
        {
            if (amount <= 0) return;
            StartCoroutine(SpawnRoutine(worldPosition, amount));
        }

        private IEnumerator SpawnRoutine(Vector3 worldPosition, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                Spawn(worldPosition);
            }
            yield return null;
        }

        // Call this inside fade loops to spawn coins once when alpha passes a threshold.
        // Returns true if coins were spawned this call.
        public bool TrySpawnOnDeathThreshold(ActorInstance actor, ref bool hasSpawnedCoins, float alpha, int amount, float threshold)
        {
            if (actor == null || hasSpawnedCoins || !actor.IsEnemy) return false;
            if (alpha >= threshold) return false;

            hasSpawnedCoins = true;
            SpawnBurst(actor.Position, amount);
            return true;
        }
    }
}
