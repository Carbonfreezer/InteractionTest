using UnityEngine;

namespace UIElements
{
    /// <summary>
    /// Simple steering class to toggle a red lamp. In contrast to lamp controller, network synchronization as to be done
    /// on the outside. This lamp is meant to look the same on all clients. 
    /// </summary>
    public class LampColorSetter : MonoBehaviour
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
        /// Flags if the light is st to on.
        /// </summary>
        private bool m_isOn;

        public void Awake()
        {
            m_renderer = GetComponent<Renderer>();
            m_renderer.sharedMaterial = m_offMaterial;
        }

        /// <summary>
        /// Sets the lamp to on / off.
        /// </summary>
        /// <param name="isOn">If we want to set it on.</param>
        public void SetLampStatus(bool isOn)
        {
            if (isOn == m_isOn)
                return;
            m_isOn = isOn;

            m_renderer.sharedMaterial = m_isOn ? m_onMaterial : m_offMaterial;
        }
    }
}
