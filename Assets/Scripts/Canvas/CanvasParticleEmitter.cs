using Assets.Scripts.Factories;
using Assets.Scripts.Libraries;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using c = Assets.Helpers.CanvasHelper;

public class CanvasParticleEmitter : MonoBehaviour
{
    private float spawnIntervalMin; // Time between spawns
    private float spawnIntervalMax; // Time between spawns
    private float speedMin;
    private float speedMax;
    private float rotationFocusMin; // Min rotation speed
    private float rotationFocusMax; // Max rotation speed
    private float fallFocusMin; // Minimum downward speed
    private float fallFocusMax; // Maximum downward speed
    private float scaleMin; // Minimum scale
    private float scaleMax; // Maximum scale
    private int prewarmCount; // Index of particles to spawn on start
    private Sprite[] sprites; // Array of sprites from the sprite sheet
    private float xMin;
    private float xMax;
    private float yMin;
    private float yMax;

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

    void Start()
    {
        PrewarmParticles();  // Show initial particles
        StartCoroutine(SpawnImagesRoutine());
    }

    private void PrewarmParticles()
    {
        for (int i = 0; i < prewarmCount; i++)
        {
            SpawnImage(preheat: true);
        }
    }

    private IEnumerator SpawnImagesRoutine()
    {
        while (true)
        {
            SpawnImage();
            var spawnInterval = RNG.Float(spawnIntervalMin, spawnIntervalMax);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

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
