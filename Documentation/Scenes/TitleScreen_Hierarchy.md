# Scene: TitleScreen
Path: `Assets/Scenes/TitleScreen.unity`
Build Index: 10
Root Object Count: 4

---

## Hierarchy

```
[Main Camera]
  Transform: pos=(0.00,0.00,-1.00), scale=(1.00,1.00,0.00)
  Camera: orthographic=True, depth=-1
  CameraManager
[EventSystem]
[Canvas]
  Transform: pos=(589.50,1278.00,0.00), scale=(1.09,1.09,1.09)
  Canvas: renderMode=ScreenSpaceOverlay, sortOrder=0
  Image: sprite=GunMetal16x16
  [Panel]
    [Backdrop] [INACTIVE]
      Image: sprite=null
    [ContinueButton]
      Transform: pos=(-540.00,-300.00,0.00), scale=(1,1,1)
      Image: sprite=Back.512x128
      Button: interactable=True
      [Label]
        TextMeshProUGUI: "Continue"
    [LoadGameButton]
      Transform: pos=(-540.00,-300.00,0.00), scale=(1,1,1)
      Image: sprite=Back.512x128
      Button: interactable=True
      [Label]
        TextMeshProUGUI: "Load Game
"
    [EndlessModeButton]
      Transform: pos=(-540.00,-300.00,0.00), scale=(1,1,1)
      Image: sprite=Back.512x128
      Button: interactable=True
      [Label]
        TextMeshProUGUI: "Crucible"
    [PartyManagerButton]
      Transform: pos=(-540.00,-300.00,0.00), scale=(1,1,1)
      Image: sprite=Back.512x128
      Button: interactable=True
      [Label]
        TextMeshProUGUI: "Party
"
    [SettingsButton]
      Transform: pos=(-540.00,-300.00,0.00), scale=(1,1,1)
      Image: sprite=Back.512x128
      Button: interactable=True
      [Label]
        TextMeshProUGUI: "Settings"
    [CreditsButton]
      Transform: pos=(-540.00,-300.00,0.00), scale=(1,1,1)
      Image: sprite=Back.512x128
      Button: interactable=True
      [Label]
        TextMeshProUGUI: "Credits"
  [ProfileButton]
    Transform: pos=(0.00,-1060.69,0.00), scale=(1,1,1)
    Image: sprite=UserIconWhite_0
    Button: interactable=True
    [Label]
      Transform: pos=(0.00,-68.00,0.00), scale=(1,1,1)
      TextMeshProUGUI: "Profile Name"
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
[TitleScreenManager]
```

## Summary

- **Total GameObjects**: 27
- **Managers**: TitleScreenManager
- **Cameras**: Main Camera
- **Canvases**: Canvas
