using Assets.Scripts.Factories;
using System;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Managers
{
    /// <summary>
    /// COINMANAGER - Spawns and manages coin pickup effects.
    /// 
    /// PURPOSE:
    /// Creates coin visuals when enemies are defeated. Coins animate
    /// toward the coin counter UI and add to player's currency.
    /// 
    /// VISUAL APPEARANCE:
    /// ```
    /// [Enemy dies] → 💰 💰 💰 (coins burst out)
    ///                  ↘ ↓ ↙
    ///               [Coin Counter]
    /// ```
    /// 
    /// COIN SPAWNING:
    /// - Spawn(position): Single coin at position
    /// - SpawnBurst(position, amount): Multiple coins at once
    /// 
    /// DEATH INTEGRATION:
    /// TrySpawnOnDeathThreshold() is called during enemy fade-out:
    /// - Spawns coins when alpha passes threshold
    /// - Prevents double-spawning via hasSpawnedCoins flag
    /// - Only spawns for enemies (not heroes)
    /// 
    /// COIN LIFECYCLE:
    /// 1. Enemy dies, DeathSequence runs
    /// 2. SpawnBurst() creates coins at death position
    /// 3. CoinInstance animates toward coin counter
    /// 4. Coin collected, currency incremented
    /// 5. CoinInstance self-destructs
    /// 
    /// RELATED FILES:
    /// - CoinFactory.cs: Creates coin GameObjects
    /// - CoinInstance.cs: Coin behavior/animation
    /// - CoinCounter.cs: UI displaying total coins
    /// - DeathHelper.cs: Triggers coin spawn on death
    /// 
    /// ACCESS: g.CoinManager
    /// </summary>
    public class CoinManager : MonoBehaviour
    {
        /// <summary>Spawns a single coin at the given position.</summary>
        public void Spawn(Vector3 position)
        {
            var go = CoinFactory.Create();
            go.transform.position = Vector2.zero;
            go.transform.rotation = Quaternion.identity;
            var instance = go.GetComponent<CoinInstance>();
            instance.name = $"Coin_{Guid.NewGuid():N}";
            instance.Spawn(position);
        }

        /// <summary>Spawns multiple coins at a position (burst effect).</summary>
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

        /// <summary>
        /// Spawns coins when death fade alpha passes threshold.
        /// Returns true if coins were spawned this call.
        /// </summary>
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
