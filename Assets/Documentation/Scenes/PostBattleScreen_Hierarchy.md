# Scene: PostBattleScreen
Path: `Assets/Scenes/PostBattleScreen.unity`
Build Index: 12
Root Object Count: 5

---

## Hierarchy

```
[Main Camera]
  Transform: pos=(0.00,0.00,-10.00), scale=(1,1,1)
  Camera: orthographic=True, depth=-1
[EventSystem]
[Background] [INACTIVE]
  Transform: pos=(-0.04,0.00,0.00), scale=(0.50,0.66,1.00)
  SpriteRenderer: sprite=null, order=-1
[Canvas]
  Transform: pos=(589.50,1278.00,0.00), scale=(1.09,1.09,1.09)
  Canvas: renderMode=ScreenSpaceOverlay, sortOrder=0
  [Background]
    Transform: pos=(0,0,0), scale=(0.56,0.56,0.56)
    Image: sprite=null
  [Title]
    Transform: pos=(0.00,970.69,0.00), scale=(0.56,0.56,0.56)
    TextMeshProUGUI: "Victory"
  [ScrollView]
    Transform: pos=(0.00,914.69,0.00), scale=(1,1,1)
    Image: sprite=Background
    [Viewport]
      Transform: pos=(-540.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=UIMask
      [Content]
        Transform: pos=(0.00,0.00,0.00), scale=(1,1,1)
    [Scrollbar Horizontal]
      Transform: pos=(-540.00,-1829.37,0.00), scale=(1,1,1)
      Image: sprite=Background
      [Sliding Area]
        Transform: pos=(531.50,10.00,0.00), scale=(1,1,1)
        [Handle]
          Image: sprite=UISprite
    [Scrollbar Vertical]
      Transform: pos=(540.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Background
      [Sliding Area]
        Transform: pos=(-10.00,-906.19,0.00), scale=(1,1,1)
        [Handle]
          Image: sprite=UISprite
  [BottomBar]
    Transform: pos=(0.00,-945.69,0.00), scale=(0.56,0.56,0.56)
    Image: sprite=null
    [NextButton]
      Transform: pos=(920.00,0.00,0.00), scale=(1,1,1)
      Button: interactable=True
      Image: sprite=null
      [Label]
        Transform: pos=(-130.00,0.00,0.00), scale=(1,1,1)
        TextMeshProUGUI: "Next"
  [FadeOverlay]
    Transform: pos=(-0.73,0.73,0.00), scale=(144.79,144.79,1.46)
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
[PostBattleManager]
  Transform: pos=(-1056.86,1406.52,5.25), scale=(1,1,1)
```

## Summary

- **Total GameObjects**: 26
- **Managers**: PostBattleManager
- **Cameras**: Main Camera
- **Canvases**: Canvas
