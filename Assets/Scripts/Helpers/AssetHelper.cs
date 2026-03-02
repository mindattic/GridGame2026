using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Libraries;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Helpers
{
    public static class AssetHelper
    {
        /// <summary>
        /// Loads an addressable asset asynchronously.
        /// Note: Caller owns the reference and must release it with Addressables.Release(result) when done.
        /// </summary>
        public static async Task<T> LoadAssetAsync<T>(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogError($"AssetHelper.LoadAssetAsync<{typeof(T).Name}> called with null/empty address.");
                return default;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;

            Debug.LogError($"Failed to load {typeof(T).Name} at address: '{address}' (Status: {handle.Status})");
            return default;
        }

        /// <summary>
        /// Loads an addressable asset synchronously (blocks until complete).
        /// Note: Caller owns the reference and must release it with Addressables.Release(result) when done.
        /// </summary>
        public static T LoadAsset<T>(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogError($"AssetHelper.LoadAsset<{typeof(T).Name}> called with null/empty address.");
                return default;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            handle.WaitForCompletion();

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;

            Debug.LogError($"Failed to load {typeof(T).Name} at address: '{address}' (Status: {handle.Status})");
            return default;
        }
    }
}
