using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace UIElements
{
    [RequireComponent(typeof(NetworkObjectWrapper))]
    public class SliderController : NetworkBehaviour
    {
        /// <summary>
        /// The position where we have the slider (0...1)
        /// </summary>
        private NetworkVariable<float> m_sliderPosition = new NetworkVariable<float>();

        [SerializeField, Tooltip("The object we want to move.")]
        private GameObject m_slider;

        [SerializeField, Tooltip("Start position")]
        private Transform m_startPosition;

        [SerializeField, Tooltip("End Position")]
        private Transform m_endPosition;

        [SerializeField, Tooltip("The audio clip we intend to play on moving.")]
        private AudioClip m_audioClipToPlay;

        [Tooltip("Gets called, when the slider has been moved the first time.")]
        public UnityEvent m_sliderMoved;

        private LocalSliderMover m_sliderMover;

        /// <summary>
        /// Asks for the value of the mover.
        /// </summary>
        public float SliderValue => m_sliderPosition.Value;

        public void Awake()
        {
            m_sliderMover = new LocalSliderMover(m_slider, m_startPosition, m_endPosition, m_audioClipToPlay);
        }

        public void Start()
        {
#pragma warning disable CS0162
            if (!Platform.IsHost)
                return;

            m_sliderMover.OnSliderMoved += OnSliderMoved;
#pragma warning restore CS0162
        }

        public void Update()
        {
            m_sliderMover.Update();
        }

        #region Overrides of NetworkBehaviour

        public override void OnNetworkSpawn()
        {
            m_sliderPosition.OnValueChanged += (value, newValue) => m_sliderMover.SliderValue = newValue;
            m_sliderMover.SliderValue = m_sliderPosition.Value;
            if (Platform.IsHost)
                m_sliderPosition.Value = 0.99999f;

            base.OnNetworkSpawn();
        }

        #endregion

        private void OnSliderMoved(float value)
        {
            m_sliderPosition.Value = value;
            m_sliderMoved.Invoke();
        }
    }
}