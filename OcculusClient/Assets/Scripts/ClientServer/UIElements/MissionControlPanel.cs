using SteeringLogging;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace UIElements
{
    /// <summary>
    /// The panel for the mission controller.
    /// </summary>
    [RequireComponent(typeof(NetworkObjectWrapper), typeof(AudioSource))]
    public class MissionControlPanel : NetworkBehaviour, IFinishable, ISteerable
    {
        /// <summary>
        /// Gets called when the game has started.
        /// </summary>
        public delegate void GameStarted();

        /// <summary>
        /// The event when the game has started.
        /// </summary>
        public event GameStarted OnGameStarted;

        /// <summary>
        /// Gets called when the debirefing has started.
        /// </summary>
        /// <param name="slot">The slot that should be loaded.</param>
        public delegate void DebriefStarted(uint slot);

        /// <summary>
        /// The debriefing started event.
        /// </summary>
        public event DebriefStarted OnDebriefStarted;

        /// <summary>
        /// Gets called when the pause button has been toggled.
        /// </summary>
        public delegate void PauseToggled();

        /// <summary>
        /// Gets invoked when the pause button has been toggled.
        /// </summary>
        public event PauseToggled OnPauseToggled;

        /// <summary>
        /// Asks for the position of the debrief slider.
        /// </summary>
        public float DebriefSliderPosition => m_netVariable.SliderPosition;

        /// <summary>
        /// Sets the log file information.
        /// </summary>
        public List<LogFileInfo> LogFiles
        {
            set => m_netVariable.ContainedLogs = value;
        }

        /// <summary>
        /// Contains the information of the controlstate of the board.
        /// </summary>
        public enum ControlState
        {
            Playing = 0,
            PlayingFree = 1,
            Replay = 2
        }

        /// <summary>
        /// Sets the steerabilty on the server side.
        /// </summary>
        public MissionControlPanel.ControlState InteractionState
        {
            set => m_netVariable.CurrentState = value;
        }

        #region Editable elements

        [Header("Play Button")]
        [SerializeField, Tooltip("The button itself.")]
        private GameObject m_playButton;

        [SerializeField, Tooltip("The lower position.")]
        private Transform m_playLower;

        [SerializeField, Tooltip("The upper position.")]
        private Transform m_playUpper;

        [Header("Stop Button")]
        [SerializeField, Tooltip("The button itself.")]
        private GameObject m_stopButton;

        [SerializeField, Tooltip("The lower position.")]
        private Transform m_stopLower;

        [SerializeField, Tooltip("The upper position.")]
        private Transform m_stopUpper;

        [Header("Pause Button")]
        [SerializeField, Tooltip("The button itself.")]
        private GameObject m_pauseButton;

        [SerializeField, Tooltip("The lower position.")]
        private Transform m_pauseLower;

        [SerializeField, Tooltip("The upper position.")]
        private Transform m_pauseUpper;

        [SerializeField, Tooltip("The control lamp for the pause button.")]
        private LampColorSetter m_lampColorSetter;


        [Header("Debrief Button")]
        [SerializeField, Tooltip("The button.")]
        private GameObject m_debriefButton;

        [SerializeField, Tooltip("The lower position.")]
        private Transform m_debriefLower;

        [SerializeField, Tooltip("The upper position.")]
        private Transform m_debriefUpper;

        [Header("Scroll Up Button")]
        [SerializeField, Tooltip("The button.")]
        private GameObject m_upButton;

        [SerializeField, Tooltip("The lower position.")]
        private Transform m_upLower;

        [SerializeField, Tooltip("The upper position.")]
        private Transform m_upUpper;

        [Header("Scroll Down Button")]
        [SerializeField, Tooltip("The button.")]
        private GameObject m_downButton;

        [SerializeField, Tooltip("The lower position.")]
        private Transform m_downLower;

        [SerializeField, Tooltip("The upper position.")]
        private Transform m_downUpper;

        [Header("Time slider")]
        [SerializeField, Tooltip("The  slider.")]
        private GameObject m_slider;

        [SerializeField, Tooltip("The left position.")]
        private Transform m_sliderLeft;

        [SerializeField, Tooltip("The right position.")]
        private Transform m_sliderRight;

        [Header("TextDisplay")]
        [SerializeField, Tooltip("The line where we show the header.")]
        private TextMeshProUGUI m_headerLine;

        [SerializeField, Tooltip("The five text lines from top to bottom.")]
        private TextMeshProUGUI[] m_textLines = new TextMeshProUGUI[5];

        #endregion

        /// <summary>
        /// The mover for the four buttons.
        /// </summary>
        private LocalButtonMover m_playButtonMover;

        private LocalButtonMover m_debriefingButtonMover;
        private LocalButtonMover m_upButtonMover;
        private LocalButtonMover m_downButtonMover;
        private LocalButtonMover m_stopButtonMover;
        private LocalButtonMover m_pauseButtonMover;

        private LocalSliderMover m_sliderMover;

        /// <summary>
        /// The material list we use for setting th materials.
        /// </summary>
        private Material[] m_materials = new Material[3];

        /// <summary>
        /// The list of the renderers we use.
        /// </summary>
        private readonly MeshRenderer[] m_renderers = new MeshRenderer[4];

        /// <summary>
        /// Flags which material is the red one.
        /// </summary>
        private readonly int[] m_redMaterialIndex = new int[4];

        /// <summary>
        /// The material for the base button.
        /// </summary>
        private Material m_baseMaterial;

        /// <summary>
        /// The material we use for activated red elememnts.
        /// </summary>
        private Material m_redMaterial;

        /// <summary>
        /// The network communication system.
        /// </summary>
        private MissionControlVariable m_netVariable = new MissionControlVariable();

        public void Awake()
        {
            AudioSource audio = GetComponent<AudioSource>();
            m_netVariable.OnInformStateChange += NetInformationChanged;
            m_playButtonMover = new LocalButtonMover(m_playButton.transform, m_playLower, m_playUpper,
                audio);
            m_renderers[0] = m_playButton.GetComponent<MeshRenderer>();
            m_redMaterialIndex[0] = 0;
            m_redMaterial = m_renderers[0].sharedMaterials[0];
            m_baseMaterial = m_renderers[0].sharedMaterials[1];

            m_debriefingButtonMover = new LocalButtonMover(m_debriefButton.transform, m_debriefLower, m_debriefUpper,
                audio);
            m_renderers[1] = m_debriefButton.GetComponent<MeshRenderer>();
            m_redMaterialIndex[1] = 1;

            m_stopButtonMover = new LocalButtonMover(m_stopButton.transform, m_stopLower, m_stopUpper, audio);
            m_renderers[2] = m_stopButton.GetComponent<MeshRenderer>();
            m_redMaterialIndex[2] = 1;


            m_pauseButtonMover = new LocalButtonMover(m_pauseButton.transform, m_pauseLower, m_pauseUpper, audio);
            m_renderers[3] = m_pauseButton.GetComponent<MeshRenderer>();
            m_redMaterialIndex[3] = 1;

            m_upButtonMover = new LocalButtonMover(m_upButton.transform, m_upLower, m_upUpper,
                audio);
            m_downButtonMover = new LocalButtonMover(m_downButton.transform, m_downLower, m_downUpper,
                audio);



        }

        /// <summary>
        /// Gets called when network information got changed.
        /// </summary>
        private void NetInformationChanged()
        {
            // Update header.
            switch (m_netVariable.CurrentState)
            {
                case MissionControlPanel.ControlState.Playing:
                    m_headerLine.text = "Playing Recording";
                    break;
                case MissionControlPanel.ControlState.PlayingFree:
                    m_headerLine.text = "Playing Free";
                    break;
                case MissionControlPanel.ControlState.Replay:
                    m_headerLine.text = "Replay: " + m_netVariable.ContainedLogs[(int)m_netVariable.MissionDebriefing]
                        .DescriptiveString;
                    break;
            }

            // Update slider.
            if (m_sliderMover != null)
                m_sliderMover.SliderValue = m_netVariable.SliderPosition;

            Material[] mats;
            // Update button painting.
            for (int i = 0; i < 2; ++i)
            {
                mats = m_renderers[i].sharedMaterials;
                mats[m_redMaterialIndex[i]] = m_netVariable.IsSteerable ? m_redMaterial : m_baseMaterial;
                m_renderers[i].sharedMaterials = mats;
            }

            // Stop button and pause button is inverse.
            mats = m_renderers[2].sharedMaterials;
            mats[m_redMaterialIndex[2]] = m_netVariable.IsSteerable ? m_baseMaterial : m_redMaterial;
            m_renderers[2].sharedMaterials = mats;

            mats = m_renderers[3].sharedMaterials;
            mats[m_redMaterialIndex[3]] = m_netVariable.IsPausable ?  m_redMaterial : m_baseMaterial;
            m_renderers[3].sharedMaterials = mats;

            // Check the pause mode.
            m_lampColorSetter.SetLampStatus(m_netVariable.PauseMode);


            // Update the display list.
            foreach (TextMeshProUGUI textLine in m_textLines)
                textLine.text = "";

            if (m_netVariable.ContainedLogs.Count == 0)
                return;

            int startPosition = Math.Max(0, (int)(m_netVariable.SelectedEntry) - 2);
            int endPosition = Math.Min(m_netVariable.ContainedLogs.Count - 1, (int)(m_netVariable.SelectedEntry) + 2);

            for (int i = startPosition; i <= endPosition; i++)
                m_textLines[i - m_netVariable.SelectedEntry + 2].text =
                    m_netVariable.ContainedLogs[i].DescriptiveString;
        }

        public void Start()
        {
            m_sliderMover = new LocalSliderMover(m_slider, m_sliderLeft, m_sliderRight,
                m_slider.GetComponent<AudioSource>().clip);

#pragma warning disable CS0162
            if (!Platform.IsHost)
                return;

            m_playButton.GetComponent<RemotePokeController>().Poking += id => PokePlay();
            m_debriefButton.GetComponent<RemotePokeController>().Poking += id => PokeDebrief();
            m_upButton.GetComponent<RemotePokeController>().Poking += id => PokeUp();
            m_downButton.GetComponent<RemotePokeController>().Poking += id => PokeDown();
            m_stopButton.GetComponent<RemotePokeController>().Poking += id => PokeStop();
            m_pauseButton.GetComponent<RemotePokeController>().Poking += id => PokePause();
            m_sliderMover.OnSliderMoved += OnSliderMoved;
            NetInformationChanged();
#pragma warning restore CS0162
        }

      

        public void Update()
        {
            m_playButtonMover.Update(Time.deltaTime);
            m_debriefingButtonMover.Update(Time.deltaTime);
            m_upButtonMover.Update(Time.deltaTime);
            m_downButtonMover.Update(Time.deltaTime);
            m_pauseButtonMover.Update(Time.deltaTime);
            m_sliderMover.Update();
        }

        /// <summary>
        /// Gets called when the slider got moved.
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnSliderMoved(float value)
        {
            m_netVariable.SliderPosition = value;
        }

        /// <summary>
        /// Gets invoked, when we have pressed the down button.
        /// </summary>
        private void PokeDown()
        {
            if (m_netVariable.SelectedEntry < m_netVariable.ContainedLogs.Count - 1)
                m_netVariable.SelectedEntry += 1;

            ButtonPressedClientRpc(3);
        }

        /// <summary>
        /// Gets invoked on down button pressed.
        /// </summary>
        private void PokeUp()
        {
            if (m_netVariable.SelectedEntry > 0)
                m_netVariable.SelectedEntry -= 1;

            ButtonPressedClientRpc(2);
        }

        /// <summary>
        /// Gets invoked on debrief button pressed.
        /// </summary>
        private void PokeDebrief()
        {
            if (!m_netVariable.IsSteerable)
                return;

            m_netVariable.MissionDebriefing = m_netVariable.SelectedEntry;
            OnDebriefStarted?.Invoke(m_netVariable.SelectedEntry);
            ButtonPressedClientRpc(1);
        }

        /// <summary>
        /// Gets invoked on play button pressed.
        /// </summary>
        private void PokePlay()
        {
            if (!m_netVariable.IsSteerable)
                return;

            OnGameStarted?.Invoke();
            ButtonPressedClientRpc(0);
        }


        /// <summary>
        /// Gets called when the stop button got pressed.
        /// </summary>
        private void PokeStop()
        {
            if (m_netVariable.IsSteerable)
                return;

            IsFinished = IFinishable.FinishStatus.StopImmediate;
            ButtonPressedClientRpc(4);
        }

        /// <summary>
        /// Gets called, when the pause button gets pressed.
        /// </summary>
        private void PokePause()
        {
            if (!m_netVariable.IsPausable)
                return;

            OnPauseToggled?.Invoke();

            ButtonPressedClientRpc(5);
        }

        /// <summary>
        /// Gets invoked when a button got pressed to trigger the effect.
        /// </summary>
        /// <param name="buttonNumber">0: Play 1: Debrief, 2: Up, 3 Down, 4 Stop, 5 Pause</param>
        /// <exception cref="NotImplementedException"></exception>
        [ClientRpc]
        private void ButtonPressedClientRpc(int buttonNumber)
        {
            switch (buttonNumber)
            {
                case 0:
                    m_playButtonMover.PressButton();
                    break;
                case 1:
                    m_debriefingButtonMover.PressButton();
                    break;
                case 2:
                    m_upButtonMover.PressButton();
                    break;
                case 3:
                    m_downButtonMover.PressButton();
                    break;
                case 4:
                    m_stopButtonMover.PressButton();
                    break;
                case 5:
                    m_pauseButtonMover.PressButton();
                    break;
                default:
                    Debug.Assert(false, "Unimplemented case for button.");
                    break;
            }
        }

        #region Implementation of IFinishable

        public IFinishable.FinishStatus IsFinished { get; private set; }

        #endregion

        #region Implementation of ISteerable

        public void StartMission(bool requiresReset)
        {
            IsFinished = IFinishable.FinishStatus.ContinueGame;
        }

        public void StartDebrief()
        {

        }

        #endregion

        /// <summary>
        /// Toggles the control lamp for pause from the outside.
        /// </summary>
        /// <param name="isInPauseMode">Flags whether we are in pause more or not.</param>
        public void SetPauseMode(bool isInPauseMode)
        {
            m_netVariable.PauseMode = isInPauseMode;
        }
    }
}