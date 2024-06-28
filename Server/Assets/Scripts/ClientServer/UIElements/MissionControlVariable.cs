using SteeringLogging;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace UIElements
{
    /// <summary>
    /// Contains the communication service for the mission control system.
    /// </summary>
    public class MissionControlVariable : NetworkVariableBase
    {
        /// <summary>
        /// Delegate for the update event.
        /// </summary>
        public delegate void InformStateChange();

        /// <summary>
        /// Gets called when something has changed. Especially interesting on the client side.
        /// </summary>
        public event InformStateChange OnInformStateChange;

        /// <summary>
        /// Flags the information that the logs are dirty and also need to be send.
        /// </summary>
        private bool m_logsDirty;

        /// <summary>
        /// The logs we currently have.
        /// </summary>
        private List<LogFileInfo> m_containedLogs = new List<LogFileInfo>();

        /// <summary>
        /// The entry we have selected on the list.
        /// </summary>
        private uint m_selectedEntry;

        /// <summary>
        /// Contains if we are debriefing a mission log.
        /// </summary>
        private uint m_missionDebriefing;

        /// <summary>
        /// The sdlider position we have.
        /// </summary>
        private float m_sliderPosition;

        /// <summary>
        /// The current control state we are in.
        /// </summary>
        private MissionControlPanel.ControlState m_currentState;

        /// <summary>
        /// Contains the information if we are in pause mode.
        /// </summary>
        private bool m_isInPauseMode;

        /// <summary>
        /// Checks if we should be able to control the panel.
        /// </summary>
        public bool IsSteerable => (m_currentState != MissionControlPanel.ControlState.Playing);

        /// <summary>
        /// Checks, if we can toggle pause mode.
        /// </summary>
        public bool IsPausable => (m_currentState != MissionControlPanel.ControlState.Replay);

        /// <summary>
        /// The position we have on the slider.
        /// </summary>
        public float SliderPosition
        {
            set
            {
                Debug.Assert(Platform.IsHost, "Only to be called on server.");
                Debug.Assert((m_sliderPosition >= 0.0f) && (m_sliderPosition < 1.0f), "Slider in illegal range");
                m_sliderPosition = value;
                SetDirty(true);
                OnInformStateChange?.Invoke();
            }
            get => m_sliderPosition;
        }

        /// <summary>
        /// Manipulates the selected entry in the data list.
        /// </summary>
        public uint SelectedEntry
        {
            set
            {
                Debug.Assert(Platform.IsHost, "Only to be called on server.");
                m_selectedEntry = value;
                SetDirty(true);
                OnInformStateChange?.Invoke();
            }
            get => m_selectedEntry;
        }

        /// <summary>
        /// Checks from the outside if we can manipulate the buttons.Needed on the client for the material.
        /// </summary>
        public MissionControlPanel.ControlState CurrentState
        {
            set
            {
                Debug.Assert(Platform.IsHost, "Only to be called on server.");
                m_currentState = value;
                SetDirty(true);
                OnInformStateChange?.Invoke();
            }
            get => m_currentState;
        }


        /// <summary>
        /// The get set functionality if we are in pause mode.
        /// </summary>
        public bool PauseMode
        {
            set
            {
                Debug.Assert(Platform.IsHost, "Only to be called on server.");
                m_isInPauseMode = value;
                SetDirty(true);
                OnInformStateChange?.Invoke();
            }
            get => m_isInPauseMode;
        }

        /// <summary>
        /// Sets / gets the information if we are debriefing a mission.
        /// </summary>
        public uint MissionDebriefing
        {
            set
            {
                Debug.Assert(Platform.IsHost, "Only to be called on server.");
                m_missionDebriefing = value;
                SetDirty(true);
                OnInformStateChange?.Invoke();
            }
            get => m_missionDebriefing;
        }

        /// <summary>
        /// Accessor for the logs we contain.
        /// </summary>
        public List<LogFileInfo> ContainedLogs
        {
            set
            {
                Debug.Assert(Platform.IsHost, "Only to be called on server.");
                Debug.Assert(value.Count < 256, "Too many entries.");
                m_containedLogs = value;
                SetDirty(true);
                m_logsDirty = true;
                OnInformStateChange?.Invoke();
            }
            get => m_containedLogs;
        }

        /// <summary>
        /// Encodes the current status in a uint leaving the top bit empty as flag.
        /// </summary>
        /// <returns>Encoded status.</returns>
        private (uint high, uint low) EncodeStatus()
        {
            uint lower = (uint)(m_sliderPosition * ushort.MaxValue) & 0xFFFF;
            uint upper = (m_missionDebriefing << 24) | (m_selectedEntry << 16);

            uint high = (uint)m_currentState;
            if (m_isInPauseMode)
                high |= (1 << 4);

            return (high, upper | lower);
        }

        /// <summary>
        /// Decodes the status from the indicated value.
        /// </summary>
        /// <param name="input"></param>
        private void DecodeStatus(uint inputHigh, uint inputLow)
        {
            m_sliderPosition = (inputLow & 0xFFFF) / (float)((ushort.MaxValue));
            m_selectedEntry = (inputLow >> 16) & 0xff;
            m_missionDebriefing = (inputLow >> 24) & 0xff;
            m_currentState = (MissionControlPanel.ControlState)(inputHigh & 0xf);
            m_isInPauseMode = ((inputHigh >> 4) & 1) != 0;
        }

        /// <summary>
        /// Generates the log for the list entry.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="containedEntries"></param>
        private void WriteLogList(FastBufferWriter writer)
        {
            byte containedEntries = (byte)m_containedLogs.Count;
            writer.WriteValueSafe(containedEntries);
            writer.TryBeginWrite(containedEntries * 5);
            foreach (LogFileInfo log in m_containedLogs)
            {
                writer.WriteByte(log.m_year);
                writer.WriteByte(log.m_month);
                writer.WriteByte(log.m_day);
                writer.WriteByte(log.m_hour);
                writer.WriteByte(log.m_minute);
            }
        }

        /// <summary>
        /// Reads in the log list.
        /// </summary>
        /// <param name="reader"></param>
        private void ReadLogList(FastBufferReader reader)
        {
            reader.ReadByteSafe(out byte containedEntries);
            reader.TryBeginRead(containedEntries * 5);
            m_containedLogs.Clear();
            for (int i = 0; i < containedEntries; i++)
            {
                LogFileInfo newLog = new LogFileInfo();
                reader.ReadByte(out newLog.m_year);
                reader.ReadByte(out newLog.m_month);
                reader.ReadByte(out newLog.m_day);
                reader.ReadByte(out newLog.m_hour);
                reader.ReadByte(out newLog.m_minute);
                m_containedLogs.Add(newLog);
            }
        }

        #region Overrides of NetworkVariableBase

        public override void ResetDirty()
        {
            base.ResetDirty();
            m_logsDirty = false;
        }

        /// <summary>
        /// Writes out the delta can eventually skip the list.
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteDelta(FastBufferWriter writer)
        {
            (uint high, uint low) = EncodeStatus();
            if (m_logsDirty)
            {
                high |= (1 << 30);
                writer.WriteValueSafe(high);
                writer.WriteValueSafe(low);
                WriteLogList(writer);
            }
            else
            {
                writer.WriteValueSafe(high);
                writer.WriteValueSafe(low);
            }
        }

        /// <summary>
        /// Writes out all.
        /// </summary>
        /// <param name="writer"></param>
        public override void WriteField(FastBufferWriter writer)
        {
            (uint high, uint low) = EncodeStatus();
            writer.WriteValueSafe(high);
            writer.WriteValueSafe(low);
            WriteLogList(writer);
        }

        public override void ReadField(FastBufferReader reader)
        {
            reader.ReadValueSafe(out uint high);
            reader.ReadValueSafe(out uint low);
            DecodeStatus(high, low);
            ReadLogList(reader);

            OnInformStateChange?.Invoke();
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            reader.ReadValueSafe(out uint high);
            reader.ReadValueSafe(out uint low);
            bool hasList = (high & (1 << 30)) != 0;
            DecodeStatus(high, low);
            if (hasList)
                ReadLogList(reader);
            m_logsDirty = false;
            OnInformStateChange?.Invoke();
        }

        #endregion
    }
}