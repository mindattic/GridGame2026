using UnityEngine;
using g = Assets.Helpers.GameHelper;


public enum CoinValue
{
    Silver = 1,
    Gold = 10,
    Red = 20
}

public class CoinInstance : MonoBehaviour
{
    // Fields
    [SerializeField] public AnimationCurve linearCurve;
    [SerializeField] public AnimationCurve slopeCurve;
    [SerializeField] public AnimationCurve sineCurve;

    private SpriteRenderer spriteRenderer;
    private ParticleSystem particles;

    private float scaleMultiplier = 0.05f;
    private float startDuration = 0.2f;
    private float moveDuration = 0.6f;

    private float timeElapsed = 0.0f;

    private Vector3 start;
    private Vector3 end;

    private CoinState state;

    private float t, x, y, z;

    private AnimationCurve cX;
    private AnimationCurve cY;

    // BounceRoutine simulation fields
    private Vector3 velocity;
    private float gravity = -20f;
    private int bouncesRemaining = 6;
    private float bounceDamp = 0.5f;
    private float minBounceVelocity = 1.5f;

    // Randomized floor height for bounce realism
    private float groundOffsetY = 0f;

    // Properties
    public Transform parent
    {
        get => gameObject.transform.parent;
        set => gameObject.transform.SetParent(value, true);
    }

    public Vector3 position
    {
        get => gameObject.transform.position;
        set => gameObject.transform.position = value;
    }

    public Quaternion rotation
    {
        get => gameObject.transform.rotation;
        set => gameObject.transform.rotation = value;
    }

    public Vector3 scale
    {
        get => gameObject.transform.localScale;
        set => gameObject.transform.localScale = value;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        particles = GetComponent<ParticleSystem>();
        transform.localScale = g.TileScale * scaleMultiplier;
    }

    public void Spawn(Vector3 position)
    {
        start = position;
        end = g.CoinCounter.GetIconWorldPosition();

        timeElapsed = 0f;
        startDuration += RNG.Float(0, 0.2f);
        moveDuration += RNG.Float(0, 0.2f);

        cX = RandomCurve();
        cY = RandomCurve();

        // Explosion impulse
        float angle = RNG.Float(0, 2 * Mathf.PI);
        float force = RNG.Float(0.5f, 1.5f);
        velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * force;
        velocity.y = Mathf.Abs(velocity.y) + 4f; // always upward
        bouncesRemaining = RNG.Int(3, 6);

        // Add slight variation to the bounce floor
        float maxOffset = g.TileSize * 0.6f;
        groundOffsetY = RNG.Float(-maxOffset, 0);

        transform.position = start;
        state = CoinState.Bounce;
    }

    private AnimationCurve RandomCurve()
    {
        int r = RNG.Int(1, 3);
        if (r == 1) return linearCurve;
        if (r == 2) return slopeCurve;
        return sineCurve;
    }

    public void Update()
    {
        switch (state)
        {
            case CoinState.Bounce:
                Bounce();
                break;

            case CoinState.Seek:
                Seek();
                break;

            case CoinState.Despawn:
                Despawn();
                break;
        }

        timeElapsed += Time.deltaTime;
    }

    private void Bounce()
    {
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Seek coin
        Vector3 pos = transform.position;
        pos += velocity * Time.deltaTime;

        // BounceRoutine on ground (assume y = start.y + groundOffsetY is ground level)
        if (pos.y <= start.y + groundOffsetY)
        {
            if (bouncesRemaining > 0 && Mathf.Abs(velocity.y) > minBounceVelocity)
            {
                pos.y = start.y + groundOffsetY;
                velocity.y *= -bounceDamp;
                bouncesRemaining--;
            }
            else
            {
                // Final settle
                pos.y = start.y + groundOffsetY;
                velocity = Vector3.zero;
                timeElapsed = 0f;
                start = pos;
                end = g.CoinCounter.GetIconWorldPosition();
                state = CoinState.Seek;
                return;
            }
        }

        transform.position = pos;
    }

    private void Seek()
    {
        t = Mathf.Clamp01(timeElapsed / moveDuration);
        x = Mathf.Lerp(start.x, end.x, sineCurve.Evaluate(t));
        y = Mathf.Lerp(start.y, end.y, sineCurve.Evaluate(t));
        z = transform.position.z;
        transform.position = new Vector3(x, y, z);
        if (timeElapsed >= moveDuration)
        {
            timeElapsed = 0;
            state = CoinState.Despawn;
        }
    }

    private void Despawn()
    {
        spriteRenderer.enabled = false;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        g.TotalCoins++;
        g.CoinCounter.value.text = g.TotalCoins.ToString("D7");
        g.AudioManager.Play($"Click");
        Destroy(gameObject);
    }
}