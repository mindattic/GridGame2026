using Scripts.Factories;
using Scripts.Libraries;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using c = Scripts.Helpers.CanvasHelper;
using Scripts.Data.Actor;
using Scripts.Data.Items;
using Scripts.Data.Skills;
using Scripts.Effects;
using Scripts.Helpers;
using Scripts.Hub;
using Scripts.Instances;
using Scripts.Instances.Actor;
using Scripts.Instances.Board;
using Scripts.Instances.SynergyLine;
using Scripts.Inventory;
using Scripts.Managers;
using Scripts.Models;
using Scripts.Models.Actor;
using Scripts.Overworld;
using Scripts.Sequences;
using Scripts.Serialization;
using Scripts.Utilities;

namespace Scripts.Canvas
{
/// <summary>
/// CANVASPARTICLEEMITTER - Spawns decorative UI particles (leaves, etc).
/// 
/// PURPOSE:
/// Creates ambient floating particle effects on UI canvases.
/// Used for menu backgrounds, victory screens, etc.
/// 
/// VISUAL EFFECT:
/// ```
/// 🍂  🍁    🍂
///   🍁  🍂    🍁  ← Leaves drift across screen
///     🍂   🍁
/// ```
/// 
/// PARTICLE BEHAVIOR:
/// - Spawns at random X positions
/// - Drifts with rotation and fall speed
/// - Uses sprites from SpriteLibrary.Leaves
/// - Auto-destroys when off-screen
/// 
/// CONFIGURATION:
/// - spawnIntervalMin/Max: Time between spawns
/// - speedMin/Max: Horizontal drift speed
/// - rotationFocusMin/Max: Rotation speed
/// - fallFocusMin/Max: Downward speed
/// - scaleMin/Max: Size range
/// - prewarmCount: Initial particles at start
/// 
/// PREWARM:
/// Spawns initial particles at Start() so screen isn't empty.
/// 
/// RELATED FILES:
/// - CanvasParticleFactory.cs: Creates particle GameObjects
/// - SpriteLibrary.cs: Provides leaf sprites
/// - TitleScreenManager.cs: Uses for menu ambiance
/// </summary>
public class CanvasParticleEmitter : MonoBehaviour
{
    #region Configuration

    private float spawnIntervalMin;
    private float spawnIntervalMax;
    private float speedMin;
    private float speedMax;
    private float rotationFocusMin;
    private float rotationFocusMax;
    private float fallFocusMin;
    private float fallFocusMax;
    private float scaleMin;
    private float scaleMax;
    private int prewarmCount;
    private Sprite[] sprites;
    private float xMin;
    private float xMax;
    private float yMin;
    private float yMax;

    #endregion

    #region Initialization

    /// <summary>Configures default particle parameters and loads leaf sprites.</summary>
    private void Awake()
    {
        xMin = -Screen.width;
        xMax = Screen.width;
        yMin = -200;
        yMax = 200;
        spawnIntervalMin = 0.1f;
        spawnIntervalMax = 0.25f;
        speedMin = 300;
        speedMax = 600;
        yMin = -1000;
        yMax = 1000;
        rotationFocusMin = 70;
        rotationFocusMax = 100;
        fallFocusMin = 40;
        fallFocusMax = 100;
        scaleMin = 0.3f;
        scaleMax = 0.4f;
        prewarmCount = 20;

        sprites = new Sprite[]
        {
            SpriteLibrary.Leaves["Leaf1"],
            SpriteLibrary.Leaves["Leaf2"],
            SpriteLibrary.Leaves["MapleLeaf1"],
            SpriteLibrary.Leaves["MapleLeaf2"],
        };

    }

    /// <summary>Spawns initial prewarm particles and begins the continuous spawn loop.</summary>
    void Start()
    {
        PrewarmParticles();
        StartCoroutine(SpawnImagesRoutine());

    #endregion
    }

    /// <summary>Spawns the initial batch of particles distributed across the screen.</summary>
    private void PrewarmParticles()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            SpawnImage(preheat: true);
        }
    }

    /// <summary>Continuously spawns new particles at random intervals.</summary>
    private IEnumerator SpawnImagesRoutine()
    {
        while (true)
        {
            SpawnImage();
            var spawnInterval = RNG.Float(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    /// <summary>Creates a single canvas particle with random sprite, position, rotation, and drift parameters.</summary>
    private void SpawnImage(bool preheat = false)
    {
        // Use factory instead of Instantiate(prefab)
        GameObject newImage = CanvasParticleFactory.Create(c.CanvasRect);
        RectTransform rect = newImage.GetComponent<RectTransform>();
        Image image = newImage.GetComponent<Image>();
        if (rect == null || image == null)
            return;

        // SelectProfile a random sprite from the sprite sheet
        image.sprite = sprites.ShuffleFirst();

        // SelectProfile start position
        float startX = preheat ? RNG.Float(xMin, xMax) : xMin; // Prewarm particles start mid-flight
        float startY = RNG.Float(yMin, yMax);
        rect.anchoredPosition = new Vector2(startX, startY);

        // SelectProfile random rotation speed, Move, and scale
        float rotRange = RNG.Float(rotationFocusMin, rotationFocusMax);
        float rotWildcard = RNG.Int(1, 3) == 1 ? RNG.Float(1, 3f) : 1f;
        float rotDirection = RNG.Boolean ? -1f : 1f;

        float rotationFocus = rotRange * rotWildcard * rotDirection;
        float horizontalFocus = RNG.Float(speedMin, speedMax);
        float fallFocus = RNG.Float(fallFocusMin, fallFocusMax);
        float scale = RNG.Float(scaleMin, scaleMax);
        rect.localScale = new Vector3(scale, scale, 1f);

        CanvasParticleInstance instance = newImage.AddComponent<CanvasParticleInstance>();
        instance.parent = transform;
        instance.Initialize(rotationFocus, horizontalFocus, fallFocus);
    }
}

}
