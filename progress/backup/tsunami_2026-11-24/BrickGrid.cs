namespace BrickPanic
{
    /// <summary>World sizes of the brick grid. Kenney bricks are real size (1 stud = 8 mm),
    /// so they are imported larger: 1 stud = 0.5 m, 1 brick = 0.6 m high, 1 plate = 0.2 m high.</summary>
    public static class BrickGrid
    {
        public const float Stud = 0.5f;          // metres per stud
        public const float BrickHeight = 0.6f;   // body height of a full brick (studs on top not counted)
        public const float PlateHeight = 0.2f;   // body height of a plate
        public const float KenneyStud = 0.08f;   // stud pitch inside the Kenney FBX files
        public const float ImportScale = Stud / KenneyStud; // 6.25
    }
}
