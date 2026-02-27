using Assets.Scripts.Factories;
using Assets.Scripts.Models;
using Game.Instances;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Behaviors
{
    public class AttackLineManager : MonoBehaviour
    {
        //Variables
        public Dictionary<(Vector2Int, Vector2Int), AttackLineInstance> attackLines = new Dictionary<(Vector2Int, Vector2Int), AttackLineInstance>();

        public bool Exists(ActorPair actorPair)
        {
            var key = GetKey(actorPair);
            return attackLines.ContainsKey(key);
        }

        public void Spawn(ActorPair actorPair)
        {
            var key = GetKey(actorPair);

            if (Exists(actorPair))
                return;

            // Use factory instead of Instantiate(prefab)
            var go = AttackLineFactory.Create();
            go.transform.position = Vector2.zero;
            go.transform.rotation = Quaternion.identity;
            var instance = go.GetComponent<AttackLineInstance>();
            attackLines[key] = instance;
            instance.Spawn(actorPair);
        }

        public void Despawn(ActorPair pair)
        {
            var key = GetKey(pair);
            if (attackLines.TryGetValue(key, out var instance))
            {
                instance.Despawn();
                attackLines.Remove(key);
            }
        }

        public void DespawnAll()
        {
            foreach (var instance in attackLines.Values)
            {

                instance.Despawn();
            }
            attackLines.Clear();
        }

        private (Vector2Int, Vector2Int) GetKey(ActorPair actorPair)
        {
            return (actorPair.startActor.location, actorPair.endActor.location);
        }
    }
}
