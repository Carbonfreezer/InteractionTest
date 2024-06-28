using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SteeringLogging
{
    /// <summary>
    /// The logging system for the action replay method.
    /// </summary>
    public class LoggingModule
    {
        private readonly float m_loggingInterval;

        /// <summary>
        /// The time, that has passed since we generated the last log.
        /// </summary>
        private float m_timeSinceLastLog;

        /// <summary>
        /// Flags that we are actively logging in the moment.
        /// </summary>
        private bool m_loggingActive;

        /// <summary>
        /// Contains a logging point. With time and state info.
        /// </summary>
        private class LoggingPoint
        {
            /// <summary>
            /// The time when the logging point was done.
            /// </summary>
            public float m_timeOfLog;

            /// <summary>
            /// The content of the log.
            /// </summary>
            public byte[] m_stateInfo;
        }

        /// <summary>
        /// Stores the current log.
        /// </summary>
        private readonly List<LoggingPoint> m_currentLog = new List<LoggingPoint>();

        /// <summary>
        /// Contains the list of all loggable objects we want to log.
        /// </summary>
        private readonly List<ILoggable> m_loggables;

        /// <summary>
        /// Creates the logging system to log data.
        /// </summary>
        /// <param name="loggingInterval">The interval from which we start logging.</param>
        public LoggingModule(float loggingInterval)
        {
            m_loggingInterval = loggingInterval;
            m_loggables = Utility.GetAllComponents<ILoggable>();
        }

        /// <summary>
        /// Generates a log in the specified interval when needed.
        /// </summary>
        public void Update()
        {
            if (!m_loggingActive)
                return;

            m_timeSinceLastLog += Time.deltaTime;
            if (m_timeSinceLastLog < m_loggingInterval)
                return;
            float logTime;
            if (m_currentLog.Count == 0)
                logTime = 0.0f;
            else
                logTime = m_timeSinceLastLog + m_currentLog[m_currentLog.Count - 1].m_timeOfLog;
            AppendLog(logTime);
        }

        /// <summary>
        /// Generates a new logging point.
        /// </summary>
        /// <param name="logTime"></param>
        private void AppendLog(float logTime)
        {
            using MemoryStream mem = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(mem);
            foreach (ILoggable loggable in m_loggables)
                loggable.Serialize(writer);

            LoggingPoint newLoggingPoint = new LoggingPoint
            {
                m_timeOfLog = logTime,
                m_stateInfo = mem.ToArray()
            };

            m_currentLog.Add(newLoggingPoint);
            m_timeSinceLastLog = 0.0f;
        }

        /// <summary>
        /// Starts the logging process from the beginning.
        /// </summary>
        public void StartLogging()
        {
            Debug.Assert(!m_loggingActive, "Logging is already active.");
            m_currentLog.Clear();
            // Force first log early.
            m_timeSinceLastLog = m_loggingInterval - 0.2f;
            m_loggingActive = true;
        }

        /// <summary>
        /// Stops the logging process.
        /// </summary>
        public void StopLogging()
        {
            Debug.Assert(m_loggingActive, "Logging already deactivated.");
            m_loggingActive = false;
        }

        /// <summary>
        /// Applies the situation state just to the last log time that is smaller than the time handed over.
        /// </summary>
        /// <param name="logTime">Point of time till we want to restore the logger. Is in [0..1] region. </param>
        public void ShowLoggingState(float logTime)
        {
            Debug.Assert(!m_loggingActive, "Logging is still active while extracting.");
            Debug.Assert((logTime >= 0.0f) && (logTime < 1.0f), "Requested logging time is not in covered interval.");
            float searchTime = logTime * m_currentLog[m_currentLog.Count - 1].m_timeOfLog;
            int resetIndex = m_currentLog.FindIndex(log => (log.m_timeOfLog > searchTime)) - 1;
            if (resetIndex < 0)
                resetIndex = 0;

            using MemoryStream mem = new MemoryStream(m_currentLog[resetIndex].m_stateInfo);
            using BinaryReader reader = new BinaryReader(mem);
            foreach (ILoggable loggable in m_loggables)
                loggable.Serialize(reader);
        }

        /// <summary>
        /// Saves the logging information to a binary writer. This is intended to be used to save
        /// information to a file for later playback.
        /// </summary>
        /// <param name="writer"></param>
        public void SaveLoggingSystem(BinaryWriter writer)
        {
            writer.Write(m_currentLog.Count);
            foreach (LoggingPoint loggingPoint in m_currentLog)
            {
                writer.Write(loggingPoint.m_stateInfo.Length);
                writer.Write(loggingPoint.m_timeOfLog);
                writer.Write(loggingPoint.m_stateInfo);
            }
        }

        /// <summary>
        /// Reads the log from a log stream.
        /// </summary>
        /// <param name="reader">Binary reader to read data from.</param>
        public void ReadLoggingFile(BinaryReader reader)
        {
            m_currentLog.Clear();
            int amountOfLogs = reader.ReadInt32();
            for (int i = 0; i < amountOfLogs; i++)
            {
                int logLength = reader.ReadInt32();
                LoggingPoint newPoint = new LoggingPoint
                {
                    m_timeOfLog = reader.ReadSingle(),
                    m_stateInfo = reader.ReadBytes(logLength)
                };
                m_currentLog.Add(newPoint);
            }
        }
    }
}