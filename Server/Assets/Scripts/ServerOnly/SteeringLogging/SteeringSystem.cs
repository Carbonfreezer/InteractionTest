using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UIElements;
using UnityEngine;

namespace SteeringLogging
{
    /// <summary>
    /// Logic component that does all the mission handling and debriefing system handling.
    /// </summary>
    [Strip]
    public class SteeringSystem : MonoBehaviour
    {
        [SerializeField, Tooltip("The time distance that should be applied between logs.")]
        private float m_loggingInterval;

        [SerializeField, Tooltip("The mission control panel, where we control the missions with.")]
        private MissionControlPanel m_missionControlPanel;

        /// <summary>
        /// Logging system that 
        /// </summary>
        private LoggingModule m_loggingModule;

        /// <summary>
        /// List with steerable components.
        /// </summary>
        private List<ISteerable> m_steerables;

        /// <summary>
        /// The list with the elements, that can get into pause mode.
        /// </summary>
        private List<IPausable> m_pausables;

        /// <summary>
        /// The list with the objects, that flag for finishing.
        /// </summary>
        private List<IFinishable> m_finisher;

        /// <summary>
        /// Flags that a log file is opened.
        /// </summary>
        private bool m_logFileOpened;

        /// <summary>
        /// The global game state for selection, Mission Running and Debriefing.
        /// </summary>
        private enum GameState
        {
            /// <summary>
            /// The user selects what he wants to do next.
            /// </summary>
            InSelection,

            /// <summary>
            /// We are running a mission.
            /// </summary>
            Mission,

            /// <summary>
            /// We have flagged for finish, but want toi wait one second to record the last scoring changes.
            /// </summary>
            WaitingForFinish,

            /// <summary>
            /// We are in debriefing mode.
            /// </summary>
            Debriefing
        }


        /// <summary>
        /// The game state we are currently in.
        /// </summary>
        private GameState m_gameState;

        /// <summary>
        /// The time we have already been in the waiting phase for finish.
        /// </summary>
        private float m_timeWaitingForFinish;

        /// <summary>
        /// The pathname of the save file.
        /// </summary>
        private string m_savePathName;

        /// <summary>
        /// The directory info for the files.
        /// </summary>
        private DirectoryInfo m_dirInfo;

        /// <summary>
        /// List with exisitng log files.
        /// </summary>
        private readonly List<LogFileInfo> m_logFiles = new List<LogFileInfo>();

        /// <summary>
        /// The last position we had on the debrief slider.
        /// </summary>
        private float m_lastDebriefSliderPosition;

        /// <summary>
        /// Gets the info, if we are in pause mode or not.
        /// </summary>
        private bool m_isInPauseMode;

        /// <summary>
        /// Creates the logging system.
        /// </summary>
        public void Awake()
        {
            m_loggingModule = new LoggingModule(m_loggingInterval);
            m_steerables = Utility.GetAllComponents<ISteerable>();
            m_finisher = Utility.GetAllComponents<IFinishable>();
            m_pausables = Utility.GetAllComponents<IPausable>();
            m_gameState = GameState.InSelection;
            m_missionControlPanel.OnDebriefStarted += MissionControlPanelOnOnDebriefStarted;
            m_missionControlPanel.OnGameStarted += MissionControlPanelOnOnGameStarted;
            m_missionControlPanel.OnPauseToggled += MissionControlPanelOnOnPauseToggled;

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            m_savePathName = Path.Combine(path, ConnectionManager.SpecificName);
            m_dirInfo = Directory.CreateDirectory(m_savePathName);
        }

       
        public void Start()
        {
            m_missionControlPanel.InteractionState = MissionControlPanel.ControlState.PlayingFree;
            GetFileInfos();
            m_steerables.ForEach(item => item.StartMission(false));
        }

        /// <summary>
        /// Gets called when the pause function gets toggled.
        /// </summary>
        private void MissionControlPanelOnOnPauseToggled()
        {
            m_isInPauseMode = !m_isInPauseMode;
            SetAllPauseMode(m_isInPauseMode);
        }


        private void MissionControlPanelOnOnGameStarted()
        {
            m_steerables.ForEach(item => item.StartMission(true));
            SetAllPauseMode(false);
            m_loggingModule.StartLogging();
            m_logFileOpened = true;
            m_gameState = GameState.Mission;
            m_missionControlPanel.InteractionState = MissionControlPanel.ControlState.Playing;
        }

