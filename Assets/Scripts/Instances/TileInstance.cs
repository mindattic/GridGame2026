using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using g = Assets.Helpers.GameHelper;

public class TileInstance : MonoBehaviour
{

    public bool IsOccupied => g.Actors.All.Any(x => x.IsPlaying && x.location == location);

    public ActorInstance Occupier => g.Actors.All.FirstOrDefault(x => x.location == location);


    public System.Action<Vector2Int> onSelectedPlayerLeaveLocation;
    public System.Action<Vector2Int> onSelectedPlayerEnterLocation;


    public string Name
    {
        get => name;
        set => Name = value;
    }
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
    public Sprite sprite
    {
        get => spriteRenderer.sprite;
        set => spriteRenderer.sprite = value;
    }
    public Color color
    {
        get => spriteRenderer.color;
        set => spriteRenderer.color = value;
    }
    public bool IsSameColumn(Vector2Int other) => this.location.x == other.x;
    public bool IsSameRow(Vector2Int other) => this.location.y == other.y;
    public bool IsNorthOf(Vector2Int other) => IsSameColumn(other) && this.location.y == other.y - 1;
    public bool IsEastOf(Vector2Int other) => IsSameRow(other) && this.location.x == other.x + 1;
    public bool IsSouthOf(Vector2Int other) => IsSameColumn(other) && this.location.y == other.y + 1;
    public bool IsWestOf(Vector2Int other) => IsSameRow(other) && this.location.x == other.x - 1;
    public bool IsNorthWestOf(Vector2Int other) => this.location.x == other.x - 1 && this.location.y == other.y - 1;
    public bool IsNorthEastOf(Vector2Int other) => this.location.x == other.x + 1 && this.location.y == other.y - 1;
    public bool IsSouthWestOf(Vector2Int other) => this.location.x == other.x - 1 && this.location.y == other.y + 1;
    public bool IsSouthEastOf(Vector2Int other) => this.location.x == other.x + 1 && this.location.y == other.y + 1;
    public bool IsAdjacentTo(Vector2Int other) => (IsSameColumn(other) || IsSameRow(other)) && Vector2Int.Distance(this.location, other) == 1;


    //Fields
    public Vector2Int location;
    public SpriteRenderer spriteRenderer;

    //Method which is used for initialization tasks that need to occur before the game starts 
    public void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    public void Start()
    {
        
    }

    public void Initialize(int col, int row)
    {
        location = new Vector2Int(col, row);
        position = Geometry.CalculatePositionByLocation(location);
        transform.localScale = g.TileScale;
    }
}
