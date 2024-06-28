using UnityEngine;

namespace UIElements
{
    /// <summary>
    /// Helper class that governs the movement of the slider. Used on server and on client side.
    /// </summary>
    public class LocalSliderMover
    {
        /// <summary>
        /// Gets invoked when the slider has been moved returns in range [0,1],
        /// </summary>
        public delegate void SliderMoved(float value);

        /// <summary>
        /// Gets invoked on the server side, when the slider has been moved.
        /// </summary>
        public event SliderMoved OnSliderMoved;

        /// <summary>
        /// The transformation of the slider.
        /// </summary>
        private Transform m_sliderTransform;

        /// <summary>
        /// The distance the two sliders are apart.
        /// </summary>
        private readonly float m_sliderDistance;

        /// <summary>
        /// The directional vector from minimal to maximal position (in local coordinates of the parent).
        /// </summary>
        private readonly Vector3 m_sliderDirection;

        /// <summary>
        /// The local position of the minimal point.
        /// </summary>
        private readonly Vector3 m_minimalLocalPosition;

        /// <summary>
        /// The current position of the slider we are be in.
        /// </summary>
        private float m_currentPosition;

        /// <summary>
        /// The destination where the slider should get to.
        /// </summary>
        private float m_sliderDestination;

        /// <summary>
        /// The audio clip we play on slider start.
        /// </summary>
        private AudioClip m_sliderStartSound;

        /// <summary>
        /// The general audio source we have.
        /// </summary>
        private AudioSource m_audioSource;

        /// <summary>
        /// The last time we have really modified the slider value.
        /// </summary>
        private float m_lastTimeDataSet;

        /// <summary>
        /// The base volume factor we apply to the slider.
        /// </summary>
        public float m_baseVolume;

        /// <summary>
        /// The slider object itself.
        /// </summary>
        private GameObject m_slider;

        /// <summary>
        /// Generates a local slider mover.
        /// </summary>
        /// <param name="slider">The game object that has to be moved.</param>
        /// <param name="minimalPosition">The leftmost position.</param>
        /// <param name="maximalPosition">The rightmost position.</param>
        /// <param name="sliderStart">The sound effect we play on slider start.</param>
        public LocalSliderMover(GameObject slider, Transform minimalPosition, Transform maximalPosition,
            AudioClip sliderStart, float baseVolume = 1.0f)
        {
            m_minimalLocalPosition = minimalPosition.localPosition;
            m_sliderDirection = maximalPosition.localPosition - minimalPosition.localPosition;
            m_sliderDistance = m_sliderDirection.magnitude;
            m_sliderDirection /= m_sliderDistance;
            m_sliderTransform = slider.transform;

            m_sliderStartSound = sliderStart;
            m_audioSource = slider.GetComponent<AudioSource>();

            m_lastTimeDataSet = 0.0f;
            m_baseVolume = baseVolume;

            m_slider = slider;

#pragma warning disable CS0162
            if (!Platform.IsHost)
                return;
            RemoteSpatialManipulationController controller = slider.GetComponent<RemoteSpatialManipulationController>();
            Debug.Assert(controller != null, "No spatial manipulation controller found");
            controller.ControlTaken += id => true;
            controller.CoordinateSet += ControllerOnCoordinateSet;
#pragma warning restore CS0162
        }

        /// <summary>
        /// Sets the slider value, has to be called on the client and on the host to position it correctly
        /// </summary>
        public float SliderValue
        {
            set
            {
                float delta = Mathf.Abs(m_sliderDestination - value);
                bool valueChanged = delta > 0.0001f;
                if (valueChanged && (Platform.HasAudio) && (m_slider.activeInHierarchy))
                {
                    if (!m_audioSource.isPlaying)
                    {
                        m_audioSource.volume = m_baseVolume;
                        m_audioSource.pitch = 1.0f;
                        m_audioSource.PlayOneShot(m_sliderStartSound);
                        m_audioSource.Play();
                    }

                    delta = Mathf.Clamp01(delta * 100.0f);
                    delta *= delta;
                    m_audioSource.volume = delta * m_baseVolume * 0.1f;

                    m_lastTimeDataSet = Time.time;
                }

                m_sliderDestination = value;
            }
        }

        /// <summary>
        /// Updates the slider position in a damped way.
        /// </summary>
        public void Update()
        {
            // See if we have to stop the sound.
            if (m_audioSource.isPlaying && Mathf.Abs(m_lastTimeDataSet - Time.time) > 0.2f)
                m_audioSource.Stop();

            if (Mathf.Abs(m_currentPosition - m_sliderDestination) > 0.1f)
            {
                m_currentPosition = m_sliderDestination;
            }
            else
            {
                m_currentPosition = 0.8f * m_currentPosition + (0.2f * m_sliderDestination);
            }

            m_sliderTransform.localPosition =
                m_minimalLocalPosition + m_currentPosition * m_sliderDirection * m_sliderDistance;
        }

        /// <summary>
        /// sets the controller coordinate in the coordinates of the parent.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="orientation"></param>
        private void ControllerOnCoordinateSet(Vector3 position, Quaternion orientation)
        {
            Vector3 offsetToFirst = position - m_minimalLocalPosition;
            float desiredSliderValue = Vector3.Dot(offsetToFirst, m_sliderDirection) / m_sliderDistance;
            desiredSliderValue = Mathf.Clamp01(desiredSliderValue) * 0.99999f;
            OnSliderMoved?.Invoke(desiredSliderValue);
        }
    }
}