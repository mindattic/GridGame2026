# Scene: Overworld
Path: `Assets/Scenes/Overworld.unity`
Build Index: 2
Root Object Count: 6

---

## Hierarchy

```
[Main Camera]
  Transform: pos=(0.00,-7.99,-8.49), scale=(1,1,1)
  Camera: orthographic=False, depth=-1
  Mode7CameraController
[EventSystem]
[Canvas]
  Transform: pos=(589.50,1278.00,0.00), scale=(1.09,1.09,1.09)
  Canvas: renderMode=ScreenSpaceOverlay, sortOrder=0
  [BackButton]
    Transform: pos=(-365.00,1106.69,0.00), scale=(1,1,1)
    Image: sprite=null
    Button: interactable=True
    [Label]
      Transform: pos=(-32.00,32.00,0.00), scale=(1,1,1)
      TextMeshProUGUI: "<"
  [Title]
    Transform: pos=(0.00,1042.69,0.00), scale=(1,1,1)
    TextMeshProUGUI: ""
  [DayNightCycle]
    Image: sprite=null
    DayNightCycle
  [OffscreenArrow]
    Transform: pos=(475.00,100.00,0.00), scale=(1,1,1)
    Image: sprite=offscreen-arrow
    OffscreenArrowIndicator
  [CameraModeButton]
    Transform: pos=(476.00,1044.26,0.00), scale=(1,1,1)
    Image: sprite=UISprite
    Button: interactable=True
    [Image]
      Image: sprite=Camera00
    [Label]
      Transform: pos=(0.00,-43.43,0.00), scale=(1,1,1)
      TextMeshProUGUI: ""
  [ChangeLeaderButton]
    Transform: pos=(403.70,1044.26,0.00), scale=(1,1,1)
    Image: sprite=UISprite
    Button: interactable=True
    [Image]
      Image: sprite=null
    [Label]
      Transform: pos=(0.00,-43.43,0.00), scale=(1,1,1)
      TextMeshProUGUI: ""
  [FadeOverlay]
    Transform: pos=(-0.73,0.73,0.00), scale=(144.79,144.79,1.46)
    Image: sprite=Black32x32
    FadeOverlayInstance
  [CutoutOverlay]
    Transform: pos=(0.00,0.00,0.00), scale=(1,1,1)
    CutoutOverlay
    [Top]
      Transform: pos=(0.00,1170.69,0.00), scale=(1,1,1)
      Image: sprite=Black32x32
      [LeftPane]
        Transform: pos=(-540.00,-65.08,0.00), scale=(1,1,1)
      [CenterPane]
        Transform: pos=(0.00,-65.08,0.00), scale=(1,1,1)
      [RightPane]
        Transform: pos=(540.00,-65.08,0.00), scale=(1,1,1)
    [Bottom] [INACTIVE]
      Transform: pos=(0.00,-1170.69,0.00), scale=(1,1,1)
      Image: sprite=Black32x32
  [EnterHubButton]
    Image: sprite=UISprite
    Button: interactable=True
    [Label]
      TextMeshProUGUI: "Enter"
[Map]
  [Terrain]
    SpriteRenderer: sprite=Terrain, order=-20010
  [Surface]
    SpriteRenderer: sprite=Surface, order=-20008
  [Soil]
    SpriteRenderer: sprite=Soil, order=-20007
    SoilInstance
  [FishingSpot] [INACTIVE]
    Transform: pos=(1258.28,-755.70,0.00), scale=(1.00,1.00,100.00)
    SpriteRenderer: sprite=null, order=4
    MapIcon
  [Heroes]
    [Hero_00]
      Transform: pos=(0,0,0), scale=(1,1,1)
      SpriteRenderer: sprite=Alice_Idle_0, order=0
      OverworldHero
    [Hero_01]
      Transform: pos=(0,0,0), scale=(1,1,1)
      SpriteRenderer: sprite=Alice_Idle_0, order=0
      OverworldHero
    [Hero_02]
      Transform: pos=(0,0,0), scale=(1,1,1)
      SpriteRenderer: sprite=Alice_Idle_0, order=0
      OverworldHero
    [Hero_03]
      Transform: pos=(0,0,0), scale=(1,1,1)
      SpriteRenderer: sprite=Alice_Idle_0, order=0
      OverworldHero
  [Canopy]
    SpriteRenderer: sprite=Canopy, order=10000
    CanopyInstance
  [Props]
    [Bushes]
      [Bush]
        Transform: pos=(12.08,2.44,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(9.69,1.78,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(12.28,-0.77,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(7.56,2.86,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(5.40,-3.79,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(5.24,-0.35,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(-0.04,-4.05,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(-2.46,-2.05,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(-4.88,1.91,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(-4.36,4.56,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(-8.06,4.24,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(-9.34,9.05,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(-8.78,5.68,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush00, order=19
        BushInstance
      [Bush]
        Transform: pos=(-4.98,-0.35,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(-0.92,-2.54,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(-3.70,6.49,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(-6.62,8.66,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(5.07,-0.97,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(1.11,-2.87,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(12.77,-0.97,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(-8.09,-0.38,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(-2.03,1.71,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(2.16,-3.30,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(-3.77,-1.79,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(6.58,3.55,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(12.74,4.86,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(19.49,4.06,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(9.67,7.84,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(16.34,0.86,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush01, order=19
        BushInstance
      [Bush]
        Transform: pos=(9.80,0.70,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(11.99,3.18,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(9.09,1.71,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(11.37,2.31,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush03, order=19
        BushInstance
      [Bush]
        Transform: pos=(11.42,1.20,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(11.87,1.21,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush05, order=19
        BushInstance
      [Bush]
        Transform: pos=(10.92,4.02,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush03, order=19
        BushInstance
      [Bush]
        Transform: pos=(8.52,-0.68,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush05, order=19
        BushInstance
      [Bush]
        Transform: pos=(10.48,-0.26,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush03, order=19
        BushInstance
      [Bush]
        Transform: pos=(3.22,-4.02,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(-5.58,8.19,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(-3.25,7.68,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(-4.19,6.22,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush03, order=19
        BushInstance
      [Bush]
        Transform: pos=(-11.45,2.46,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(2.43,-4.81,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(4.84,-3.52,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush04, order=19
        BushInstance
      [Bush]
        Transform: pos=(4.34,-0.69,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush03, order=19
        BushInstance
      [Bush]
        Transform: pos=(3.89,-4.98,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush03, order=19
        BushInstance
      [Bush]
        Transform: pos=(13.28,3.44,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush05, order=19
        BushInstance
      [Bush]
        Transform: pos=(9.58,-2.49,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Bush03, order=19
        BushInstance
    [Clouds]
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud01, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud01, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud01, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud01, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud01, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud01, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud01, order=9999
        CloudInstance
      [Cloud]
        Transform: pos=(-28.86,0.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Cloud00, order=9999
        CloudInstance
    [CloudShadows]
      Transform: pos=(-28.31,0.00,0.00), scale=(1,1,1)
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud01, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
      [CloudShadow]
        SpriteRenderer: sprite=Cloud00, order=9998
        CloudShadowInstance
    [Grasses]
      [Grass]
        Transform: pos=(12.23,-2.42,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(8.89,-0.34,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(11.66,-1.17,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-8.24,2.30,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-10.38,5.05,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-20.59,7.70,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-6.28,9.31,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-8.88,4.59,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-4.21,4.39,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-4.37,10.28,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(7.94,-0.43,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(14.10,4.36,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass02, order=19
        GrassInstance
      [Grass]
        Transform: pos=(7.40,-0.12,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(12.92,-0.90,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(17.38,-1.22,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(19.64,5.19,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(5.41,4.76,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(1.89,-5.05,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-0.57,0.99,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-2.73,3.25,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-3.46,5.39,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-6.37,-2.03,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-1.39,-2.51,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-6.59,-1.30,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass02, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-2.81,2.53,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(-3.24,-3.47,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(14.10,3.61,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(7.72,0.31,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass02, order=19
        GrassInstance
      [Grass]
        Transform: pos=(11.96,-2.04,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(8.90,-3.36,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass02, order=19
        GrassInstance
      [Grass]
        Transform: pos=(11.56,2.74,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(11.51,4.13,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(13.84,0.31,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass02, order=19
        GrassInstance
      [Grass]
        Transform: pos=(7.82,-1.40,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(11.07,-1.87,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(10.66,2.77,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(14.19,1.81,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(13.57,-0.78,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(10.18,4.54,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(11.28,-0.69,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(13.41,5.11,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass01, order=19
        GrassInstance
      [Grass]
        Transform: pos=(9.01,2.56,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass00, order=19
        GrassInstance
      [Grass]
        Transform: pos=(8.38,2.93,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass02, order=19
        GrassInstance
      [Grass]
        Transform: pos=(9.13,3.10,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Grass02, order=19
        GrassInstance
    [Rocks]
      RockInstance
      SpriteRenderer: sprite=null, order=0
      [Rock]
        Transform: pos=(-8.34,6.55,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock02_0, order=30
      [Rock]
        Transform: pos=(-7.15,5.20,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock03_0, order=30
      [Rock]
        Transform: pos=(-10.17,3.79,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock00_0, order=30
      [Rock]
        Transform: pos=(-1.84,7.72,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock03_0, order=30
      [Rock]
        Transform: pos=(-8.89,-1.95,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock04_0, order=30
      [Rock]
        Transform: pos=(-1.56,-2.52,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock03_0, order=30
      [Rock]
        Transform: pos=(2.33,7.84,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock00_0, order=30
      [Rock]
        Transform: pos=(9.65,7.28,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock00_0, order=30
      [Rock]
        Transform: pos=(12.67,-1.66,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock02_0, order=30
      [Rock]
        Transform: pos=(7.75,13.09,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock00_0, order=30
      [Rock]
        Transform: pos=(10.21,7.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock05_0, order=30
      [Rock]
        Transform: pos=(7.81,4.93,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock04_0, order=30
      [Rock]
        Transform: pos=(14.23,0.02,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(-4.15,5.16,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(-10.41,-2.89,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(-9.91,4.88,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock05_0, order=30
      [Rock]
        Transform: pos=(13.95,0.85,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock03_0, order=30
      [Rock]
        Transform: pos=(5.51,3.82,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock04_0, order=30
      [Rock]
        Transform: pos=(5.87,-1.36,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(11.06,8.65,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock03_0, order=30
      [Rock]
        Transform: pos=(14.64,4.93,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(18.79,4.08,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock05_0, order=30
      [Rock]
        Transform: pos=(-13.41,10.15,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock02_0, order=30
      [Rock]
        Transform: pos=(-10.95,5.86,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock02_0, order=30
      [Rock]
        Transform: pos=(-2.54,2.14,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(2.55,-4.26,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock04_0, order=30
      [Rock]
        Transform: pos=(-9.69,-0.60,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(-16.38,-0.66,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock05_0, order=30
      [Rock]
        Transform: pos=(18.44,-1.46,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock03_0, order=30
      [Rock]
        Transform: pos=(9.06,8.03,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock04_0, order=30
      [Rock]
        Transform: pos=(2.89,12.67,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(5.41,5.06,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock03_0, order=30
      [Rock]
        Transform: pos=(-2.71,3.23,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(2.89,-2.89,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(-5.35,-1.23,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock05_0, order=30
      [Rock]
        Transform: pos=(-8.78,2.49,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(-2.37,5.06,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock04_0, order=30
      [Rock]
        Transform: pos=(12.10,6.60,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock04_0, order=30
      [Rock]
        Transform: pos=(-5.12,4.20,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(-4.03,9.12,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock04_0, order=30
      [Rock]
        Transform: pos=(9.24,1.11,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(-4.83,-3.06,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock03_0, order=30
      [Rock]
        Transform: pos=(-8.43,3.17,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(11.93,-2.32,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock05_0, order=30
      [Rock]
        Transform: pos=(6.50,3.97,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock03_0, order=30
      [Rock]
        Transform: pos=(-10.83,2.43,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(18.91,1.28,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock01_0, order=30
      [Rock]
        Transform: pos=(-9.70,3.10,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Rock05_0, order=30
    [Trees]
      [Tree]
        Transform: pos=(10.35,3.70,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree03, order=30
        TreeInstance
      [Tree]
        Transform: pos=(7.24,-0.88,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree03, order=30
        TreeInstance
      [Tree]
        Transform: pos=(10.63,-2.06,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(14.86,1.78,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(1.58,-3.75,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(0.32,-2.71,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree02, order=30
        TreeInstance
      [Tree]
        Transform: pos=(1.46,1.09,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-4.14,1.94,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(3.58,6.91,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(7.39,11.62,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-6.14,-4.27,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-0.01,-4.73,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-4.27,6.71,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(6.82,-3.57,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree02, order=30
        TreeInstance
      [Tree]
        Transform: pos=(15.43,4.43,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(11.20,4.43,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(9.62,3.30,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(2.91,6.53,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-12.10,1.67,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(6.47,-0.33,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(16.97,1.38,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(8.18,-2.63,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-6.09,-0.16,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree02, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-4.84,2.46,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-7.64,5.47,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-1.71,-4.03,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree02, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-2.74,6.66,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(5.05,11.95,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree01, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-7.31,-3.02,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree02, order=30
        TreeInstance
      [Tree]
        Transform: pos=(10.80,3.00,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(7.06,6.22,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(8.09,8.74,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree02, order=30
        TreeInstance
      [Tree]
        Transform: pos=(3.34,-0.57,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-3.61,-0.93,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-5.19,8.79,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree02, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-9.75,6.55,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-11.18,9.33,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
      [Tree]
        Transform: pos=(-13.45,-0.45,0.00), scale=(1,1,1)
        SpriteRenderer: sprite=Tree00, order=30
        TreeInstance
  [Caravan]
    Transform: pos=(-1.04,-1.87,0.00), scale=(0.10,0.10,1.00)
    SpriteRenderer: sprite=overworld-caravan, order=0
    CaravanInstance
[OverworldManager]
  Transform: pos=(-0.83,-1.00,756.67), scale=(1,1,1)
  OverworldManager
[BattleTransition]
  ScreenGrabber
  ZoomEffect
```

## Summary

- **Total GameObjects**: 321
- **Managers**: OverworldManager
- **Cameras**: Main Camera
- **Canvases**: Canvas
