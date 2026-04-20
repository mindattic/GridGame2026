using UnityEngine;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Config;
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
    // Attach to a scene object and set PropsRoot programmatically. MapPath,
    // LoadOnStart, and ClearExisting are compile-time constants from
    // MapPropEditorConfig; PropMapHotkeys reads PropsRoot / MapPath via the
    // public properties below rather than reflecting into private fields.
    public sealed class MapPropEditorBootstrapper : MonoBehaviour
    {
        private Transform propsRoot;

        /// <summary>Programmatic access to the props hierarchy root.</summary>
        public Transform PropsRoot
        {
            get => propsRoot;
            set => propsRoot = value;
        }

        /// <summary>Resources path of the props map JSON.</summary>
        public string MapPath => MapPropEditorConfig.MapPath;

        /// <summary>Performs initial setup after all Awake calls complete.</summary>
        private void Start()
        {
            if (MapPropEditorConfig.LoadOnStart && propsRoot != null && !string.IsNullOrWhiteSpace(MapPropEditorConfig.MapPath))
            {
                PropMapIO.LoadInto(propsRoot, MapPropEditorConfig.MapPath, MapPropEditorConfig.ClearExisting);
            }
        }

        [ContextMenu("Load Map Now")]
        /// <summary>Load now.</summary>
        private void LoadNow()
        {
            if (propsRoot == null) return;
            PropMapIO.LoadInto(propsRoot, MapPropEditorConfig.MapPath, clearExisting: true);
        }
    }
}
