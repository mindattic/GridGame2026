# Scene: Settings
Path: `Assets/Scenes/Settings.unity`
Build Index: 8
Root Object Count: 4

---

## Hierarchy

```
[Main Camera]
  Transform: pos=(0.00,0.00,-10.00), scale=(1,1,1)
  Camera: orthographic=True, depth=-1
[EventSystem]
[Canvas]
  Transform: pos=(589.50,1278.00,0.00), scale=(1.09,1.09,1.09)
  Canvas: renderMode=ScreenSpaceOverlay, sortOrder=0
  Image: sprite=GunMetal16x16
  [BackButton]
    Transform: pos=(-420.00,970.69,0.00), scale=(1,1,1)
    Image: sprite=Green-Button_0
    Button: interactable=True
    [Label]
      TextMeshProUGUI: "Back"
  [Title]
    Transform: pos=(0.00,1042.69,0.00), scale=(1,1,1)
    TextMeshProUGUI: "Settings
"
  [ScrollView]
    Image: sprite=null
    [Viewport]
      Transform: pos=(-540.00,914.69,0.00), scale=(1,1,1)
      Image: sprite=UIMask
      [Content]
        Transform: pos=(531.50,0.00,0.00), scale=(1,1,1)
    [Scrollbar Horizontal]
      Transform: pos=(-540.00,-914.69,0.00), scale=(1,1,1)
      Image: sprite=Background
      [Sliding Area]
        Transform: pos=(531.50,10.00,0.00), scale=(1,1,1)
        [Handle]
          Image: sprite=UISprite
    [Scrollbar Vertical]
      Transform: pos=(540.00,914.69,0.00), scale=(1,1,1)
      Image: sprite=Background
      [Sliding Area]
        Transform: pos=(-10.00,-906.19,0.00), scale=(1,1,1)
        [Handle]
          Image: sprite=UISprite
  [DefaultsButton]
    Transform: pos=(-467.31,-968.70,0.00), scale=(1,1,1)
    Image: sprite=Button.128x64
    Button: interactable=True
    [Text (TMP)]
      TextMeshProUGUI: "Defaults"
  [SaveButton]
    Transform: pos=(460.20,-968.70,0.00), scale=(1,1,1)
    Image: sprite=Button.128x64
    Button: interactable=True
    [Text (TMP)]
      TextMeshProUGUI: "Save
"
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
[SettingsManager]
  Transform: pos=(-0.83,-1.00,756.67), scale=(1,1,1)
```

## Summary

- **Total GameObjects**: 27
- **Managers**: SettingsManager
- **Cameras**: Main Camera
- **Canvases**: Canvas
