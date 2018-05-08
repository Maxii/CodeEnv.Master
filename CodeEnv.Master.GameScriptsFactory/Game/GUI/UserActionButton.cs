// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserActionButton.cs
// Multipurpose AGuiButton that handles events that the User is required to manually deal with, e.g. a ResearchCompleted event.
// Doubles as the user's pause/resume button. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Multipurpose AGuiButton that handles events that the User is required to manually deal with, e.g. a ResearchCompleted event.
/// Doubles as the user's pause/resume button. When manually handling an event, pause/resume functionality is not exposed so 
/// won't allow user attempt to change pause state until all events are handled.
/// <remarks>Once I add an additional type of event (besides RschCompleted), events can arrive in the same frame before a pause
/// from the first stops date progression. In that case, I'll need a Stack memory of the sequence so they can be handled in order
/// by the user.</remarks>
/// </summary>
public class UserActionButton : AGuiButton {

    private static IEnumerable<KeyCode> _validPauseResumeKeys = new KeyCode[] { KeyCode.Pause };
    private static IEnumerable<KeyCode> _validOpenRschScreenKeys = new KeyCode[] { };

    protected override IEnumerable<KeyCode> ValidKeys {
        get { return _actionMode == ActionMode.PauseResume ? _validPauseResumeKeys : _validOpenRschScreenKeys; }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private PlayerResearchManager _userRschMgr;
    private ResearchWindow _userRschWindow;
    private DebugControls _debugCntls;

    private UISprite _actionIcon;
    private ActionMode _actionMode = ActionMode.PauseResume;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _debugCntls = DebugControls.Instance;
        _userRschWindow = ResearchWindow.Instance;
        _actionIcon = gameObject.GetSingleComponentInImmediateChildren<UISprite>();
        _gameMgr.isPausedChanged += IsPausedChangedEventHandler;
        _gameMgr.isReadyForPlayOneShot += IsReadyForPlayEventHandler;
    }

    /// <summary>
    /// Subscribes to all non-pause/resume events that this button is intended to handle.
    /// </summary>
    private void SubscribeToActionEvents() {
        D.Assert(_debugCntls.UserSelectsTechs);
        _userRschMgr.researchCompleted += UserResearchCompletedEventHandler;
    }

    #region Event and Property Change Handlers

    private void IsPausedChangedEventHandler(object sender, EventArgs e) {
        HandleIsPausedChanged();
    }

    private void IsReadyForPlayEventHandler(object sender, EventArgs e) {
        _userRschMgr = _gameMgr.UserAIManager.ResearchMgr;

        if (_debugCntls.UserSelectsTechs) {
            SubscribeToActionEvents();
            EventDelegate.Add(_userRschWindow.onHideComplete, ResearchWindowClosedEventHandler);
            ShowOpenResearchScreenButton();   // Prompt User to pick the initial tech to research
        }
        else {
            ShowPauseResumeButton();
        }
    }

    private void UserResearchCompletedEventHandler(object sender, PlayerResearchManager.ResearchCompletedEventArgs e) {
        HandleUserResearchCompleted(e.CompletedResearch);
    }

    private void ResearchWindowClosedEventHandler() {
        HandleUserClosedResearchWindow();
    }

    #endregion

    private void HandleIsPausedChanged() {
        if (_actionMode == ActionMode.PauseResume) {
            ShowPauseResumeButton();
        }
    }

    private void HandleUserResearchCompleted(ResearchTask completedResearch) {
        D.Assert(completedResearch.IsCompleted);
        D.Assert(!_gameMgr.IsPaused);

        if (!_userRschMgr.IsResearchQueued) {
            EventDelegate.Add(_userRschWindow.onHideComplete, ResearchWindowClosedEventHandler);
            ShowOpenResearchScreenButton();
        }
    }

    private void HandleUserClosedResearchWindow() {
        if (_userRschMgr.CurrentResearchTask != TempGameValues.NoResearch) {
            // User has picked a tech to research now or when prerequisites are met which has resulted in the assignment of a ResearchTask
            // only subscribed when waiting for user to open research window
            EventDelegate.Remove(_userRschWindow.onHideComplete, ResearchWindowClosedEventHandler);
            _actionMode = ActionMode.PauseResume;
            // must resume after ResearchScreen is closed as closing only cancels the pause
            // request that came from opening the screen, not the one we sent from HandleUserResearchCompleted
            _gameMgr.RequestPauseStateChange(toPause: false);
        }
        // else User closed the ResearchWindow without selecting a new Tech to research
    }

    protected override void HandleValidClick() {
        if (_actionMode == ActionMode.PauseResume) {
            bool toPause = !_gameMgr.IsPaused;
            _gameMgr.RequestPauseStateChange(toPause);
            if (toPause) {
                //__ShowDialogBoxTest();
            }
        }
        else {
            D.AssertEqual(ActionMode.OpenResearchScreen, _actionMode);
            GuiManager.Instance.ClickRschScreenButton();
        }
    }

    private void ShowOpenResearchScreenButton() {
        _actionMode = ActionMode.OpenResearchScreen;
        // must immediately pause as User might delay pressing the button to bring up the ResearchScreen
        _gameMgr.RequestPauseStateChange(toPause: true);

        PopulateButton(AtlasID.MyGui, "microscope23_16", "Click to open Research Screen");
    }

    private void ShowPauseResumeButton() {
        string spriteFilename = _gameMgr.IsPaused ? "Run1_16" : "Pause1_16";
        string tooltip = _gameMgr.IsPaused ? "Click to Resume" : "Click to Pause";
        PopulateButton(AtlasID.MyGui, spriteFilename, tooltip);
    }

    private void PopulateButton(AtlasID atlasID, string filename, string tooltip) {
        _actionIcon.atlas = atlasID.GetAtlas();
        _actionIcon.spriteName = filename;
        _tooltipContent = tooltip;
    }

    protected override void Cleanup() {
        if (_gameMgr != null) {
            _gameMgr.isReadyForPlayOneShot -= IsReadyForPlayEventHandler;
            _gameMgr.isPausedChanged -= IsPausedChangedEventHandler;
        }
        if (_userRschMgr != null) {
            _userRschMgr.researchCompleted -= UserResearchCompletedEventHandler;
        }
        if (_userRschWindow != null) {
            EventDelegate.Remove(_userRschWindow.onHideComplete, ResearchWindowClosedEventHandler);
        }
    }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        D.Assert(gameObject.activeInHierarchy);
    }

    private void __ShowDialogBoxTest() {
        var dialogWindow = DialogWindow.Instance;
        var dialogSettings = new DialogWindow.DialogSettings() {
            Title = "Pause Test",
            Text = "I am paused",
            ShowDoneButton = true,
            DoneButtonDelegate = new EventDelegate(() => {
                D.Warn("Done Button was clicked!");
                dialogWindow.Hide();
            }),
        };
        dialogWindow.Show(FormID.TextDialog, dialogSettings);
    }

    #endregion

    #region Nested Classes

    public enum ActionMode {
        None,

        PauseResume,

        OpenResearchScreen
    }

    #endregion

}

