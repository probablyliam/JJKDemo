namespace JJKDemo.Combat
{
    /// <summary>
    /// Lightweight global gate that shared player controllers (the third-person mover and
    /// camera rig) read to suspend look/move input during non-gameplay states such as a
    /// pre-trial countdown or an end screen.
    ///
    /// It deliberately knows nothing about any specific game mode — a mode manager simply
    /// flips <see cref="InputEnabled"/>, so the player and camera stay mode-agnostic.
    /// </summary>
    public static class GameplayInputGate
    {
        /// <summary>When false, the player mover and camera rig ignore movement/look input.</summary>
        public static bool InputEnabled = true;

        /// <summary>Restore the default (input allowed). Called when no mode is driving the gate.</summary>
        public static void Reset()
        {
            InputEnabled = true;
        }
    }
}
