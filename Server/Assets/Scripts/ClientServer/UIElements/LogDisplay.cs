using SteeringLogging;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UIElements.LogDisplay
{
    [RequireComponent(typeof(NetworkObjectWrapper))]
    public class LogDisplay : NetworkBehaviour, ILoggable, ISteerable
    {
        [SerializeField, Tooltip("The text component where we would like to show our log.")]
        private TextMeshProUGUI m_textComponent;

        [SerializeField, Tooltip("Tme amount of log entries we want to have on display.")]
        private int m_logLinesToDisplay;

        /// <summary>
        /// The logging variable.
        /// </summary>
        private readonly LogVariable m_variable = new LogVariable();

        /// <summary>
        /// Sets the clock information from the outside.
        /// </summary>
        /// <param name="clock">Clock To set.</param>
        public void SetClock(SyncedClock clock)
        {
            m_variable.Initialize(m_logLinesToDisplay, clock, m_textComponent);
        }

        /// <summary>
        /// Appends a new log to the system.
        /// </summary>
        /// <param name="text">Text to show.</param>
        public void AppendLog(string text)
        {
            m_variable.AppendLog(text);
        }

        #region Implementation of ILoggable

        /// <inheritdoc />
        public void Serialize(BinaryReader reader)
        {
            m_variable.Serialize(reader);
        }

        /// <inheritdoc />
        public void Serialize(BinaryWriter writer)
        {
            m_variable.Serialize(writer);
        }

        #endregion

        #region Implementation of ISteerable

        public void StartMission(bool requiresReset)
        {
            if (requiresReset)
                m_variable.ClearLog();
        }

        public void StartDebrief()
        {
            m_variable.ClearLog();
        }

        #endregion
    }
}