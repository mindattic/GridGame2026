using UnityEngine;

namespace Scripts.Data.Config
{
    /// <summary>
    /// MAPPROPEDITORCONFIG - Static tuning values for the Props map editor tools.
    /// <para>PURPOSE: Replaces the former [SerializeField] fields on
    /// MapPropEditorBootstrapper and PropMapHotkeys with compile-time constants.
    /// These components are editor-tooling helpers; <c>propsRoot</c> and
    /// <c>loader</c> stay as plain private fields set programmatically.</para>
    /// <para>USAGE: Referenced from MapPropEditorBootstrapper.Start /
    /// MapPropEditor PropMapHotkeys.Update.</para>
    /// <para>RELATED FILES: MapPropEditor.cs, MapPropEditorBootstrapper.cs</para>
    /// </summary>
    public static class MapPropEditorConfig
    {
        // ── Bootstrapper ─────────────────────────────────────────────────────
        // Resources path (without extension) of the props map JSON to load.
        public const string MapPath = "Maps/Test/Test";

        // If true, load MapPath into propsRoot during Start.
        public const bool LoadOnStart = true;

        // If true, destroy existing children of propsRoot before loading.
        public const bool ClearExisting = true;

        // ── Save Hotkey (PropMapHotkeys) ─────────────────────────────────────
        // Key that triggers a map save (combined with modifier requirements).
        public const KeyCode SaveKey = KeyCode.S;

        // If true, Ctrl (or Cmd) must be held while pressing SaveKey.
        public const bool RequireCtrl = true;

        // If true, Alt must be held while pressing SaveKey.
        public const bool RequireAlt = true;
    }
}
