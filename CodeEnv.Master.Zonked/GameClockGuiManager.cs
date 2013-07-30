// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameClockGuiManager.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
[Obsolete]
public class GameClockGuiManager : GuiManagerBase<GameClockGuiManager> {

    public GuiElements guiElements = new GuiElements();

    private GameEventManager eventMgr;
    private GameClockSpeed[] speedsOrderedByRisingValue;
    private float[] orderedSliderStepValues;


    void Awake() {
        eventMgr = GameEventManager.Instance;
    }

    void Start() {
        UpdateRate = UpdateFrequency.Seldom;
        InitializeGui();
    }

    protected override void InitializeGui() {
        base.InitializeGui();
    }

    protected override void AcquireGuiReferences() {
        if (!guiElements.gameSpeedSlider) {
            WarnOnMissingGuiElementReference(typeof(UISlider));
            guiElements.gameSpeedSlider = GetComponentInChildren<UISlider>();
        }
        if (!guiElements.pauseButton) {
            WarnOnMissingGuiElementReference(typeof(UIButton));
            guiElements.pauseButton = GetComponentInChildren<UIButton>();
        }

        bool isMissingUiLabel = false;
        if (!guiElements.gameSpeedReadout) {
            WarnOnMissingGuiElementReference(typeof(UILabel));
            isMissingUiLabel = true;
        }
        if (!guiElements.gameDateReadout) {
            WarnOnMissingGuiElementReference(typeof(UILabel));
            isMissingUiLabel = true;
        }
        if (isMissingUiLabel) {
            UILabel[] uiLabels = GetComponentsInChildren<UILabel>();
            foreach (var uiLabel in uiLabels) { // IMPROVE literals
                string labelName = uiLabel.name.ToLower();
                if (labelName.Contains("speed")) {
                    guiElements.gameSpeedReadout = uiLabel;
                }
                else if (labelName.Contains("date")) {
                    guiElements.gameDateReadout = uiLabel;
                }
            }
        }
    }

    protected override void SetupGuiEventHandlers() {
        guiElements.gameSpeedSlider.onValueChange += OnGameSpeedSliderChange;   // NGUI delegate dedicated to sliders
        UIEventListener.Get(guiElements.pauseButton.gameObject).onClick += OnPauseButtonClick;  // NGUI general event system
    }

    protected override void InitializeGuiWidgets() {
        GameClockSpeed[] speeds = Enum.GetValues(typeof(GameClockSpeed)) as GameClockSpeed[];
        //GameClockSpeed[] noSpeed = new GameClockSpeed[] { GameClockSpeed.None; };
        //speeds = speeds.Except<GameClockSpeed>(noSpeed) as GameClockSpeed[];
        //speeds = Enums<GameClockSpeed>.Remove(speeds, GameClockSpeed.None);
        speeds = speeds.Except<GameClockSpeed>(GameClockSpeed.None).ToArray<GameClockSpeed>();

        int numberOfSliderSteps = speeds.Length;
        guiElements.gameSpeedSlider.numberOfSteps = numberOfSliderSteps;

        //var sortedSpeeds = from s in speeds orderby s.SpeedMultiplier() select s;   // using Linq Query syntax
        var sortedSpeeds = speeds.OrderBy(s => s.SpeedMultiplier());   // using IEnumerable extension methods and lamba
        speedsOrderedByRisingValue = sortedSpeeds.ToArray<GameClockSpeed>();
        orderedSliderStepValues = MyNguiUtilities.GenerateOrderedSliderStepValues(numberOfSliderSteps);

        // Set Sliders initial tPrefsValue to that associated with GameClockSpeed.Normal
        int indexOfNormalSpeed = speedsOrderedByRisingValue.FindIndex<GameClockSpeed>(s => (s == GameClockSpeed.Normal));
        float sliderValueAtNormalSpeed = orderedSliderStepValues[indexOfNormalSpeed];
        guiElements.gameSpeedSlider.sliderValue = sliderValueAtNormalSpeed;

        RefreshGameSpeedReadout(GameClockSpeed.Normal);
    }

    private void RefreshGameSpeedReadout(GameClockSpeed clockSpeed) {
        guiElements.gameSpeedReadout.text = CommonTerms.MultiplySign + clockSpeed.SpeedMultiplier().ToString();
    }

    private void OnGameSpeedSliderChange(float gameSpeedSliderValue) {
        float tolerance = 0.05F;
        int index = orderedSliderStepValues.FindIndex<float>(v => Mathfx.Approx(gameSpeedSliderValue, v, tolerance));
        Arguments.ValidateNotNegative(index);
        GameClockSpeed clockSpeed = speedsOrderedByRisingValue[index];

        // dispatch event to GameClock telling of change
        eventMgr.Raise<GameSpeedChangeEvent>(new GameSpeedChangeEvent(clockSpeed));
        RefreshGameSpeedReadout(clockSpeed);
    }

    private void OnPauseButtonClick(GameObject sender) {
        UILabel pauseButtonLabel = guiElements.pauseButton.GetComponentInChildren<UILabel>();
        pauseButtonLabel.text = (GameManager.IsGamePaused) ? UIMessages.PauseButtonLabel : UIMessages.ResumeButtonLabel;
        // Toggle pause state
        GameManager.IsGamePaused = !GameManager.IsGamePaused;
    }

    void Update() {
        if (ToUpdate()) {
            guiElements.gameDateReadout.text = GameTime.Date.FormattedDate;
        }
    }

    protected override void OnApplicationQuit() {
        instance = null;
    }

    [Serializable]
    public class GuiElements {
        public UISlider gameSpeedSlider;
        public UIButton pauseButton;
        public UILabel gameSpeedReadout;
        public UILabel gameDateReadout;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

