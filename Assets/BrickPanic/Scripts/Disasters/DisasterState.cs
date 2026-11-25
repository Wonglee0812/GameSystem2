namespace BrickPanic
{
    /// <summary>Shared disaster strength that other systems read (wear, AI panic later).
    /// 0 = calm, 1 = full strength of a level-1 disaster; higher levels go above 1.</summary>
    public static class DisasterState
    {
        public static float Intensity;

        /// <summary>Multiplier for the time-based joint wear (PhysicsActivator). 1 = normal. The tsunami sets 0 so only
        /// real contact forces break joints.</summary>
        public static float WearScale = 1f;
    }
}
