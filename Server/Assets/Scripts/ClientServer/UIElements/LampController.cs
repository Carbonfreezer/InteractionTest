using UnityEngine;

namespace UIElements
{
    /// <summary>
    /// This is a controller for on / off lamps, that may be individually toggled on a per client basis.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class LampController : MonoBehaviour
    {
        [SerializeField, Tooltip("The material in case we are off.")]
        private Material m_offMaterial;

        [SerializeField, Tooltip("The material in case we are on.")]
        private Material m_onMaterial;

        /// <summary>
        /// Render used.
        /// </summary>
        private Renderer m_renderer;

        /// <summary>
        /// Flags the lamp as being on.
        /// </summary>
        private bool m_isOn;

        /// <summary>
        /// Flags if we are already registered.
        /// </summary>
        private bool m_registered;

        public void Awake()
        {
            m_renderer = GetComponent<Renderer>();
            m_renderer.sharedMaterial = m_offMaterial;
        }

        public void Start()
        {
            CheckRegistration();
        }

        /// <summary>
        /// Tests for registration.
        /// </summary>
        private void CheckRegistration()
        {
            if (m_registered)
                return;
            m_registered = true;
            RPCChanneler.Singleton.RegisterLamp(this);
        }

        /// <summary>
        /// Sets the lamp status for a specific client id.
        /// </summary>
        /// <param name="isOn">Whether on / off.</param>
        /// <param name="clientId">Client id to set for.</param>
        public void SetLampStatus(bool isOn, ulong clientId)
        {
            Debug.Assert(Platform.IsHost, "Only allowed to be called on host.");
            RPCChanneler.Singleton.LampInvocation(this, isOn, clientId);
        }

        /// <summary>
        /// Sets the lamp to off for all clients.
        /// </summary>
        public void SwitchOff()
        {
            Debug.Assert(Platform.IsHost, "Only allowed to be called on host.");
            CheckRegistration();
            RPCChanneler.Singleton.SwitchOffLampForAll(this);
        }

        /// <summary>
        /// Gets called from the RPC channeler to set the light to on off.
        /// </summary>
        /// <param name="isOn">If we want to set it on.</param>
        public void SwitchReception(bool isOn)
        {
            if (isOn == m_isOn)
                return;
            m_isOn = isOn;

            m_renderer.sharedMaterial = m_isOn ? m_onMaterial : m_offMaterial;
        }
    }
}