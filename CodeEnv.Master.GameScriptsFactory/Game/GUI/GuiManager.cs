// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiManager.cs
// Overall GuiManager that handles the showing state of Gui elements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Overall GuiManager that handles the showing state of the GUI's fixed panels.
/// </summary>
public class GuiManager : AMonoSingleton<GuiManager> {

#pragma warning disable 0649

    /// <summary>
    /// The fixed panels of the GUI that should normally be hidden when a popup shows.
    /// Use GuiShowModeControlButton.exceptions to exclude a panel listed here from being hidden.
    /// </summary>
    //[FormerlySerializedAs("fixedGuiPanels")]
    [Tooltip("The fixed panels of the GUI that should be hidden when a popup shows.")]
    [SerializeField]
    private List<UIPanel> _fixedGuiPanels;

#pragma warning restore 0649

    private IDictionary<GuiElementID, GameObject> _buttonLookup;
    private List<UIPanel> _hiddenPanels;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _hiddenPanels = new List<UIPanel>();
        if (GameManager.Instance.CurrentScene == SceneLevel.GameScene && _fixedGuiPanels.IsNullOrEmpty()) {
            D.WarnContext(gameObject, "{0}.fixedGuiPanels list is empty.", GetType().Name);
        }
        CheckDebugSettings();
        InitializeButtonClickSystem();
    }

    private void InitializeButtonClickSystem() {
        _buttonLookup = new Dictionary<GuiElementID, GameObject>();
        var allButtons = gameObject.GetSafeComponentsInChildren<UIButton>(includeInactive: true);
        var buttonGuiElements = allButtons.Select(b => b.gameObject.GetComponent<AGuiElement>()).Where(ge => ge != null);
        buttonGuiElements.ForAll(bge => {
            _buttonLookup.Add(bge.ElementID, bge.gameObject);
            D.Log("{0} added {1} to ButtonLookup.", GetType().Name, bge.ElementID.GetValueName());
        });
    }

    /// <summary>
    /// Calls "OnClick" on the gameObject associated with this buttonID.
    /// </summary>
    /// <param name="buttonID">The button identifier.</param>
    public void ClickButton(GuiElementID buttonID) {
        var buttonGo = _buttonLookup[buttonID];
        GameInputHelper.Instance.Notify(buttonGo, "OnClick");
    }

    public void ShowFixedPanels() {
        _hiddenPanels.ForAll(p => p.alpha = Constants.OneF);
        _hiddenPanels.Clear();
    }

    public void HideFixedPanels(IList<UIPanel> exceptions) {
        D.Assert(!_hiddenPanels.Any(), "{0} attempting to hide panels that are already hidden.".Inject(GetType().Name));
        WarnIfExceptionNotNeeded(exceptions);
        _hiddenPanels.AddRange(_fixedGuiPanels.Except(exceptions));
        _hiddenPanels.ForAll(p => p.alpha = Constants.ZeroF);
    }

    private void WarnIfExceptionNotNeeded(IEnumerable<UIPanel> exceptions) {
        exceptions.ForAll(e => {
            if (!_fixedGuiPanels.Contains(e)) {
                D.Warn("{0}: UIPanel exception {1} not needed.", GetType().Name, e.name);
            }
        });
    }

    //[System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void CheckDebugSettings() {
        DebugSettings debugSettings = DebugSettings.Instance;
        if (debugSettings.DisableGui) {
            GuiCameraControl.Instance.GuiCamera.enabled = false;
        }
        if (!debugSettings.EnableFpsReadout) {
            gameObject.GetSingleComponentInChildren<FpsReadout>().gameObject.SetActive(false);
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Stacked Panels Archive

    //private Stack<IList<UIPanel>> _stackedPanelsToReappear;

    /// <summary>
    /// Panels that are immune to visibility manipulation.
    /// </summary>
    //private IList<UIPanel> _immunePanels;

    //private IList<UIPanel> AcquireImmunePanels() {
    //    UIPanel uiRootPanel = UIRoot.list[0].gameObject.GetSafeMonoBehaviour<UIPanel>();
    //    UIPanel tooltipPanel = gameObject.GetSafeMonoBehaviourInChildren<TooltipHudWindow>().gameObject.GetSafeMonoBehaviour<UIPanel>();
    //    GameObject tempDebugGo = GameObject.Find("UI Debug");
    //    UIPanel[] tempDebugPanels = tempDebugGo.GetComponentsInChildren<UIPanel>(includeInactive: true);
    //    return new List<UIPanel>(tempDebugPanels) { uiRootPanel, tooltipPanel };
    //}

    //public void ChangeVisibilityOfUIElements(GuiVisibilityMode visMode, IEnumerable<UIPanel> exceptions) {
    //    switch (visMode) {
    //        case GuiVisibilityMode.Hidden:
    //            WarnIfExceptionNotNeeded(exceptions);
    //            var panelsToDisappear = fixedGuiPanels.Except(exceptions);
    //            ChangeVisibility(false, panelsToDisappear);
    //            _stackedPanelsToReappear.Push(panelsToDisappear.ToList<UIPanel>());
    //            break;
    //        case GuiVisibilityMode.Visible:
    //            if (GameManager.Instance.CurrentScene == SceneLevel.GameScene && _stackedPanelsToReappear.Count == Constants.Zero) {
    //                // if LobbyScene, there are currently no fixed UIElements to restore
    //                D.Warn("{0}: The stack holding the lists of UIPanels to restore should not be empty!", GetType().Name);
    //            }
    //            var panelsToReappear = _stackedPanelsToReappear.Pop();
    //            ChangeVisibility(true, panelsToReappear);
    //            break;
    //        case GuiVisibilityMode.None:
    //        default:
    //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(visMode));
    //    }
    //}

    //private void ChangeVisibility(bool isVisible, IEnumerable<UIPanel> panels) {
    //    panels.ForAll(p => {
    //        D.Log("{0}: Changing UIPanel {1} active state to {2}.", GetType().Name, p.name, isVisible);
    //        NGUITools.SetActive(p.gameObject, isVisible);

    //        #region Alternative Visibility Control Approach Archive

    //        p.alpha = isVisible ? Constants.OneF : Constants.ZeroF;

    //        #endregion
    //    });
    //}

    #endregion

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

