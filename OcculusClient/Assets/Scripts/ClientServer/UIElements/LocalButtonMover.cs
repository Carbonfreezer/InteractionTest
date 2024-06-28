using UnityEngine;
using Random = UnityEngine.Random;

namespace UIElements
{
    /// <summary>
    /// Helper class that initiates the moving of a button over time.
    /// </summary>
    public class LocalButtonMover
    {
        private Transform m_objectToMove;

        private enum InternalState
        {
            Waiting,
            GoingDown,
            GoingUp
        }

        /// <summary>
        /// Indicates what we are currently doing.
        /// </summary>
        private InternalState m_internalState = InternalState.Waiting;

        /// <summary>
        /// The time we are already in state when moving.
        /// </summary>
        private float m_timeInState;

        /// <summary>
        /// The time we spend for each of the transition periods.
        /// </summary>
        private const float TransitionTime = 0.3f;

        /// <summary>
        /// The local lower position of the button.
        /// </summary>
        private Vector3 m_lowerPoint;

        /// <summary>
        /// The local upperPosition of the button.
        /// </summary>
        private Vector3 m_upperPoint;

        /// <summary>
        /// The sound effect we would like to play.
        /// </summary>
        private AudioSource m_audioSource;

        /// <summary>
        /// Constructor for the helper class to move a button.
        /// </summary>
        /// <param name="objectToMove">The core of the button we intend to move.</param>
        /// <param name="lowerPosition">Coordinate system for the lower position of the button.</param>
        /// <param name="upperPosition">Coordinate system for the upper position of the button.</param>
        /// <param name="audioSource">Audio clip we play on button pressed.</param>
        public LocalButtonMover(Transform objectToMove, Transform lowerPosition, Transform upperPosition,
            AudioSource audioSource)
        {
            m_objectToMove = objectToMove;
            m_lowerPoint = lowerPosition.localPosition;
            m_upperPoint = upperPosition.localPosition;
            m_objectToMove.localPosition = m_upperPoint;
            m_audioSource = audioSource;
            m_internalState = InternalState.Waiting;
        }

        /// <summary>
        /// Invoked to press the button.
        /// </summary>
        public void PressButton()
        {
            if (m_internalState != InternalState.Waiting)
                return;

            m_timeInState = 0.0f;
            m_internalState = InternalState.GoingDown;

            m_audioSource.pitch = Random.Range(0.9f, 1.1f);
            if ((Platform.HasAudio) && (m_objectToMove.gameObject.activeInHierarchy))
                m_audioSource.Play();
        }

        /// <summary>
        /// Update with passed time has to be manually called from the system we are embedded in.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void Update(float deltaTime)
        {
            float blendFactor;
            switch (m_internalState)
            {
                case InternalState.Waiting:
                    return;
                case InternalState.GoingDown:
                    if (m_timeInState > TransitionTime)
                    {
                        m_timeInState = 0.0f;
                        m_internalState = InternalState.GoingUp;
                        return;
                    }

                    blendFactor = Mathf.SmoothStep(0.0f, 1.0f, m_timeInState / TransitionTime);
                    m_objectToMove.localPosition = m_upperPoint + blendFactor * (m_lowerPoint - m_upperPoint);
                    break;
                case InternalState.GoingUp:
                    if (m_timeInState > TransitionTime)
                    {
                        m_internalState = InternalState.Waiting;
                        return;
                    }

                    blendFactor = Mathf.SmoothStep(0.0f, 1.0f, m_timeInState / TransitionTime);
                    m_objectToMove.localPosition = m_lowerPoint + blendFactor * (m_upperPoint - m_lowerPoint);
                    break;
            }

            m_timeInState += deltaTime;
        }
    }
}