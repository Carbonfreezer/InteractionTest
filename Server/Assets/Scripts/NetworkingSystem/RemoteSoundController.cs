using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server side of a sound starting object. A network object component has to be included with no transformation necesarry.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class RemoteSoundController : NetworkBehaviour
{
    /// <summary>
    /// The audio source 
    /// </summary>
    private AudioSource m_audioSource;

    #region Methods for server

    /// <summary>
    /// Property for the pitch.
    /// </summary>
    public float Pitch { get; set; }

    /// <summary>
    /// Setter attribute for the volume.
    /// </summary>
    public float Volume { get; set; }

    /// <summary>
    /// Starts playing the sound.
    /// </summary>
    public void PlaySound()
    {
        PlaySoundClientRPC(Volume, Pitch);
    }

    #endregion

    #region Overrides of NetworkBehaviour

    public override void OnNetworkSpawn()
    {
        m_audioSource = GetComponent<AudioSource>();
        Volume = Pitch = 1.0f;

        base.OnNetworkSpawn();
    }

    #endregion

    #region methods for client

    [ClientRpc]
    private void PlaySoundClientRPC(float volume, float pitch)
    {
        if (!IsSpawned)
            return;

#pragma warning disable CS0162
        // Do not play sounds on host.
        if (!Platform.HasAudio)
            return;

        m_audioSource.volume = volume;
        m_audioSource.pitch = pitch;
        m_audioSource.Play();
#pragma warning restore CS0162
    }

    #endregion
}