
/// <summary>
/// Platform specific defines.
/// </summary>
public static  class Platform
{
    /// <summary>
    /// We are the host.
    /// </summary>
    public const bool IsHost = false;

    /// <summary>
    /// Flags that we want to play audio on the platform. Usually not on the host.
    /// Separated from host information for debug purposes.
    /// </summary>
    public const bool HasAudio = true;
}
