using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Scripts.Helpers;
using Scripts.Factories;
using Scripts.Managers;
using scene = Scripts.Helpers.SceneHelper;
using Scripts.Helpers;
using Scripts.Libraries;
using Scripts.Canvas;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Managers
{
/// <summary>
/// POSTBATTLEMANAGER - Manages the post-battle results screen.
/// 
/// PURPOSE:
/// Displays battle results including experience gained, level ups,
/// and rewards after completing a stage.
/// 
/// VISUAL LAYOUT:
/// ```
/// ┌─────────────────────────────────────┐
/// │         Battle Results              │
/// ├─────────────────────────────────────┤
/// │  ┌─────────────────────────────┐   │
/// │  │ [Hero1] Paladin             │   │
/// │  │ EXP: ████████░░ +150        │   │ ← Experience pane
/// │  │ LEVEL UP! 5 → 6             │   │
/// │  └─────────────────────────────┘   │
/// │  ┌─────────────────────────────┐   │
/// │  │ [Hero2] Archer              │   │
/// │  │ EXP: ██████████ +200        │   │
/// │  └─────────────────────────────┘   │
/// │                                     │
/// │            [ Next ]                 │
/// └─────────────────────────────────────┘
/// ```
/// 
/// EXPERIENCE PANES:
/// Each hero gets a HeroExperiencePane showing:
/// - Portrait and name
/// - Experience bar animation
/// - Level up notification if applicable
/// 
/// FLOW:
/// 1. Stage completed successfully
/// 2. Experience calculated and applied
/// 3. PostBattle scene loaded
/// 4. Panes animate experience gain
/// 5. Player clicks Next to continue
/// 
/// RELATED FILES:
/// - HeroExperiencePaneFactory.cs: Creates pane GameObjects
/// - HeroExperiencePane.cs: Pane behavior/animation
/// - StageManager.cs: Triggers post-battle
/// - ProfileHelper.cs: Saves progress
/// 
/// ACCESS: Scene-based manager (PostBattle scene)
/// </summary>
public class PostBattleManager : MonoBehaviour
{
    #region Configuration

    private const float AutoEnableDelay = 0.25f;
    private const float PaneSpacing = 8f;

    #endregion

    #region References

    private RectTransform _scrollContent;
    private Button _nextButton;
    private string nextSceneName;

    #endregion

    #region Runtime State

    private readonly List<HeroExperiencePane> _panes = new List<HeroExperiencePane>();
    private bool _monitoring;
    private VerticalLayoutGroup _layout;
    private ContentSizeFitter _fitter;

    #endregion

    #region Initialization

    /// <summary>Initializes component references and state.</summary>
    private void Awake()
    {
        // Guard: require active save & party
        var save = ProfileHelper.CurrentProfile?.CurrentSave;
        if (save == null || save.Party?.Members == null || save.Party.Members.Count == 0)
        {
            SceneHelper.Switch.ToTitleScreen();
            return;
        }

        ResolveSceneReferences();
        ResolvePrefabsAndConfig();
        ConfigureLayout();
        BuildPanes();
    }

    /// <summary>Performs initial setup after all Awake calls complete.</summary>
    private void Start() => scene.FadeIn();

    /// <summary>Cleans up resources when the object is destroyed.</summary>
    private void OnDestroy()
    {
        if (_nextButton != null)
            _nextButton.onClick.RemoveListener(OnNext);
    }

    #endregion

    #region Scene Setup

    /// <summary>Resolve scene references.</summary>
    private void ResolveSceneReferences()
    {
        var contentGO = GameObject.Find(GameObjectHelper.PostBattleScreen.Content);
        if (contentGO == null)
        {
            Debug.LogError("PostBattleManager: Could not find content at path: " + GameObjectHelper.PostBattleScreen.Content);
            return;
        }
        _scrollContent = contentGO.GetComponent<RectTransform>();

        var nextGO = GameObject.Find(GameObjectHelper.PostBattleScreen.NextButton);
        if (nextGO == null)
        {
            Debug.LogError("PostBattleManager: Could not find NextButton at path: " + GameObjectHelper.PostBattleScreen.NextButton);
        }
        else
        {
            _nextButton = nextGO.GetComponent<Button>();
            _nextButton.onClick.RemoveListener(OnNext); // ensure single subscription
            _nextButton.onClick.AddListener(OnNext);
            _nextButton.gameObject.SetActive(false);
        }
    }

    /// <summary>Resolve prefabs and config.</summary>
    private void ResolvePrefabsAndConfig()
    {
        nextSceneName = ExperienceTracker.NextSceneAfterPostBattleScreen;
        if (string.IsNullOrEmpty(nextSceneName))
            nextSceneName = SceneHelper.Hub; // go to Hub by default
    }

    /// <summary>Configure layout.</summary>
    private void ConfigureLayout()
    {
        if (_scrollContent == null) return;
        _layout = _scrollContent.GetComponent<VerticalLayoutGroup>();
        if (_layout == null)
            _layout = _scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
        _layout.spacing = PaneSpacing;
        _layout.childAlignment = TextAnchor.UpperLeft;
        _layout.childControlHeight = true;
        _layout.childControlWidth = true;
        _layout.childForceExpandHeight = false;
        _layout.childForceExpandWidth = true;

        _fitter = _scrollContent.GetComponent<ContentSizeFitter>();
        if (_fitter == null)
            _fitter = _scrollContent.gameObject.AddComponent<ContentSizeFitter>();
        _fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>Runs per-frame update logic.</summary>
    private void Update()
    {
        if (!_monitoring || _panes.Count == 0) return;
        if (_panes.All(p => p != null && p.IsFillComplete))
        {
            _monitoring = false;
            StartCoroutine(EnableNextSoon());
        }
    }

    /// <summary>Enable next soons this component.</summary>
    private IEnumerator EnableNextSoon()
    {
        yield return new WaitForSeconds(AutoEnableDelay);
        if (_nextButton != null)
            _nextButton.gameObject.SetActive(true);
    }

    /// <summary>Creates the panes.</summary>
    private void BuildPanes()
    {
        if (_scrollContent == null) return;

        // Clear existing children
        for (int i = _scrollContent.childCount - 1; i >= 0; i--)
            Destroy(_scrollContent.GetChild(i).gameObject);

        var save = ProfileHelper.CurrentProfile.CurrentSave;
        var party = save.Party?.Members?.Select(m => m.CharacterClass).Where(s => s != CharacterClass.None).ToList() ?? new List<CharacterClass>();
        var roster = save.Roster?.Members?.Select(m => m.CharacterClass).Where(s => s != CharacterClass.None).ToList() ?? new List<CharacterClass>();

        _panes.Clear();

        foreach (var ch in party)
            CreatePane(ch, true, ExperienceTracker.GetXPGained(ch));
        foreach (var ch in roster.Where(c => !party.Contains(c)))
            CreatePane(ch, false, ExperienceTracker.GetXPGained(ch));

        _monitoring = true;
        if (_panes.All(p => p.IsFillComplete))
            StartCoroutine(EnableNextSoon());
    }

    /// <summary>Creates the pane.</summary>
    private void CreatePane(CharacterClass character, bool inParty, int xpGained)
    {
        if (_scrollContent == null) return;
        // Use factory instead of Instantiate(prefab)
        var go = HeroExperiencePaneFactory.Create(_scrollContent);
        go.name = $"Pane_{character}";
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition3D = Vector3.zero;
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = 128f;
            le.preferredHeight = 128f;
            le.flexibleHeight = 0;
            le.flexibleWidth = 0;
        }
        var pane = go.GetComponent<HeroExperiencePane>();
        if (pane != null)
        {
            pane.Build(character, xpGained, inParty);
            _panes.Add(pane);
        }
    }

    /// <summary>Handles the next event.</summary>
    private void OnNext()
    {
        var save = ProfileHelper.CurrentProfile.CurrentSave;
        if (save != null)
        {
            foreach (var kv in ExperienceTracker.AllGains)
            {
                var id = kv.Key; 
                var gained = kv.Value;
                var entry = save.Party.Members.FirstOrDefault(m => m.CharacterClass == id) ?? save.Roster.Members.FirstOrDefault(m => m.CharacterClass == id);
                if (entry != null)
                {
                    entry.TotalXP = Mathf.Max(0, entry.TotalXP + gained);
                }
            }
            ProfileHelper.Save(true);
        }
        ExperienceTracker.Clear();
        SceneHelper.Fade.To(nextSceneName);
    }

    #endregion
}

}
