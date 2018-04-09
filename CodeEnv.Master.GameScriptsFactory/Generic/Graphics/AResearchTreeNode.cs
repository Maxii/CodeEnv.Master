// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AResearchTreeNode.cs
// Abstract AGuiWaitForInitializeMember for nodes of a research tree that show research progress and the
// content that will be enabled when research is completed.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract AGuiWaitForInitializeMember for nodes of a research tree that show research progress and the
/// content that will be enabled when research is completed.
/// </summary>
public abstract class AResearchTreeNode : AGuiWaitForInitializeMember {

    protected const string RemainingRschTimeFormat = "{0:0.}";

    [SerializeField]
    protected UISprite _nodeImageSprite = null;

    [SerializeField]
    protected UILabel _nodeNameLabel = null;

    [SerializeField]
    protected UIProgressBar _rschProgressBar = null;

    [SerializeField]
    protected UIWidget _timeToCompletionContainer = null;

#pragma warning disable 0649

    [SerializeField]
    protected UIWidget[] _enabledContentContainers;

#pragma warning restore 0649

    [SerializeField]
    private TreeNodeID _nodeID = default(TreeNodeID);
    public TreeNodeID NodeID {
        get {
            D.AssertNotDefault(_nodeID);
            return _nodeID;
        }
    }

    protected UILabel _timeToCompletionLabel;
    protected GameManager _gameMgr;

    protected sealed override void Awake() {
        if (__CheckForGraphMakerNode()) {
            // disables all colliders as we are in my GraphMaker ResearchTree Editor
            return;
        }
        base.Awake();
    }

    protected override void InitializeValuesAndReferences() {
        _timeToCompletionLabel = _timeToCompletionContainer.gameObject.GetSingleComponentInChildren<UILabel>();
        _gameMgr = GameManager.Instance;
        InitializeContentContainers();
        InitializeTimeToCompletionTooltip();
    }

    private void InitializeContentContainers() {
        foreach (var container in _enabledContentContainers) {
            MyEventListener.Get(container.gameObject).onTooltip += EnabledContentContainerTooltipEventHandler;
            MyEventListener.Get(container.gameObject).onClick += EnabledContentContainerClickedEventHandler;
            NGUITools.SetActive(container.gameObject, false);
        }
    }

    private void InitializeTimeToCompletionTooltip() {
        MyEventListener.Get(_timeToCompletionContainer.gameObject).onTooltip += TimeToCompletionTooltipEventHandler;
        MyEventListener.Get(_timeToCompletionContainer.gameObject).onClick += TimeToCompletionClickedEventHandler;
    }

    #region Event and Property Change Handlers

    protected abstract void EnabledContentContainerTooltipEventHandler(GameObject containerGo, bool show);

    protected abstract void TimeToCompletionTooltipEventHandler(GameObject containerGo, bool show);

    private void TimeToCompletionClickedEventHandler(GameObject containerGo) {
        // The TimeToCompletion sprite has a separate collider to provide tooltip info, but has no separate click functionality
        HandleNodeClicked();
    }

    private void EnabledContentContainerClickedEventHandler(GameObject containerGo) {
        // The EnabledContentContainer sprites have a separate collider to provide tooltip info, but has no separate click functionality
        HandleNodeClicked();
    }

    void OnClick() {
        HandleNodeClicked();
    }

    #endregion

    protected abstract void HandleNodeClicked();

    /// <summary>
    /// Refreshes the values of the member widgets.
    /// <remarks>Specific to AResearchTreeNodes as the node's UserResearchTask is only rarely set more than once,
    /// the only exception being the reuse of the node for FutureTech. However, each time the ResearchWindow
    /// shows, some of the widget values may need to be refreshed (progressBar, TimeToCompletion, etc.)</remarks>
    /// </summary>
    public void RefreshMemberWidgetValues() {
        D.Assert(IsInitialized);
        RefreshMemberWidgetValues_Internal();
    }

    protected abstract void RefreshMemberWidgetValues_Internal();

    #region Cleanup

    private void Unsubscribe() {
        foreach (var container in _enabledContentContainers) {
            MyEventListener.Get(container.gameObject).onTooltip -= EnabledContentContainerTooltipEventHandler;
            MyEventListener.Get(container.gameObject).onClick -= EnabledContentContainerClickedEventHandler;
        }
        MyEventListener.Get(_timeToCompletionContainer.gameObject).onTooltip -= TimeToCompletionTooltipEventHandler;
        MyEventListener.Get(_timeToCompletionContainer.gameObject).onClick -= TimeToCompletionClickedEventHandler;
    }

    public void Highlight(HighlightMode mode) {
        switch (mode) {
            case HighlightMode.None:
                _nodeImageSprite.color = TempGameValues.GeneralHighlightColor.ToUnityColor();
                break;
            case HighlightMode.Queued:
                _nodeImageSprite.color = TempGameValues.ResearchQueuedColor.ToUnityColor();
                break;
            case HighlightMode.Researching:
                _nodeImageSprite.color = TempGameValues.CurrentResearchColor.ToUnityColor();
                break;
            case HighlightMode.Completed:
                _nodeImageSprite.color = TempGameValues.DisabledColor.ToUnityColor();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mode));
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #endregion

    #region Debug

    private bool __CheckForGraphMakerNode() {
        var graphMakerNode = gameObject.GetComponent<WMG_Node>();
        bool toEnableColliders = graphMakerNode == null;
        __EnableColliders(toEnableColliders);
        return !toEnableColliders;
    }

    private void __EnableColliders(bool toEnable) {
        var allColliders = gameObject.GetComponentsInChildren<Collider>();
        foreach (var col in allColliders) {
            col.enabled = toEnable;
        }
    }

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        Utility.ValidateNotNullOrEmpty<UIWidget>(_enabledContentContainers);
        foreach (var container in _enabledContentContainers) {
            D.AssertNotNull(container);
        }
        D.AssertNotNull(_nodeImageSprite);
        D.AssertNotNull(_nodeNameLabel);
        D.AssertNotNull(_rschProgressBar);
        D.AssertNotNull(_timeToCompletionContainer);
    }


    #endregion

    #region Nested Classes

    public enum HighlightMode {
        None,
        Queued,
        Researching,
        Completed
    }

    #endregion



}

