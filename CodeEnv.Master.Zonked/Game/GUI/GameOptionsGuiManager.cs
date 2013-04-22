// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameOptionsGuiManager.cs
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
public class GameOptionsGuiManager : GuiManagerBase<GameOptionsGuiManager> {

    public GuiWidgets guiWidgets = new GuiWidgets();

    private GameEventManager eventMgr;
    private PlayerPrefsManager playerPrefsMgr;


    void Awake() {
        eventMgr = GameEventManager.Instance;
        playerPrefsMgr = PlayerPrefsManager.Instance;
    }

    void Start() {
        InitializeGui();
    }

    protected override void InitializeGui() {
        base.InitializeGui();
    }

    protected override void AcquireGuiReferences() {
        if (!guiWidgets.acceptOptionsButton) {
            WarnOnMissingGuiElementReference(typeof(UIButton));
            guiWidgets.acceptOptionsButton = GetComponentInChildren<UIButton>();
        }

        bool isMissingUiCheckbox = false;
        if (!guiWidgets.pauseAfterLoad) {
            WarnOnMissingGuiElementReference(typeof(UICheckbox));
            isMissingUiCheckbox = true;
        }
        if (!guiWidgets.resetOnFocus) {
            WarnOnMissingGuiElementReference(typeof(UICheckbox));
            isMissingUiCheckbox = true;
        }
        if (!guiWidgets.zoomOutOnCursor) {
            WarnOnMissingGuiElementReference(typeof(UICheckbox));
            isMissingUiCheckbox = true;
        }
        if (!guiWidgets.cameraRoll) {
            WarnOnMissingGuiElementReference(typeof(UICheckbox));
            isMissingUiCheckbox = true;
        }
        if (isMissingUiCheckbox) {
            UICheckbox[] uiCheckboxes = GetComponentsInChildren<UICheckbox>();
            foreach (var uiCheckbox in uiCheckboxes) { // IMPROVE literals
                string labelName = uiCheckbox.name.ToLower();
                if (labelName.Contains("pause")) {
                    guiWidgets.pauseAfterLoad = uiCheckbox;
                }
                else if (labelName.Contains("focusTarget")) {
                    guiWidgets.resetOnFocus = uiCheckbox;
                }
                if (labelName.Contains("zoom")) {
                    guiWidgets.zoomOutOnCursor = uiCheckbox;
                }
                else if (labelName.Contains("roll")) {
                    guiWidgets.cameraRoll = uiCheckbox;
                }
            }
        }

        bool isMissingUiPopupList = false;
        if (!guiWidgets.gameSpeedAfterLoad) {
            WarnOnMissingGuiElementReference(typeof(UIPopupList));
            isMissingUiPopupList = true;
        }
        if (!guiWidgets.universeSize) {
            WarnOnMissingGuiElementReference(typeof(UIPopupList));
            isMissingUiPopupList = true;
        }
        if (isMissingUiPopupList) {
            UIPopupList[] uiPopupLists = GetComponentsInChildren<UIPopupList>();
            foreach (var uiPopupList in uiPopupLists) { // IMPROVE literals
                string listName = uiPopupList.transform.parent.name.ToLower();  // Note parent
                if (listName.Contains("speed")) {
                    //guiWidgets.gameSpeedAfterLoad = uiPopupList;
                }
                else if (listName.Contains("size")) {
                    guiWidgets.universeSize = uiPopupList;
                }
            }
        }
    }

