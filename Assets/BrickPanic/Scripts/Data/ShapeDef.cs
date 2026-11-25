using UnityEngine;

namespace BrickPanic
{
    public enum ShapeKind { Brick, Plate, Slope, Round }

    /// <summary>One brick shape (mesh + size in studs). Shape x Material makes every brick.</summary>
    [CreateAssetMenu(menuName = "Brick Panic/Shape", fileName = "SHP_New")]
    public class ShapeDef : ScriptableObject
    {
        [Tooltip("Name shown to the player. Default: Brick 2x4.")]
        public string displayName = "Brick 2x4";
        [Tooltip("Brick = full height, Plate = a third of the height, Slope and Round = full height with a special look.")]
        public ShapeKind kind = ShapeKind.Brick;
        [Tooltip("Mesh from the Kenney Brick Kit (imported at scale 6.25, so 1 stud = 0.5 m).")]
        public Mesh mesh;
        [Tooltip("Size in studs: x = along the X axis, y = along the Z axis. Default 4 x 2 (a 2x4 brick).")]
        public Vector2Int studs = new Vector2Int(4, 2);

        public bool IsPlate => kind == ShapeKind.Plate;
        public float Height => IsPlate ? BrickGrid.PlateHeight : BrickGrid.BrickHeight;
        public float HeightFactor => IsPlate ? 1f / 3f : 1f;
        public int StudCount => studs.x * studs.y;
        /// <summary>Size of the body in metres (studs on top not counted).</summary>
        public Vector3 BodySize => new Vector3(studs.x * BrickGrid.Stud, Height, studs.y * BrickGrid.Stud);

        public float Price(MaterialDef m) => m.pricePerStud * StudCount * HeightFactor;
        public float Mass(MaterialDef m) => m.massPerStud * StudCount * HeightFactor;
    }
}
