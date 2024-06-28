/// <summary>
/// This is an interface for all components, that must get informed, when a client
/// got disconnected. This is typical for components, that store the client id.
/// </summary>
internal interface IDisconnectable
{
    /// <summary>
    /// Gets called, when a client got disconnected (invoked on server).
    /// </summary>
    /// <param name="clientId">The id of the client.</param>
    void InformClientDisconnection(ulong clientId);
}