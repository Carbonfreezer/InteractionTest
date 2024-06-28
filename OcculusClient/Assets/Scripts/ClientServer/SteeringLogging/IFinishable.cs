namespace SteeringLogging
{
    /// <summary>
    /// Interface implementation for game components, that want to finish the game. This means that the logging gets deactivated.
    /// </summary>
    public interface IFinishable
    {
        /// <summary>
        /// Contains the finish status of the game.
        /// </summary>
        public enum FinishStatus
        {
            /// <summary>
            /// The game is still ongoing.
            /// </summary>
            ContinueGame,

            /// <summary>
            /// We want to stop but want to make sure, that the actual state got logged.
            /// </summary>
            StopDelay,

            /// <summary>
            /// We want to stop immediately.
            /// </summary>
            StopImmediate
        }
        /// <summary>
        /// Flags that the game is finished and that a log should be generated.
        /// </summary>
        FinishStatus IsFinished { get; }
    }
}