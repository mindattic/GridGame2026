using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Assets.Helpers;
using Assets.Scripts.Managers;
using scene = Assets.Helpers.SceneHelper;
using Assets.Helper;
using Assets.Scripts.Libraries; // GameObjectHelper

public class PostBattleManager : MonoBehaviour
{
    // Constants / configuration (no inspector exposure)
    private const float AutoEnableDelay = 0.25f;   // delay before enabling Next after all panes animate
    private const float PaneSpacing = 8f;          // vertical spacing between panes

    // Scene wired references (resolved in Awake via GameObjectHelper)
    private RectTransform _scrollContent;          // Canvas/ScrollView/Viewport/Content
    private Button _nextButton;                    // Canvas/BottomBar/NextButton

    // Prefab (fetched from PrefabLibrary at runtime)
    private GameObject _heroExperiencePanePrefab;

    // Destination scene decided at runtime (defaults to tracker hint, fallback to Hub)
    private string nextSceneName;

    // Runtime state
    private readonly List<HeroExperiencePane> _panes = new List<HeroExperiencePane>();
    private bool _monitoring;
    private VerticalLayoutGroup _layout;
    private ContentSizeFitter _fitter;

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

    private void Start() => scene.FadeIn();

    private void OnDestroy()
    {
        if (_nextButton != null)
            _nextButton.onClick.RemoveListener(OnNext);
    }

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

    private void ResolvePrefabsAndConfig()
    {
        _heroExperiencePanePrefab = PrefabLibrary.Get("HeroExperiencePane");
        if (_heroExperiencePanePrefab == null)
            Debug.LogError("PostBattleManager: HeroExperiencePane prefab not found in PrefabLibrary.");

        nextSceneName = ExperienceTracker.NextSceneAfterPostBattleScreen;
        if (string.IsNullOrEmpty(nextSceneName))
            nextSceneName = SceneHelper.Hub; // go to Hub by default
    }

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

    private void Update()
    {
        if (!_monitoring || _panes.Count == 0) return;
        if (_panes.All(p => p != null && p.IsFillComplete))
        {
            _monitoring = false;
            StartCoroutine(EnableNextSoon());
        }
    }

    private IEnumerator EnableNextSoon()
    {
        yield return new WaitForSeconds(AutoEnableDelay);
        if (_nextButton != null)
            _nextButton.gameObject.SetActive(true);
    }

    private void BuildPanes()
    {
        if (_scrollContent == null || _heroExperiencePanePrefab == null) return;

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

    private void CreatePane(CharacterClass character, bool inParty, int xpGained)
    {
        if (_heroExperiencePanePrefab == null || _scrollContent == null) return;
        var go = Instantiate(_heroExperiencePanePrefab, _scrollContent);
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
}