        private void MissionControlPanelOnOnDebriefStarted(uint slot)
        {
            m_steerables.ForEach(item => item.StartDebrief());
            SetAllPauseMode(false);
            m_gameState = GameState.Debriefing;
            m_missionControlPanel.InteractionState = MissionControlPanel.ControlState.Replay;
            m_lastDebriefSliderPosition = -1.0f;

            // Read in the log file.
            Debug.Assert(!m_logFileOpened, "Log is currently running.");
            string fileName = m_logFiles[(int)slot].m_completeName;
            using (BinaryReader reader = new BinaryReader(File.OpenRead(fileName)))
                m_loggingModule.ReadLoggingFile(reader);
        }

        /// <summary>
        /// Sets the pause mode to all registered components.
        /// </summary>
        /// <param name="isInPauseMode">Indicates if we are in pause mode or not.</param>
        private void SetAllPauseMode(bool isInPauseMode)
        {
            m_pausables.ForEach(item=>item.SetPauseMode(isInPauseMode));
            m_missionControlPanel.SetPauseMode(isInPauseMode);
            m_isInPauseMode = isInPauseMode;
        }

        /// <summary>
        /// Method to update the file infos.
        /// </summary>
        private void GetFileInfos()
        {
            FileInfo[] files = m_dirInfo.GetFiles();
            m_logFiles.Clear();
            foreach (FileInfo file in files)
            {
                LogFileInfo newFile = new LogFileInfo()
                {
                    m_completeName = file.FullName,
                    m_year = (byte)(file.CreationTime.Year - 2000),
                    m_month = (byte)(file.CreationTime.Month),
                    m_day = (byte)(file.CreationTime.Day),
                    m_hour = (byte)(file.CreationTime.Hour),
                    m_minute = (byte)(file.CreationTime.Minute)
                };
                m_logFiles.Add(newFile);
            }

            m_missionControlPanel.LogFiles = m_logFiles;
        }

        /// <summary>
        /// Saves the current log to a file.
        /// </summary>
        public void Update()
        {
            switch (m_gameState)
            {
                case GameState.InSelection:
                    // Nothing in the moment.
                    break;
                case GameState.Mission:
                    if (!m_isInPauseMode)
                        m_loggingModule.Update();

                   
                    if (m_finisher.Any(cand => cand.IsFinished == IFinishable.FinishStatus.StopImmediate))
                    {
                        DumpLog();
                        m_missionControlPanel.InteractionState = MissionControlPanel.ControlState.PlayingFree;
                        m_gameState = GameState.InSelection;
                    }
                    else if (m_finisher.Any(cand => cand.IsFinished == IFinishable.FinishStatus.StopDelay))
                    {
                        m_timeWaitingForFinish = 0.0f;
                        m_gameState = GameState.WaitingForFinish;
                    }

                    break;
                case GameState.WaitingForFinish:
                    m_timeWaitingForFinish += Time.deltaTime;
                    m_loggingModule.Update();
                    if ((m_timeWaitingForFinish > m_loggingInterval * 2.0f) && (m_logFileOpened))
                    {
                        DumpLog();
                        m_missionControlPanel.InteractionState = MissionControlPanel.ControlState.PlayingFree;
                        m_gameState = GameState.InSelection;
                    }

                    break;

                case GameState.Debriefing:
                    float sliderPosition = m_missionControlPanel.DebriefSliderPosition;
                    if (Mathf.Abs(sliderPosition - m_lastDebriefSliderPosition) > Mathf.Epsilon)
                    {
                        m_loggingModule.ShowLoggingState(sliderPosition);
                        m_lastDebriefSliderPosition = sliderPosition;
                    }
                    break;
            }
        }

        /// <summary>
        /// Stops the logging process and dumps the save file.
        /// </summary>
        private void DumpLog()
        {
            if (!m_logFileOpened)
                return;

            m_logFileOpened = false;
            m_loggingModule.StopLogging();
            string finalName = Path.Combine(m_savePathName, $"{DateTime.Now.Ticks}.log");
            using (BinaryWriter writer = new BinaryWriter(File.Create(finalName)))
                m_loggingModule.SaveLoggingSystem(writer);

            GetFileInfos();
        }
    }
}