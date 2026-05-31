// --- File: Assets/Scripts/Helpers/Bootstrap.cs ---
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
    /// <summary>
    /// BOOTSTRAP - One-time application startup configuration, entry-scene-independent.
    /// <para>PURPOSE: Runs once at app launch (before any scene loads) to apply global
    /// settings that must hold regardless of which scene is the start scene. Currently
    /// pins <c>Application.targetFrameRate = 60</c> so the Editor matches build framerate
    /// from the very first frame — perf spikes are then visible throughout the whole
    /// session, not just once <see cref="Scripts.Managers.GameManager"/> applies the
    /// user's framerate setting on entering battle (US-004, bible §30.5).</para>
    /// <para>Uses <c>[RuntimeInitializeOnLoadMethod]</c> rather than a MonoBehaviour Awake
    /// because <see cref="Scripts.Data.Config.StartSceneConfig.StartScene"/> is configurable —
    /// no single scene's manager is guaranteed to run at boot.</para>
    /// <para>RELATED FILES: GameManager.cs, StartSceneConfig.cs</para>
    /// </summary>
    public static class Bootstrap
    {
        /// <summary>Target framerate pinned at startup (bible §30.5). GameManager later
        /// refines this from the user's saved setting when a battle begins.</summary>
        public const int DefaultTargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Application.targetFrameRate = DefaultTargetFrameRate;
        }
    }
}
