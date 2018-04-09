// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TechResearchTreeNode.cs
// AResearchTreeNode that displays info on a Technology that has been, is being or can be researched by the user.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// AResearchTreeNode that displays info on a Technology that has been, is being or can be 
/// researched by the user. Info includes research progress and a description including the
/// equipment and capability stats that are enabled by completing research of the tech.
/// Also allows the user to click on the Node to select it for research.
/// </summary>
public class TechResearchTreeNode : AResearchTreeNode {

    private const string DebugNameFormat = "{0}[{1}]";

    public override string DebugName {
        get {
            if (UserResearchTask == null) {
                return base.DebugName;
            }
            return DebugNameFormat.Inject(base.DebugName, UserResearchTask.Tech.DebugName);
        }
    }

    private ResearchTask _userResearchTask;
    public ResearchTask UserResearchTask {
        get { return _userResearchTask; }
        set {
            D.AssertNull(_userResearchTask);
            _userResearchTask = value;
            ResearchTaskPropSetHandler();
        }
    }

    public override bool IsInitialized { get { return _userResearchTask != null; } }

    protected override string TooltipContent { get { return UserResearchTask.Tech.Name; } }

    private ResearchForm _parentRschForm;
    private ResearchForm ParentRschForm {
        get {
            _parentRschForm = _parentRschForm ?? gameObject.GetSingleComponentInParents<ResearchForm>();
            return _parentRschForm;
        }
    }

    /// <summary>
    /// Lookup for the stats that would be enabled if/when this tech is researched, keyed by the Output container's gameObject. 
    /// Used to show the right tooltip when the container is hovered over.
    /// </summary>
    private IDictionary<GameObject, AImprovableStat> _statLookup;

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _statLookup = new Dictionary<GameObject, AImprovableStat>();
    }

    #region Event and Property Change Handlers

    protected override void EnabledContentContainerTooltipEventHandler(GameObject containerGo, bool show) {
        if (show) {
            AImprovableStat stat = _statLookup[containerGo];
            TooltipHudWindow.Instance.Show(stat);
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    protected override void TimeToCompletionTooltipEventHandler(GameObject containerGo, bool show) {
        if (show) {
            string remainingRschTimeText = RemainingRschTimeFormat.Inject(UserResearchTask.TimeToComplete.TotalInHours);
            //D.Log("{0} is showing {1} hours in TimeToCompletion Tooltip.", DebugName, remainingRschTimeText);
            TooltipHudWindow.Instance.Show(remainingRschTimeText);
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

    private void ResearchTaskPropSetHandler() {
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();

        ResearchTask rschTask = UserResearchTask;
        Technology tech = rschTask.Tech;
        _nodeImageSprite.atlas = tech.ImageAtlasID.GetAtlas();
        _nodeImageSprite.spriteName = tech.ImageFilename;
        _nodeNameLabel.text = tech.Name;

        AImprovableStat[] stats = tech.GetEnabledStats();
        for (int i = Constants.Zero; i < stats.Length; i++) {
            UIWidget container = _enabledContentContainers[i];
            NGUITools.SetActive(container.gameObject, true);

            UISprite iconSprite = container.gameObject.GetSingleComponentInChildren<UISprite>();
            AImprovableStat stat = stats[i];
            iconSprite.atlas = stat.ImageAtlasID.GetAtlas();
            iconSprite.spriteName = stat.ImageFilename;
            _statLookup.Add(container.gameObject, stat);
        }

        if (UserResearchTask.IsCompleted) {
            NGUITools.SetActive(_rschProgressBar.gameObject, false);
            NGUITools.SetActive(_timeToCompletionContainer.gameObject, false);
            Highlight(HighlightMode.Completed);
        }
        else {
            if (!_rschProgressBar.gameObject.activeSelf) {
                NGUITools.SetActive(_rschProgressBar.gameObject, true);
                NGUITools.SetActive(_timeToCompletionContainer.gameObject, true);
            }
            _rschProgressBar.value = rschTask.CompletionPercentage;
            _timeToCompletionLabel.text = RemainingRschTimeFormat.Inject(rschTask.TimeToComplete.TotalInHours);
        }
    }

    protected override void RefreshMemberWidgetValues_Internal() {
        //D.Log("{0} is refreshing its progress bar and time to completion values.", DebugName);
        if (UserResearchTask.IsCompleted) {
            NGUITools.SetActive(_rschProgressBar.gameObject, false);
            NGUITools.SetActive(_timeToCompletionContainer.gameObject, false);
            Highlight(HighlightMode.Completed);
        }
        else {
            _rschProgressBar.value = UserResearchTask.CompletionPercentage;
            _timeToCompletionLabel.text = RemainingRschTimeFormat.Inject(UserResearchTask.TimeToComplete.TotalInHours);
        }
    }

    protected override void HandleNodeClicked() {
        if (DebugControls.Instance.UserSelectsTechs) {
            if (!UserResearchTask.IsCompleted) {
                D.Log("User has selected {0} for research.", DebugName);
                ParentRschForm.HandleNodeClicked(UserResearchTask);
            }
        }
    }

    /// <summary>
    /// Resets this GuiElement instance so it can be reused.
    /// </summary>
    public override void ResetForReuse() {
        _userResearchTask = null;
        _statLookup.Clear();
        _rschProgressBar.value = Constants.ZeroPercent;
        _timeToCompletionLabel.text = null;
    }


}

