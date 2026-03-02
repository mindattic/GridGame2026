using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Factories;
using Scripts.Helpers;
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
using Scripts.Utilities;

namespace Scripts.Serialization
{
    // Attach to a scene object and point to the Props root and map path.
    public sealed class MapPropEditorBootstrapper : MonoBehaviour
    {
        [SerializeField] private Transform propsRoot;
        [SerializeField] private string mapPath = "Maps/Test/Test";
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool clearExisting = true;

        private void Start()
        {
            if (loadOnStart && propsRoot != null && !string.IsNullOrWhiteSpace(mapPath))
            {
                PropMapIO.LoadInto(propsRoot, mapPath, clearExisting);
            }
        }

        [ContextMenu("Load Map Now")]
        private void LoadNow()
        {
            if (propsRoot == null) return;
            PropMapIO.LoadInto(propsRoot, mapPath, clearExisting: true);
        }
    }
}
