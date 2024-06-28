using SteeringLogging;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UIElements.LogDisplay
{
    /// <summary>
    /// Contains the information for the log display.
    /// </summary>
    public class LogVariable : NetworkVariableBase, ILoggable
    {
        /// <summary>
        /// Contains the number of log entries we like to show.
        /// </summary>
        private int m_numOfLogsToShow;

        /// <summary>
        /// The log with the strings we like to display.
        /// </summary>
        private List<string> m_logContainer;

        /// <summary>
        /// The clock we use to generate time annotations.
        /// </summary>
        private SyncedClock m_clock;

        /// <summary>
        /// The text element where we show the text.
        /// </summary>
        private TextMeshProUGUI m_textElement;

        /// <summary>
        /// Bypasses delta compression and requests a full rewrite.
        /// </summary>
        private bool m_requestFullRewrite;

        /// <summary>
        /// The list with the delta information for delta compression.
        /// </summary>
        private List<string> m_deltaRewrite;

        /// <summary>
        /// Sets the required data from the outside.
        /// </summary>
        /// <param name="numOfLogsToDisplay">The number of log lines we would like to have,</param>
        /// <param name="clock">The clock we use to annotate the data.</param>
        /// <param name="textElement">The text element we like to steer.</param>
        public void Initialize(int numOfLogsToDisplay, SyncedClock clock, TextMeshProUGUI textElement)
        {
            m_clock = clock;
            m_textElement = textElement;
            m_numOfLogsToShow = numOfLogsToDisplay;
            m_logContainer = new List<string>(numOfLogsToDisplay);
            m_deltaRewrite = new List<string>(numOfLogsToDisplay);
        }

        /// <summary>
        /// Appends a new log to the text.
        /// </summary>
        /// <param name="logText">The text we like to append to the log.</param>
        public void AppendLog(string logText)
        {
            if (m_logContainer.Count >= m_numOfLogsToShow)
                m_logContainer.RemoveAt(0);
            string logLine = m_clock.HoursMinutesText + ": " + logText;
            m_logContainer.Add(logLine);
            m_deltaRewrite.Add(logLine);
            UpdateDisplay();
            SetDirty(true);
        }

        /// <summary>
        /// Called from outside to clear the log.
        /// </summary>
        public void ClearLog()
        {
            m_logContainer.Clear();
            UpdateDisplay();
            m_requestFullRewrite = true;
            SetDirty(true);
        }

        /// <summary>
        /// Updates the text value on the display.
        /// </summary>
        private void UpdateDisplay()
        {
            string result = "";
            foreach (string text in m_logContainer)
                result += text + "\n";
            m_textElement.text = result;
        }

        #region Overrides of NetworkVariableBase

        public override void ResetDirty()
        {
            m_deltaRewrite.Clear();
            m_requestFullRewrite = false;
            base.ResetDirty();
        }

        /// <inheritdoc />
        public override void WriteDelta(FastBufferWriter writer)
        {
            if (m_requestFullRewrite)
            {
                writer.WriteByteSafe(0);
                WriteField(writer);
            }
            else
            {
                byte length = (byte)m_deltaRewrite.Count;
                Debug.Assert(length > 0, "We should have something to write as delta.");
                writer.WriteByteSafe(length);
                foreach (string content in m_deltaRewrite)
                    writer.WriteValueSafe(content);
            }
        }

        /// <inheritdoc />
        public override void WriteField(FastBufferWriter writer)
        {
            writer.WriteValueSafe(m_logContainer.Count);
            foreach (string content in m_logContainer)
                writer.WriteValueSafe(content);
        }

        /// <inheritdoc />
        public override void ReadField(FastBufferReader reader)
        {
            m_logContainer.Clear();
            reader.ReadValueSafe(out int entries);
            for (int i = 0; i < entries; i++)
            {
                reader.ReadValueSafe(out string line);
                m_logContainer.Add(line);
            }

            UpdateDisplay();
        }

        /// <inheritdoc />
        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            reader.ReadByteSafe(out byte deltaInfo);
            if (deltaInfo == 0)
            {
                ReadField(reader);
                return;
            }

            for (int i = 0; i < deltaInfo; ++i)
            {
                reader.ReadValueSafe(out string line);
                if (m_logContainer.Count >= m_numOfLogsToShow)
                    m_logContainer.RemoveAt(0);
                m_logContainer.Add(line);
            }
            UpdateDisplay();
        }

        #endregion

        #region Implementation of ILoggable

        /// <inheritdoc />
        public void Serialize(BinaryReader reader)
        {
            m_logContainer.Clear();
            int numEntries = reader.ReadInt32();
            for (int i = 0; i < numEntries; i++)
            {
                string text = reader.ReadString();
                m_logContainer.Add(text);
            }

            UpdateDisplay();
            m_requestFullRewrite = true;
            SetDirty(true);
        }

        /// <inheritdoc />
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(m_logContainer.Count);
            foreach (string text in m_logContainer)
                writer.Write(text);
        }

        #endregion
    }
}