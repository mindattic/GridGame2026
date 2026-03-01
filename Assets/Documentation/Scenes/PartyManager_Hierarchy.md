# Scene: PartyManager
Path: `Assets/Scenes/PartyManager.unity`
Build Index: 3
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
  Image: sprite=GunMetal16x16
  [BackButton]
    Transform: pos=(-402.37,999.59,0.00), scale=(1,1,1)
    Image: sprite=Button.128x64
    Button: interactable=True
    [Label]
      TextMeshProUGUI: "Game"
  [Title]
    Transform: pos=(0.00,-129.31,0.00), scale=(1,1,1)
    TextMeshProUGUI: "Hero"
  [StatsDisplay]
    Transform: pos=(-256.00,600.00,0.00), scale=(1,1,1)
    [Background]
      Transform: pos=(0.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Background.Menu
    [Top]
      Transform: pos=(0.00,200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Top
    [Right]
      Transform: pos=(200.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Right
    [Bottom]
      Transform: pos=(0.00,-200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Bottom
    [Left]
      Transform: pos=(-200.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Left
    [TopLeft]
      Transform: pos=(-200.00,200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.TopLeft
    [TopRight]
      Transform: pos=(200.00,200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.TopRight
    [BottomRight]
      Transform: pos=(200.00,-200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.BottomRight
    [BottomLeft]
      Transform: pos=(-200.00,-200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.BottomLeft
    [Panel]
      Transform: pos=(-32.00,153.09,0.00), scale=(1,1,1)
      Image: sprite=UserIconWhite_0
      [LVL]
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "LVL"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
      [HP]
        Transform: pos=(0.00,-50.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "HP"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
      [STR]
        Transform: pos=(0.00,-100.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "STR"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
      [VIT]
        Transform: pos=(0.00,-150.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "VIT"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
      [AGI]
        Transform: pos=(0.00,-200.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "AGI"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
      [SPD]
        Transform: pos=(0.00,-250.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "SPD"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
      [STA]
        Transform: pos=(0.00,-250.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "SPD"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
      [INT]
        Transform: pos=(0.00,-250.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "SPD"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
      [WIS]
        Transform: pos=(0.00,-250.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "SPD"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
      [LCK]
        Transform: pos=(0.00,-300.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(-100.00,-2.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "LCK"
        [Bar]
          [Back]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-back-3
          [Fill]
            Transform: pos=(-50.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=health-bar-1
          [Front] [INACTIVE]
            Transform: pos=(-50.00,16.00,0.00), scale=(1,1,1)
            Image: sprite=null
        [Value]
          Transform: pos=(165.00,0.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "0"
  [EquipmentDisplay]
    Transform: pos=(256.00,600.00,0.00), scale=(1,1,1)
    [Background]
      Transform: pos=(0.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Background.Menu
    [Top]
      Transform: pos=(0.00,200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Top
    [Right]
      Transform: pos=(200.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Right
    [Bottom]
      Transform: pos=(0.00,-200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Bottom
    [Left]
      Transform: pos=(-200.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Left
    [TopLeft]
      Transform: pos=(-200.00,200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.TopLeft
    [TopRight]
      Transform: pos=(200.00,200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.TopRight
    [BottomRight]
      Transform: pos=(200.00,-200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.BottomRight
    [BottomLeft]
      Transform: pos=(-200.00,-200.00,0.00), scale=(1,1,1)
      Image: sprite=Button.BottomLeft
    [Panel]
      Transform: pos=(-232.00,200.00,0.00), scale=(1,1,1)
      Image: sprite=Background
      [Weapons]
        Transform: pos=(48.00,-128.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(105.00,76.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "Weapon"
        [Dropdown]
          Transform: pos=(4.00,48.00,0.00), scale=(1,1,1)
          Image: sprite=Background.DarkBlue
          [Label]
            Transform: pos=(177.50,-0.50,0.00), scale=(1,1,1)
            TextMeshProUGUI: "-"
          [Arrow]
            Transform: pos=(355.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=DropdownArrow
          [Template] [INACTIVE]
            Transform: pos=(185.00,-22.00,0.00), scale=(1,1,1)
            Image: sprite=Background.DarkBlue
            [Viewport]
              Transform: pos=(-185.00,0.00,0.00), scale=(1,1,1)
              Image: sprite=Background.DarkBlue
              [Content]
                Transform: pos=(176.00,0.00,0.00), scale=(1,1,1)
                [Item]
                  Transform: pos=(0.00,-24.00,0.00), scale=(1,1,1)
                  [Item Background]
                    Image: sprite=Background.LightBlue
                  [Item Checkmark] [INACTIVE]
                    Transform: pos=(-166.00,0.00,0.00), scale=(1,1,1)
                    Image: sprite=Checkmark
                  [Item Icon]
                    Transform: pos=(-144.00,2.00,0.00), scale=(1,1,1)
                    Image: sprite=Sword
                  [Item Label]
                    Transform: pos=(34.00,0.00,0.00), scale=(1,1,1)
                    TextMeshProUGUI: "Option A"
            [Scrollbar]
              Transform: pos=(185.00,0.00,0.00), scale=(1,1,1)
              Image: sprite=Background.DarkBlue
              [Sliding Area]
                Transform: pos=(-10.00,-256.00,0.00), scale=(1,1,1)
                [Handle]
                  Transform: pos=(0.00,-196.80,0.00), scale=(1,1,1)
                  Image: sprite=UISprite
      [Armor]
        Transform: pos=(48.00,-215.00,0.00), scale=(1,1,1)
        [Label]
          Transform: pos=(105.00,76.00,0.00), scale=(1,1,1)
          TextMeshProUGUI: "Armor"
        [Dropdown]
          Transform: pos=(4.00,48.00,0.00), scale=(1,1,1)
          Image: sprite=Background.DarkBlue
          [Label]
            Transform: pos=(177.50,-0.50,0.00), scale=(1,1,1)
            TextMeshProUGUI: "-"
          [Arrow]
            Transform: pos=(355.00,0.00,0.00), scale=(1,1,1)
            Image: sprite=DropdownArrow
          [Template] [INACTIVE]
            Transform: pos=(185.00,-22.00,0.00), scale=(1,1,1)
            Image: sprite=Background.DarkBlue
            [Viewport]
              Transform: pos=(-185.00,0.00,0.00), scale=(1,1,1)
              Image: sprite=Background.DarkBlue
              [Content]
                Transform: pos=(176.00,0.00,0.00), scale=(1,1,1)
                [Item]
                  Transform: pos=(0.00,-24.00,0.00), scale=(1,1,1)
                  [Item Background]
                    Image: sprite=Background.LightBlue
                  [Item Checkmark] [INACTIVE]
                    Transform: pos=(-166.00,0.00,0.00), scale=(1,1,1)
                    Image: sprite=Checkmark
                  [Item Icon]
                    Transform: pos=(-144.00,2.00,0.00), scale=(1,1,1)
                    Image: sprite=Sword
                  [Item Label]
                    Transform: pos=(34.00,0.00,0.00), scale=(1,1,1)
                    TextMeshProUGUI: "Option A"
            [Scrollbar]
              Transform: pos=(185.00,0.00,0.00), scale=(1,1,1)
              Image: sprite=Background.DarkBlue
              [Sliding Area]
                Transform: pos=(-10.00,-256.00,0.00), scale=(1,1,1)
                [Handle]
                  Transform: pos=(0.00,-196.80,0.00), scale=(1,1,1)
                  Image: sprite=UISprite
  [PartyMemberCountLabel]
    Transform: pos=(469.50,1008.59,0.00), scale=(1,1,1)
    TextMeshProUGUI: "0/6"
  [AddRemovePartyMemberButton]
    Image: sprite=Back.512x128
    Button: interactable=True
    [Background]
      Transform: pos=(0.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Background.Menu
    [Top]
      Transform: pos=(0.00,64.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Top
    [Right]
      Transform: pos=(280.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Right
    [Bottom]
      Transform: pos=(0.00,-64.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Bottom
    [Left]
      Transform: pos=(-280.00,0.00,0.00), scale=(1,1,1)
      Image: sprite=Button.Left
    [TopLeft]
      Transform: pos=(-280.00,64.00,0.00), scale=(1,1,1)
      Image: sprite=Button.TopLeft
    [TopRight]
      Transform: pos=(280.00,64.00,0.00), scale=(1,1,1)
      Image: sprite=Button.TopRight
    [BottomRight]
      Transform: pos=(280.00,-64.00,0.00), scale=(1,1,1)
      Image: sprite=Button.BottomRight
    [BottomLeft]
      Transform: pos=(-280.00,-64.00,0.00), scale=(1,1,1)
      Image: sprite=Button.BottomLeft
    [Label]
      TextMeshProUGUI: "Add to Party"
  [RosterCarousel]
    Transform: pos=(0.00,-170.69,0.00), scale=(1,1,1)
    Image: sprite=party-manager-gradient
    [Panel]
      Transform: pos=(0.00,-500.00,0.00), scale=(1,1,1)
      Image: sprite=Background
  [TestTooltip]
    Transform: pos=(0.00,137.53,0.00), scale=(1,1,1)
    Image: sprite=UISprite
    Button: interactable=True
    [Text (TMP)]
      TextMeshProUGUI: "Button"
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
[PartyManager]
  Transform: pos=(-1056.86,1406.52,5.25), scale=(1,1,1)
```

## Summary

- **Total GameObjects**: 155
- **Managers**: PartyManager
- **Cameras**: Main Camera
- **Canvases**: Canvas
