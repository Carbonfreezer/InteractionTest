using UnityEngine;

namespace UIElements
{
    /// <summary>
    /// This is a simple button controller.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ButtonController : MonoBehaviour, ITriggerable
    {
        [SerializeField, Tooltip("The core button that also has the remote poker component.")]
        private Transform m_coreButton;

        [SerializeField, Tooltip("The position we have for the button, when we are down.")]
        private Transform m_downPosition;

        [SerializeField, Tooltip("The position we are when we are up.")]
        private Transform m_upPosition;

        /// <summary>
        /// We can not use a unity event here, because we need to return a value.
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public delegate bool ButtonPressed(ulong clientId);

        /// <summary>
        /// Gets invoked when the button has been pressed.
        /// </summary>
        public event ButtonPressed ButtonPressEvent;

        /// <summary>
        /// The system we use for locally moving the button.
        /// </summary>
        private LocalButtonMover m_localMover;

        public void Start()
        {
            m_localMover =
                new LocalButtonMover(m_coreButton, m_downPosition, m_upPosition, GetComponent<AudioSource>());

            if (Platform.IsHost)
                m_coreButton.gameObject.GetComponent<RemotePokeController>().Poking += OnPoking;

            RPCChanneler.Singleton.RegisterTrigger(this);
        }

        /// <summary>
        /// Gets called when er get poked.
        /// </summary>
        /// <param name="clientid">The client who pressed the button.</param>
        private void OnPoking(ulong clientid)
        {
            if ((ButtonPressEvent != null) && (ButtonPressEvent(clientid)))
                RPCChanneler.Singleton.TriggerInvocation(this);
        }

        public void Update()
        {
            m_localMover.Update(Time.deltaTime);
        }

        #region Implementation of ITriggerable

        public void Trigger()
        {
            m_localMover.PressButton();
        }

        public GameObject InternalObject => gameObject;

        #endregion
    }
}