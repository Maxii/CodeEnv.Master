// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiManager.cs
// Overall GuiManager that handles the visibility of Gui elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using UnityEngine;

/// <summary>
/// Overall GuiManager that handles the visibility of Gui elements.
/// </summary>
public class GuiManager : AMonoSingleton<GuiManager> {

    private Stack<IList<UIPanel>> _stackedPanelsToReappear;
    private UIPanel[] _panelsThatNeverDisappear;

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        _stackedPanelsToReappear = new Stack<IList<UIPanel>>();
    }

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        UIPanel uiRootPanel = UIRoot.list[0].gameObject.GetSafeMonoBehaviourComponent<UIPanel>();
        UIPanel tooltipPanel = gameObject.GetSafeMonoBehaviourComponentInChildren<UITooltip>().gameObject.GetSafeMonoBehaviourComponent<UIPanel>();
        _panelsThatNeverDisappear = new UIPanel[] { uiRootPanel, tooltipPanel };
    }

    protected override void Start() {
        base.Start();
        CheckDebugSettings();
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void CheckDebugSettings() {
        DebugSettings debugSettings = DebugSettings.Instance;
        if (debugSettings.DisableGui) {
            GuiCameraControl.Instance.GuiCamera.enabled = false;
        }
        if (!debugSettings.EnableFpsReadout) {
            GameObject fpsReadoutParentGo = gameObject.GetSafeMonoBehaviourComponentInChildren<FpsReadout>().transform.parent.gameObject;
            fpsReadoutParentGo.SetActive(false);
        }
    }

    public void ChangeVisibilityOfUIElements(GuiVisibilityMode visMode, IEnumerable<UIPanel> exceptions) {
        switch (visMode) {
            case GuiVisibilityMode.Hidden:
                var activeUIRootChildPanelCandidates = gameObject.GetSafeMonoBehaviourComponentsInChildren<UIPanel>().Except(_panelsThatNeverDisappear);
                // Can't use UIPanel.list as it contains ALL active UIPanels, not just those in the 2DUI
                //D.Log("Active 2DUI UIPanels found on HideVisibleGuiPanels event: {0}{1}.", Constants.NewLine, activeUIRootChildPanelCandidates.Concatenate());
                var panelsToDisappear = activeUIRootChildPanelCandidates.Except(exceptions);
                ChangeVisibility(false, panelsToDisappear);
                _stackedPanelsToReappear.Push(panelsToDisappear.ToList<UIPanel>());
                break;
            case GuiVisibilityMode.Visible:
                D.Assert(_stackedPanelsToReappear.Count != Constants.Zero, "The stack holding the lists of UIPanels to restore should not be empty!");
                var panelsToReappear = _stackedPanelsToReappear.Pop().Except(exceptions);
                ChangeVisibility(true, panelsToReappear);
                break;
            case GuiVisibilityMode.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(visMode));
        }
    }

    private void ChangeVisibility(bool isVisible, IEnumerable<UIPanel> panels) {
        panels.ForAll(p => {
            NGUITools.SetActive(p.gameObject, isVisible);

            #region Alternative Visibility Control Approach Archive

            // There is an alternative way using the Panel's alpha property to manage the panel's widget's visibility. 
            // The issue I encountered was the current algorithm finds ALL panels, not just those onscreen, and
            // makes them invisible (deactivates them). While MyNguiPlayAnimation.ifDisabledOnPlay = enableThenPlay;
            // reactivates any UIPanels it intends to play, there is no such setting to fix the UIPanel if its alpha = 0.
            //var uiWindow = p.gameObject.GetComponent<UIWindow>();   // assumes max of 1 UIWindow per panel
            //if (uiWindow != null) {
            //    if (isVisible) {
            //        uiWindow.Show();
            //    }
            //    else {
            //        uiWindow.Hide();
            //    }
            //}
            //else {
            //    p.alpha = isVisible ? 1.0F : Constants.ZeroF;
            //}
            //D.Log("UIPanel {0} is {1}.", p.gameObject.name, visibility);

            #endregion

        });
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Restore All Panels on Game Change System Archive

    //public bool ReadyForSceneChange { get; private set; }

    //private void OnLoadSavedGame(LoadSavedGameEvent e) {
    //    MakeAllPanelsReappear();
    //}

    //private void OnBuildNewGame(BuildNewGameEvent e) {
    //    MakeAllPanelsReappear();
    //}

    /// <summary>
    /// Restores all panels to visibility in preparation for a scene change.
    /// </summary>
    //private void MakeAllPanelsReappear() {
    //    //D.Log("Restoring all panels to visibility in preparation for scene change.");
    //    for (int i = 0; i < _stackedPanelsToReappear.Count; i++) {
    //        var panelsToReappear = _stackedPanelsToReappear.Pop();
    //        ChangeVisibility(true, panelsToReappear);
    //    }
    //    //ReadyForSceneChange = true;
    //    // no need to reset this to false after as GuiManagers donot survive scene changes
    //}

    #endregion

}

