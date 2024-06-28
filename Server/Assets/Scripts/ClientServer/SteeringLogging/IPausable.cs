namespace SteeringLogging
{
    /// <summary>
    /// This interface is implemented by all game objects that have to get into pause mode.
    /// This is especially relevant for time based automatic systems.
    /// </summary>
    public interface IPausable
    {
        /// <summary>
        /// Sets whether we are currently in pause mode or not.
        /// </summary>
        /// <param name="isInPauseMode">Indicates Pause mode.</param>
        void SetPauseMode(bool isInPauseMode);
    }
}