    protected override void SetupGuiEventHandlers() {
        // Ngui Delegates
        guiWidgets.cameraRoll.onStateChange += OnCameraRollOptionChange;
        guiWidgets.pauseAfterLoad.onStateChange += OnPauseAfterReloadOptionChange;
        guiWidgets.resetOnFocus.onStateChange += OnResetOnFocusOptionChange;
        guiWidgets.zoomOutOnCursor.onStateChange += OnZoomOutOnCursorOptionChange;

        guiWidgets.gameSpeedAfterLoad.onSelectionChange += OnGameSpeedAfterLoadOptionChange;
        guiWidgets.universeSize.onSelectionChange += OnUniverseSizeOptionChange;   // TODO move to game setup screen

        UIEventListener.Get(guiWidgets.acceptOptionsButton.gameObject).onClick += OnAcceptOptionsButtonClick;  // NGUI general event system
    }

    protected override void InitializeGuiWidgets() {
        // guiWidgets.cameraRoll.isChecked = playerPrefsMgr.IsCameraRollEnabled;
        guiWidgets.pauseAfterLoad.isChecked = playerPrefsMgr.IsPauseOnLoadEnabled;
        guiWidgets.resetOnFocus.isChecked = playerPrefsMgr.IsResetOnFocusEnabled;
        guiWidgets.zoomOutOnCursor.isChecked = playerPrefsMgr.IsZoomOutOnCursorEnabled;
        // guiWidgets.gameSpeedAfterLoad.selectionName = playerPrefsMgr.GameSpeedOnLoad.GetName();
        guiWidgets.universeSize.selection = playerPrefsMgr.SizeOfUniverse.GetName();

    }

    private void OnAcceptOptionsButtonClick(GameObject sender) {
        // do nothing for now. Later I could accumlate changes below, then send all on this button click
        // which would make sense if I added a Cancel button

        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));
    }

    private void OnUniverseSizeOptionChange(string item) {
        UniverseSize size;
        if (!Enums<UniverseSize>.TryParse(item, true, out size)) {
            WarnOnIncorrectName(item);
            return;
        }

        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));
        if (size != UniverseSize.Normal) {
            Debug.LogError("Universe Size Change is only allowed during New Game Setup.");
            return;
        }
        GameManager.UniverseSize = size;
        playerPrefsMgr.SizeOfUniverse = size;
    }

    private void OnGameSpeedAfterLoadOptionChange(string item) {
        GameClockSpeed speed;
        if (!Enums<GameClockSpeed>.TryParse(item, true, out speed)) {
            WarnOnIncorrectName(item);
            return;
        }

        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));

        // UNDONE
        playerPrefsMgr.GameSpeedOnLoad = speed;
        Debug.LogWarning("OnGameSpeedAfterLoadOptionChange() is not yet fully implemented.");
    }

    private void OnZoomOutOnCursorOptionChange(bool state) {
        _CameraControl.Instance.IsScrollZoomOutOnCursorEnabled = state;
        playerPrefsMgr.IsZoomOutOnCursorEnabled = state;

        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));

    }

    private void OnResetOnFocusOptionChange(bool state) {
        _CameraControl.Instance.IsResetOnFocusEnabled = state;
        playerPrefsMgr.IsResetOnFocusEnabled = state;

        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));

    }

    private void OnPauseAfterReloadOptionChange(bool state) {

        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));

        // UNDONE
        playerPrefsMgr.IsPauseOnLoadEnabled = state;
        Debug.LogWarning("OnPauseAfterReloadOptionChange() is not yet fully implemented.");
    }

    private void OnCameraRollOptionChange(bool state) {
        _CameraControl.Instance.IsRollEnabled = state;
        playerPrefsMgr.IsCameraRollEnabled = state;

        System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(0);
        Debug.Log("{0}.{1}() method called.".Inject(GetType(), stackFrame.GetMethod().Name));

    }


    protected override void OnApplicationQuit() {
        instance = null;
    }

    [Serializable]
    public class GuiWidgets {
        public UIButton acceptOptionsButton;

        public UICheckbox cameraRoll;
        public UICheckbox resetOnFocus;
        public UICheckbox zoomOutOnCursor;
        public UICheckbox pauseAfterLoad;

        public UIPopupList gameSpeedAfterLoad;
        public UIPopupList universeSize;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

