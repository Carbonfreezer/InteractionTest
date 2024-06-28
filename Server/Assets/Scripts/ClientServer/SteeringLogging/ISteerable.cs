namespace SteeringLogging
{
    /// <summary>
    /// Interface for all game components, that must be informed of whether a mission has started or whether it has handed
    /// and does not accept any more inputs.
    /// </summary>
    public interface ISteerable
    {
        /// <summary>
        /// Gets called when a mission starts. It is meant for activation of interaction and eventual preparation. 
        /// </summary>
        /// <param name="requiresReset">Flags that internal information should be reset.</param>
        void StartMission(bool requiresReset);

        /// <summary>
        /// Gets called when the mission debriefing should be started.
        /// </summary>
        void StartDebrief();
    }
}