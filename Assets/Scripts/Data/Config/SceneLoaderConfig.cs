namespace Scripts.Data.Config
{
    /// <summary>
    /// SCENELOADERCONFIG - Static tuning values for SceneLoader.
    /// <para>PURPOSE: Replaces the former [SerializeField] tuning fields on
    /// SceneLoader with compile-time constants. The four UI references
    /// (progressBar, progressLabel, fadePanel, progressPanel) are now resolved
    /// at runtime via transform.Find() against the LoadingScreen scaffold
    /// hierarchy rather than Inspector drag-drop.</para>
    /// <para>USAGE: Referenced from SceneLoader.Start / FadeFromBlackRoutine /
    /// ShowUIAfterDelay / LoadRoutine.</para>
    /// <para>RELATED FILES: SceneLoader.cs, LoadingScreenScaffold.cs</para>
    /// </summary>
    public static class SceneLoaderConfig
    {
        // ── Timings ──────────────────────────────────────────────────────────
        public const float FadeInDuration     = 0.4f;
        public const float UIShowDelay        = 1.0f;
        public const float MinimumVisibleTime = 0.5f;

        // ── Behavior ─────────────────────────────────────────────────────────
        public const bool ActivateWhenReady = true;

        // ── Editor Bootstrap ─────────────────────────────────────────────────
        // If the game starts on the LoadingScreen with no pending target,
        // automatically load this scene.
        public const bool   AutoLoadWhenLaunchedDirectly = true;
        public const string BootstrapScene              = "TitleScreen";

        // ── Scene hierarchy paths (under Canvas, resolved in Start) ──────────
        public const string CanvasName        = "Canvas";
        public const string FadePanelPath     = "FadePanel";
        public const string ProgressPanelPath = "ProgressPanel";
        public const string ProgressBarPath   = "ProgressPanel/ProgressBar";
        public const string ProgressLabelPath = "ProgressPanel/ProgressLabel";
    }
}
