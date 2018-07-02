// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserActionButton.cs
// Multipurpose AGuiButton that handles cases that the User is required to manually deal with, e.g. a need to pick a
// Research topic or deal with a dialog. 
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
/// Multipurpose AGuiButton that handles cases that the User is required to manually deal with, e.g. a need to pick a
/// Research topic or deal with a dialog. Doubles as the user's pause/resume button. When manually handling a case, 
/// pause/resume functionality is not exposed so won't allow user to change pause state until all situations are handled.
/// <remarks>6.5.18 UNCLEAR Now that I've added additional cases, the methods that initiate handling of the case may be 
/// called in the same frame before a pause from the first stops date progression? In that case, I'll need a Stack memory of the 
/// sequence so they can be handled in order by the user. Accordingly, I've added an Assert(mode == PauseResume) in each case 
/// initiation method to detect this.</remarks>
/// </summary>
public class UserActionButton : AGuiButton, IUserActionButton {

    private static IEnumerable<KeyCode> _validPauseResumeKeys = new KeyCode[] { KeyCode.Pause };
    private static IEnumerable<KeyCode> _noValidKeys = new KeyCode[] { };

    protected override IEnumerable<KeyCode> ValidKeys {
        get { return _actionMode == ActionMode.PauseResume ? _validPauseResumeKeys : _noValidKeys; }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private DialogWindow _userDialogWindow;
    private PlayerResearchManager _userResearchMgr;
    private ResearchWindow _userResearchWindow;

    private UISprite _actionIcon;
    private DialogWindowSettings _dialogWindowSettings;
    private ActionMode _actionMode = ActionMode.PauseResume;

    #region MonoBehaviour Singleton Pattern

    protected static UserActionButton _instance;
    public static UserActionButton Instance {
        get {
            if (_instance == null) {
                if (IsApplicationQuiting) {
                    //D.Warn("Application is quiting while trying to access {0}.Instance.".Inject(typeof(UserActionButton).Name));
                    return null;
                }
                // Instance is required for the first time, so look for it                        
                Type thisType = typeof(UserActionButton);
                _instance = GameObject.FindObjectOfType(thisType) as UserActionButton;
                // value is required for the first time, so look for it                        
                if (_instance == null) {
                    var stackFrame = new System.Diagnostics.StackTrace().GetFrame(2);
                    string callerIdMessage = "{0}.{1}().".Inject(stackFrame.GetMethod().DeclaringType, stackFrame.GetMethod().Name);
                    D.Error("No instance of {0} found. Is it destroyed/deactivated? Called by {1}.".Inject(thisType.Name, callerIdMessage));
                }
                _instance.InitializeOnInstance();
            }
            return _instance;
        }
    }

    protected override void Awake() {
        // If no other MonoBehaviour has requested Instance in an Awake() call executing
        // before this one, then we are it. There is no reason to search for an object
        if (_instance == null) {
            _instance = this as UserActionButton;
            InitializeOnInstance();
        }
        InitializeOnAwake();
    }

    #endregion

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// Note: This method is not called by instance copies, only by the original instance. If not persistent across scenes,
    /// then this method will be called each time the new instance in a scene is instantiated.
    /// </summary>
    private void InitializeOnInstance() {
        GameReferences.UserActionButton = _instance;
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    private void InitializeOnAwake() {
        InitializeValuesAndReferences();
        __ValidateOnAwake();
    }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        __debugCntls = DebugControls.Instance;
        _userResearchWindow = ResearchWindow.Instance;
        _userDialogWindow = DialogWindow.Instance;
        _actionIcon = gameObject.GetSingleComponentInImmediateChildren<UISprite>();
        _gameMgr.isPausedChanged += IsPausedChangedEventHandler;
        _gameMgr.isReadyForPlayOneShot += IsReadyForPlayEventHandler;
    }

    #region Event and Property Change Handlers

    private void IsPausedChangedEventHandler(object sender, EventArgs e) {
        HandleIsPausedChanged();
    }

    private void IsReadyForPlayEventHandler(object sender, EventArgs e) {
        _userResearchMgr = _gameMgr.UserAIManager.ResearchMgr;

        if (__debugCntls.UserSelectsTechs) {
            EventDelegate.Add(_userResearchWindow.onHideComplete, ResearchWindowClosedEventHandler);
            ShowOpenResearchScreenButton();   // Prompt User to pick the initial tech to research
        }
        else {
            ShowPauseResumeButton();
        }
    }

    private void ResearchWindowClosedEventHandler() {
        HandleUserClosedResearchWindow();
    }

    private void DialogWindowClosedEventHandler() {
        HandleUserClosedDialogWindow();
    }

    #endregion

    private void HandleIsPausedChanged() {
        if (_actionMode == ActionMode.PauseResume) {
            ShowPauseResumeButton();
        }
    }

    public void ShowPickResearchPromptButton(ResearchTask completedResearch) {
        D.Assert(completedResearch.IsCompleted);
        D.Assert(!_gameMgr.IsPaused);
        D.Assert(__debugCntls.UserSelectsTechs);
        D.AssertEqual(ActionMode.PauseResume, _actionMode); // if this fails, see remarks in header
        D.Assert(!_userResearchMgr.IsResearchQueued);   // shouldn't show button if more research already queued

        EventDelegate.Add(_userResearchWindow.onHideComplete, ResearchWindowClosedEventHandler);
        ShowOpenResearchScreenButton();
    }

    private void HandleUserClosedResearchWindow() {
        if (_userResearchMgr.CurrentResearchTask != TempGameValues.NoResearch) {
            // User has picked a tech to research now or when prerequisites are met which has resulted in the assignment of a ResearchTask.
            // Only subscribed when waiting for user to open research window
            EventDelegate.Remove(_userResearchWindow.onHideComplete, ResearchWindowClosedEventHandler);
            _actionMode = ActionMode.PauseResume;
            // must resume after ResearchScreen is closed as closing only cancels the pause
            // request that came from opening the screen, not the one we sent from ShowOpenResearchScreenButton
            _gameMgr.RequestPauseStateChange(toPause: false);
            if (_gameMgr.IsPaused) {
                ShowPauseResumeButton();    // 6.5.18 Still need to change the icon to Run when request doesn't change pause state
            }
        }
        // else User closed the ResearchWindow without selecting a new Tech to research
    }

    private void HandleUserClosedDialogWindow() {
        // Only subscribed when waiting for user to open dialog window
        EventDelegate.Remove(_userDialogWindow.onHideComplete, DialogWindowClosedEventHandler);
        _actionMode = ActionMode.PauseResume;
        // must resume after DialogWindow is closed as closing only cancels the pause
        // request that came from opening the window, not the one we sent from ShowOpenDialogWindowButton
        _gameMgr.RequestPauseStateChange(toPause: false);
        if (_gameMgr.IsPaused) {
            ShowPauseResumeButton();    // 6.5.18 Still need to change the icon to Run when request doesn't change pause state
        }
    }

    public void ShowPickDesignPromptButton(FormID formID, APopupDialogForm.DialogSettings settings) {
        D.AssertNull(_dialogWindowSettings);
        D.AssertEqual(ActionMode.PauseResume, _actionMode); // if this fails, see remarks in header

        EventDelegate.Add(_userDialogWindow.onHideComplete, DialogWindowClosedEventHandler);
        _dialogWindowSettings = new DialogWindowSettings(formID, settings);
        ShowOpenDialogWindowButton();
    }

    protected override void HandleValidClick() {
        if (_actionMode == ActionMode.PauseResume) {
            bool toPause = !_gameMgr.IsPaused;
            _gameMgr.RequestPauseStateChange(toPause);
            if (toPause) {
                //__ShowDialogBoxTest();
            }
        }
        else if (_actionMode == ActionMode.OpenResearchScreen) {
            GuiManager.Instance.ClickRschScreenButton();
        }
        else {
            D.AssertEqual(ActionMode.OpenDialogWindow, _actionMode);
            _userDialogWindow.Show(_dialogWindowSettings.DialogFormID, _dialogWindowSettings.Settings);
            _dialogWindowSettings = null;
        }
    }

    private void ShowOpenDialogWindowButton() {
        _actionMode = ActionMode.OpenDialogWindow;
        // must immediately pause as User might delay pressing the button
        _gameMgr.RequestPauseStateChange(toPause: true);

        PopulateButton(AtlasID.MyGui, TempGameValues.LargeDialogIconFilename, "Click to open Dialog Window");
    }

    private void ShowOpenResearchScreenButton() {
        _actionMode = ActionMode.OpenResearchScreen;
        // must immediately pause as User might delay pressing the button
        _gameMgr.RequestPauseStateChange(toPause: true);

        PopulateButton(AtlasID.MyGui, TempGameValues.LargeScienceIconFilename, "Click to open Research Screen");
    }

    private void ShowPauseResumeButton() {
        string spriteFilename = _gameMgr.IsPaused ? TempGameValues.LargeRunIconFilename : TempGameValues.LargePauseIconFilename;
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
        if (_userResearchWindow != null) {
            EventDelegate.Remove(_userResearchWindow.onHideComplete, ResearchWindowClosedEventHandler);
        }
        if (_userDialogWindow != null) {
            EventDelegate.Remove(_userDialogWindow.onHideComplete, DialogWindowClosedEventHandler);
        }
        _instance = null;
    }

    #region Debug

    private DebugControls __debugCntls;

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        UnityUtility.ValidateComponentPresence<BoxCollider>(gameObject);
        D.Assert(gameObject.activeInHierarchy);
    }

    private void __ShowDialogBoxTest() {
        var dialogWindow = DialogWindow.Instance;
        var dialogSettings = new APopupDialogForm.DialogSettings {
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

        OpenResearchScreen,

        OpenDialogWindow
    }

    public class DialogWindowSettings {

        public FormID DialogFormID { get; private set; }

        public APopupDialogForm.DialogSettings Settings { get; private set; }

        public DialogWindowSettings(FormID dialogFormId, APopupDialogForm.DialogSettings settings) {
            DialogFormID = dialogFormId;
            Settings = settings;
        }
    }

    #endregion

}

