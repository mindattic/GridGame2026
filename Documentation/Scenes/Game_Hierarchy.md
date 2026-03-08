# Scene: Game
Path: `Assets/Scenes/Game.unity`
Build Index: 1
Root Object Count: 11

---

## Hierarchy

```
[Main Camera]
  Transform: pos=(0.00,0.00,-1.00), scale=(1.00,1.00,0.00)
  Camera: orthographic=True, depth=-1
  CameraManager
[Overlay Camera]
  Transform: pos=(0.00,0.00,-1.00), scale=(1.00,1.00,0.00)
  Camera: orthographic=True, depth=1
[EventSystem]
[Canvas]
  Transform: pos=(589.50,1278.00,0.00), scale=(1.09,1.09,1.09)
  Canvas: renderMode=ScreenSpaceOverlay, sortOrder=0
  [AbilityBar]
    Transform: pos=(0.00,703.00,0.00), scale=(1,1,1)
    Image: sprite=TitleBar
    AbilityBar
    [Label]
      TextMeshProUGUI: "Test"
  [Pointer]
  [Announcements]
    [WaveAnnouncement]
      [Image]
        Transform: pos=(0,0,0), scale=(7.91,1.00,1.00)
        Image: sprite=Transparent32x32
      [Back]
        Transform: pos=(0.00,8.00,0.00), scale=(1,1,1)
        TextMeshProUGUI: "Wave 1/\u221E"
      [Front]
        TextMeshProUGUI: "Wave 1/\u221E"
    [VictoryAnnouncement]
      [Image]
        Transform: pos=(0,0,0), scale=(7.91,1.00,1.00)
        Image: sprite=Transparent32x32
      [Back]
        Transform: pos=(0.00,8.00,0.00), scale=(1,1,1)
        TextMeshProUGUI: "Victory!"
      [Front]
        TextMeshProUGUI: "Victory!"
    [DefeatAnnouncement]
      [Image]
        Transform: pos=(0,0,0), scale=(7.91,1.00,1.00)
        Image: sprite=Transparent32x32
      [Back]
        Transform: pos=(0.00,8.00,0.00), scale=(1,1,1)
        TextMeshProUGUI: "Defeat"
      [Front]
        TextMeshProUGUI: "Defeat"
  [CoinCounter]
    Transform: pos=(137.63,-29.31,0.00), scale=(1,1,1)
    [Icon]
      Transform: pos=(168.79,1023.68,0.00), scale=(0.17,0.17,1.00)
      Image: sprite=Coin
    [Glow]
      Transform: pos=(168.79,1023.68,0.00), scale=(0.17,0.17,1.00)
      Image: sprite=Coin
    [Value]
      Transform: pos=(193.79,1053.65,0.00), scale=(1,1,1)
      MeshRenderer: materials=1
      TextMeshProUGUI: "0000000"
  [TimelineBar]
    Transform: pos=(0.00,840.00,0.00), scale=(1,1,1)
    TimelineBarInstance
    [Line]
      Image: sprite=null
    [Left]
      Transform: pos=(-444.75,0.00,0.00), scale=(1,1,1)
      Image: sprite=null
    [Right]
      Transform: pos=(444.75,0.00,0.00), scale=(1,1,1)
      Image: sprite=null
    [Tags]
      Transform: pos=(0.00,16.00,0.00), scale=(1,1,1)
  [ManaPool]
    Transform: pos=(0.00,761.50,0.00), scale=(1,1,1)
    [BankButton]
      Transform: pos=(-415.60,0.00,0.00), scale=(1,1,1)
      Image: sprite=Button.128x64
      Button: interactable=True
      [Label]
        TextMeshProUGUI: "Bank
"
    [HeroBar]
      Transform: pos=(72.00,0.00,0.00), scale=(1,1,1)
      [Back]
        Image: sprite=action-bar-back-1
      [Fill]
        Image: sprite=action-bar-1
    [EnemyBar]
      Transform: pos=(72.00,-80.00,0.00), scale=(1,1,1)
      [Back]
        Image: sprite=action-bar-back-1
      [Fill]
        Image: sprite=action-bar-1
  [AbilityCastConfirm]
    AbilityCastConfirm
    Image: sprite=Back.512x128
    [Label]
      Transform: pos=(-192.00,0.00,0.00), scale=(1,1,1)
      TextMeshProUGUI: "Cast Heal"
    [CancelButton]
      Transform: pos=(135.36,0.00,0.00), scale=(1,1,1)
      Image: sprite=Cancel
      Button: interactable=True
      [Label]
        TextMeshProUGUI: "X"
    [CastButton]
      Transform: pos=(198.90,0.00,0.00), scale=(1,1,1)
      Image: sprite=Confirm
      Button: interactable=True
      [Label]
        TextMeshProUGUI: "Ok"
  [Card]
    Transform: pos=(540.00,-1170.69,0.00), scale=(1,1,1)
    [AbilityButtonContainer]
      Transform: pos=(-1080.00,256.00,0.00), scale=(1,1,1)
      Image: sprite=action-bar-1
    [Backdrop]
      Transform: pos=(-1080.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Back.1024x256
    [Portrait]
      Transform: pos=(-128.00,-128.00,0.00), scale=(1,1,1)
      Image: sprite=Mannequin
    [Title]
      Transform: pos=(-1055.00,236.00,0.00), scale=(1,1,1)
      TextMeshProUGUI: "Name"
    [Details]
      Transform: pos=(-1055.00,196.00,0.00), scale=(1,1,1)
      TextMeshProUGUI: "Description"
    [ArrowLeft]
      Transform: pos=(-320.00,50.00,0.00), scale=(1,1,1)
      Image: sprite=ArrowLeft
      Button: interactable=True
      [Text (TMP)]
        TextMeshProUGUI: ""
    [ArrowRight]
      Transform: pos=(-250.00,50.00,0.00), scale=(1,1,1)
      Image: sprite=ArrowRight
      Button: interactable=True
      [Text (TMP)]
        TextMeshProUGUI: ""
  [Portraits]
  [FadeOverlay]
    Transform: pos=(-0.50,0.50,0.00), scale=(99.00,99.00,1.00)
    Image: sprite=Black32x32
  [CutoutOverlay]
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
  [Clock]
    Transform: pos=(-540.00,1100.00,0.00), scale=(1,1,1)
    TextMeshProUGUI: "10:41 AM"
  [PauseButton]
    Transform: pos=(440.00,1100.00,0.00), scale=(1,1,1)
    Image: sprite=Pause
    Button: interactable=True
    [Label]
      TextMeshProUGUI: ""
  [PauseMenu]
    Image: sprite=Black32x32
    [ParallaxBackground]
      [Slide1]
        Transform: pos=(0.00,1170.69,0.00), scale=(1.00,1.00,0.00)
      [Slide2]
        Transform: pos=(0.00,-1170.69,0.00), scale=(1.00,1.00,0.00)
      [Slide3]
        Transform: pos=(0.00,-1170.69,0.00), scale=(1.00,1.00,0.00)
    [Inner]
      Transform: pos=(0.00,300.00,0.00), scale=(1,1,1)
      [SectionGameplay]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=null
        [Image]
          Image: sprite=Button.Bottom
        [Label]
          Transform: pos=(-387.00,24.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "Gameplay
"
      [ResumeButton]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=Back.512x128
        Button: interactable=True
        [Label]
          TextMeshProUGUI: "Resume"
      [RunAwayButton]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=Back.512x128
        Button: interactable=True
        [Label]
          TextMeshProUGUI: "Run Away"
      [QuickSaveGameButton]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=Back.512x128
        Button: interactable=True
        [Label]
          TextMeshProUGUI: "Quick Save"
      [CreateSaveGameButton]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=Back.512x128
        Button: interactable=True
        [Label]
          TextMeshProUGUI: "New Save"
      [RestartStageButton]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=Back.512x128
        Button: interactable=True
        [Label]
          TextMeshProUGUI: "Restart Stage"
      [SectionOptions]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=null
        [Image]
          Image: sprite=Button.Bottom
        [Label]
          Transform: pos=(-387.00,24.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "Options
"
      [PartyManagerButton]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=Back.512x128
        Button: interactable=True
        [Label]
          TextMeshProUGUI: "Party"
      [StageSelectButton]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=Back.512x128
        Button: interactable=True
        [Label]
          TextMeshProUGUI: "Stage Select"
      [SettingsButton]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=Back.512x128
        Button: interactable=True
        [Label]
          TextMeshProUGUI: "Settings
"
      [QuitButton]
        Transform: pos=(-400.00,-300.00,0.00), scale=(1,1,1)
        Image: sprite=Back.512x128
        Button: interactable=True
        [Label]
          TextMeshProUGUI: "Quit
"
[Canvas3D]
  Transform: pos=(0.00,0.00,99.00), scale=(0.01,0.01,0.01)
  Canvas: renderMode=WorldSpace, sortOrder=1
[Background]
  Transform: pos=(-0.04,0.00,0.00), scale=(0.50,0.66,1.00)
  SpriteRenderer: sprite=00, order=0
[Board]
  [BoardOverlay]
    Transform: pos=(0,0,0), scale=(9999.00,9999.00,1.00)
    SpriteRenderer: sprite=Black32x32, order=100
  [FocusIndicator]
    Transform: pos=(-1000.00,-1000.00,0.00), scale=(1,1,1)
    SpriteRenderer: sprite=focus-indicator, order=100
  [TargetModeOverlay]
    Transform: pos=(0,0,0), scale=(0.40,0.40,1.00)
    SpriteRenderer: sprite=TargetOverlay-v1, order=50
[Game]
  AudioSource: clip=null
  AudioSource: clip=null
  CameraManager
  TurnManager
  BoardManager
  ActorManager
  AttackLineManager
  CoinManager
  ConsoleManager
  LogManager
  AbilityManager
[PostProcessing]
  Transform: pos=(423.32,1706.41,0.00), scale=(1,1,1)
[Effects]
[ManaPoolManager]
```

## Summary

- **Total GameObjects**: 105
- **Managers**: ManaPoolManager
- **Cameras**: Main Camera, Overlay Camera
- **Canvases**: Canvas, Canvas3D
