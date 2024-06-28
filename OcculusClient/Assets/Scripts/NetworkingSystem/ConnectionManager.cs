using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the connection over the network. If started as a server its sends regularly a UDP package as a broadcast
/// to be discovered as a server. 
/// </summary>
[RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
public class ConnectionManager : MonoBehaviour
{
    #region Portinformation adjusted per game

    /// <summary>
    /// The port we send our discovery signal from.
    /// </summary>
    private const int DiscoveryHostPort = 7775;

    /// <summary>
    /// Identification string to be used, if there are several servers with similar constructs on the network.
    /// </summary>
    private const string Identification = "InteractionTest";

    #endregion

    /// <summary>
    /// The udp port for the discovery service.
    /// </summary>
    private UdpClient m_udpSocket;

    /// <summary>
    /// Flags that the socket is disposed. Unity does not like double disposal on that.
    /// </summary>
    private bool m_isUdpSocketDisposed;

    /// <summary>
    /// Flags the different network states the client may be in.
    /// </summary>
    private enum ClientState
    {
        Searching,
        Connecting,
        Connected,
        Restarting
    }

    /// <summary>
    /// Contains the information if we are connected as a client.
    /// </summary>
    private ClientState m_clientState;

    /// <summary>
    /// The ip address we have received a connection request for
    /// </summary>
    private string m_connectionString;

    /// <summary>
    /// The port the host is operating on.
    /// </summary>
    private ushort m_operationPort;

    /// <summary>
    /// The coroutine that sends as a client regular pings for finding a server.
    /// </summary>
    private Coroutine m_clientCoroutine;

    /// <summary>
    /// The network manager component er use.
    /// </summary>
    private NetworkManager m_networkManager;

    /// <summary>
    /// The transport layer we use.
    /// </summary>
    private UnityTransport m_transportLayer;

    /// <summary>
    /// Flag used to find out of the level is loaded.
    /// </summary>
    private AsyncOperation m_loadingOp;

    /// <summary>
    /// Contains a list with the anchors to set and save the world positions-.
    /// </summary>
    private List<TransformLight> m_listOfAnchors;

    /// <summary>
    /// Asks for a specific name, used for directory creation on server.
    /// </summary>
    public static string SpecificName => Identification;

    /// <summary>
    /// Depending on the situation we start a host or client udp port.
    /// </summary>
    void Awake()
    {
        // In case of the scene reload we may be the second connection manager.
        if (FindObjectsOfType<ConnectionManager>().Length > 1)
        {
            m_isUdpSocketDisposed = true;
            Destroy(this.gameObject);
            return;
        }

        if (Platform.IsHost)
        {
            m_udpSocket = new UdpClient(DiscoveryHostPort) { EnableBroadcast = true, MulticastLoopback = false };
            m_isUdpSocketDisposed = false;
        }
    }

    /// <summary>
    /// First we patch the prefabs that can eventually get spawned.As a host we start the host and a thread to listen for client
    /// requests. As a client we spawn a thread to listen for server responses and regularily send out discovery packets. 
    /// </summary>
    void Start()
    {
        m_networkManager = GetComponent<NetworkManager>();
        m_networkManager.OnClientDisconnectCallback += ClientDisconnected;
        m_transportLayer = GetComponent<UnityTransport>();

#pragma warning disable CS0162
        if (Platform.IsHost)
        {
            // We need to find a free UDP port here.
            using (UdpClient transient = new UdpClient(0))
                m_transportLayer.ConnectionData.Port = (ushort)((IPEndPoint)transient.Client.LocalEndPoint).Port;
            m_transportLayer.ConnectionData.ServerListenAddress = IPAddress.Any.ToString();

            m_networkManager.StartHost();
            UdpHostRoutine();
        }
        else
        {
            SearchForServer();
        }
#pragma warning restore CS0162
    }

    /// <summary>
    /// Searches for a server.
    /// </summary>
    private void SearchForServer()
    {
        m_udpSocket = new UdpClient(0) { EnableBroadcast = true, MulticastLoopback = false };
        m_isUdpSocketDisposed = false;
        m_clientState = ClientState.Searching;
        m_clientCoroutine = StartCoroutine(SendDiscoveryPackets());
        UdpClientRoutine();
    }

    /// <summary>
    /// Gets invoked when a client got disconnected. If done on the server side the control locks have to be removed.
    /// On the client side the client has to search again.
    /// </summary>
    /// <param name="clientId"></param>
    private void ClientDisconnected(ulong clientId)
    {
#pragma warning disable CS0162
        if (Platform.IsHost)
        {
            RPCChanneler.Singleton.ReleaseInteractionOnDisconnect(clientId);
        }
        else
        {
            // Get the old positions from the nodes.
            m_listOfAnchors = FindObjectOfType<AnchorControllerComponent>().AnchorPositions;
            m_networkManager.Shutdown();
            m_loadingOp = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

            m_clientState = ClientState.Restarting;
        }
#pragma warning restore CS0162
    }

    /// <summary>
    /// Stop all thready if still running.
    /// </summary>
    void OnDestroy()
    {
        if (!m_isUdpSocketDisposed)
            m_udpSocket.Dispose();
    }

    /// <summary>
    /// For every incoming request we send a response.
    /// </summary>
    private async void UdpHostRoutine()
    {
        Byte[] sendBytes =
            Encoding.ASCII.GetBytes(Identification + "Response Port: " + m_transportLayer.ConnectionData.Port);
        try
        {
            // Will get destroyed automatically, when the sockets gets disposed.
            while (true)
            {
                // Wait for a client request to arrive.
                UdpReceiveResult result = await m_udpSocket.ReceiveAsync();

                if (Encoding.ASCII.GetString(result.Buffer) == Identification + "Request")
                    // Reply with a ping to the client.
                    await m_udpSocket.SendAsync(sendBytes, sendBytes.Length, result.RemoteEndPoint);
            }
        }
        catch (ObjectDisposedException)
        {
            // Nothing to do here. may happen, when we stop the program.
        }
    }

    /// <summary>
    /// Thrad for the client waits for incoming response to identify the server ip.
    /// </summary>
    private async void UdpClientRoutine()
    {
        try
        {
            UdpReceiveResult result;
            string transmission;
            string[] separator = { "Port: " };
            do
            {
                result = await m_udpSocket.ReceiveAsync();
                transmission = Encoding.ASCII.GetString(result.Buffer);
            } while (!transmission.StartsWith(Identification + "Response"));

            string[] splitResult = transmission.Split(separator, 2, StringSplitOptions.None);
            m_operationPort = ushort.Parse(splitResult[1]);

            m_connectionString = result.RemoteEndPoint.Address.ToString();
            m_clientState = ClientState.Connecting;
        }
        catch (ObjectDisposedException)
        {
            // Nothing to do here.
        }
    }

    /// <summary>
    /// Co routine to send discovers packages to find a server to connect to.
    /// </summary>
    /// <returns></returns>
    private IEnumerator SendDiscoveryPackets()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes(Identification + "Request");
        IPEndPoint target = new IPEndPoint(IPAddress.Broadcast, DiscoveryHostPort);
        while (true)
        {
            m_udpSocket.Send(sendBytes, sendBytes.Length, target);
            yield return new WaitForSecondsRealtime(1.0f);
        }
    }

    /// <summary>
    /// Waits for the client connection flag, if it receives so it stops the discovery co routine and starts the client.
    /// </summary>
    void Update()
    {
#pragma warning disable CS0162
        if (Platform.IsHost)
            return;

        switch (m_clientState)
        {
            case ClientState.Searching:
                // Nothing to do here.
                break;
            case ClientState.Connecting:
                StopCoroutine(m_clientCoroutine);
                m_udpSocket.Dispose();
                m_isUdpSocketDisposed = true;
                m_clientState = ClientState.Connected;
                m_transportLayer.ConnectionData.Address = m_connectionString;
                m_transportLayer.ConnectionData.Port = m_operationPort;
                m_networkManager.StartClient();
                break;
            case ClientState.Connected:
                // Nothing to do here.
                break;
            case ClientState.Restarting:
                if ((!m_networkManager.ShutdownInProgress) && (m_loadingOp.isDone))
                {
                    FindObjectOfType<AnchorControllerComponent>().AnchorPositions = m_listOfAnchors;
                    SearchForServer();
                }

                break;
        }
#pragma warning restore CS0162
    }
}