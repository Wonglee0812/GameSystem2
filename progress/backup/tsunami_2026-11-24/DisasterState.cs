namespace BrickPanic
{
    /// <summary>Shared disaster strength that other systems read (wear, AI panic later).
    /// 0 = calm, 1 = full strength of a level-1 disaster; higher levels go above 1.</summary>
    public static class DisasterState
    {
        public static float Intensity;
    }
}
