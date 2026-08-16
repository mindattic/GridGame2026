namespace Scripts.Data.Config
{
    /// <summary>
    /// STARTSCENECONFIG - Declares which scene the game starts in.
    /// <para>PURPOSE: Single source of truth for the Play Mode boot scene and the
    /// first entry of EditorBuildSettings.scenes (i.e. the scene a built player
    /// launches into). Keeping the value in code â€” not in EditorPrefs, not in a
    /// JSON file â€” means the choice is diffable, reviewable, and survives fresh
    /// clones without any editor-side setup.</para>
    /// <para>AUTHORING: Do NOT hand-edit. Run GridGame.Console.ps1 â†’ Option 20
    /// ("Set Start Scene"), which rewrites the StartScene constant below and
    /// lets Unity's StartSceneAuthority.[InitializeOnLoad] hook apply the value
    /// on the next domain reload.</para>
    /// <para>RELATED FILES: StartSceneAuthority.cs, GridGame.Console.ps1</para>
    /// </summary>
    public static class StartSceneConfig
    {
        // START_SCENE_BEGIN â€” PS1 rewrites the single line below. Keep markers intact.
        public const string StartScene = "SplashScreen";
        // START_SCENE_END
    }
}
