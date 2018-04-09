// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
//File: ResearchForm.cs
/// AForm that displays the interactive Technology Tree in the ResearchWindow. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// AForm that displays the interactive Technology Tree in the ResearchWindow. 
/// <remarks>IMPROVE Make use of _activeLinkLookup with highlighting.</remarks>
/// </summary>
public class ResearchForm : AForm {

    public override FormID FormID { get { return FormID.Research; } }

    private TechResearchTreeNode _futureTechNode;
    private IDictionary<TreeNodeID, TechResearchTreeNode> _nodeLookup;
    private IDictionary<TreeLinkID, ResearchTreeLink> _linkLookup;
    private IDictionary<TreeNodeID, TechResearchTreeNode> _activeNodeLookup;
    private IDictionary<TreeLinkID, ResearchTreeLink> _activeLinkLookup;

    private bool _areMemberValuesAssigned;
    private GameManager _gameMgr;

    protected override void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        InventoryAllNodesAndLinks();
        _activeNodeLookup = new Dictionary<TreeNodeID, TechResearchTreeNode>();
        _activeLinkLookup = new Dictionary<TreeLinkID, ResearchTreeLink>();
    }

    private void InventoryAllNodesAndLinks() {
        _nodeLookup = gameObject.GetComponentsInChildren<TechResearchTreeNode>().ToDictionary(node => node.NodeID);
        _linkLookup = gameObject.GetComponentsInChildren<ResearchTreeLink>().ToDictionary(link => link.LinkID);

        foreach (var node in _nodeLookup.Values) {
            NGUITools.SetActive(node.gameObject, false);
        }
        foreach (var link in _linkLookup.Values) {
            NGUITools.SetActive(link.gameObject, false);
        }
    }

    public sealed override void PopulateValues() {
        if (!_areMemberValuesAssigned) {
            AssignValuesToMembers();
            InitializeFutureTechResearchHandling();
            _areMemberValuesAssigned = true;
        }
        else {
            RefreshMemberValues();
            RefreshMemberHighlights();
        }
    }

    protected override void AssignValuesToMembers() {
        D.Assert(!_areMemberValuesAssigned);
        var allRschTasks = _gameMgr.UserAIManager.ResearchMgr.GetAllResearchTasks();
        foreach (var task in allRschTasks) {
            Assign(task);
        }
    }

    private void Assign(ResearchTask rschTask) {
        Technology taskTech = rschTask.Tech;
        var taskTechNodeID = taskTech.NodeID;
        TechResearchTreeNode techNode = _nodeLookup[taskTechNodeID];
        NGUITools.SetActive(techNode.gameObject, true);
        _activeNodeLookup.Add(taskTechNodeID, techNode);
        techNode.UserResearchTask = rschTask;

        var techPrereqNodeIDs = taskTech.Prerequisites.Select(preqTech => preqTech.NodeID);
        var linkIDsToActivate = techPrereqNodeIDs.Select(preqNodeID => new TreeLinkID(preqNodeID, taskTechNodeID));
        var linksToActivate = linkIDsToActivate.Select(linkID => _linkLookup[linkID]);
        linksToActivate.ForAll(link => {
            NGUITools.SetActive(link.gameObject, true);
            _activeLinkLookup.Add(link.LinkID, link);
        });
    }

    private void InitializeFutureTechResearchHandling() {
        // find the FutureTech node for use when a previous FutureTechRschTask has completed
        int lastColumn = _activeNodeLookup.Keys.Select(nodeID => nodeID.Column).Max();
        _futureTechNode = _activeNodeLookup.Values.Single(node => node.NodeID.Column == lastColumn);

        _gameMgr.UserAIManager.ResearchMgr.futureTechRschCompleted += FutureTechRschCompletedEventHandler;
    }

    private void RefreshMemberValues() {
        _activeNodeLookup.Values.ForAll(node => node.RefreshMemberWidgetValues());
    }

    #region Event and Property Change Handlers

    private void FutureTechRschCompletedEventHandler(object sender, UserResearchManager.FutureTechResearchCompletedEventArgs e) {
        HandleFutureTechRschCompleted(e.NextFutureTechResearchTask);
    }

    #endregion

    public void HandleNodeClicked(ResearchTask nodeRschTask) {
        _gameMgr.UserAIManager.ResearchMgr.ChangeCurrentResearchTo(nodeRschTask);
        RefreshMemberHighlights();
    }

    private void HandleFutureTechRschCompleted(ResearchTask nextFutureTechRuntimeCreatedRschTask) {
        _futureTechNode.ResetForReuse();
        //D.Log("{0} is setting {1}'s ResearchTask to {2}.", DebugName, _futureTechNode.DebugName, nextFutureTechRuntimeCreatedRschTask.DebugName);
        _futureTechNode.UserResearchTask = nextFutureTechRuntimeCreatedRschTask;
    }

    private void RefreshMemberHighlights() {
        ResetUncompletedNodeHighlights();
        var userRschMgr = _gameMgr.UserAIManager.ResearchMgr;
        ResearchTask currentRschTask = userRschMgr.CurrentResearchTask;
        if (currentRschTask != TempGameValues.NoResearch) {
            // If NoResearch, nothing is queued so there should be no uncompleted highlights 
            TreeNodeID currentRschTechNodeID = currentRschTask.Tech.NodeID;
            var currentRschNode = _activeNodeLookup[currentRschTechNodeID];
            currentRschNode.Highlight(AResearchTreeNode.HighlightMode.Researching);

            IList<ResearchTask> queuedRschTasks;
            if (userRschMgr.TryGetQueuedResearchTasks(out queuedRschTasks)) {
                var nodesToHighlightAsQueued = queuedRschTasks.Select(qTask => qTask.Tech.NodeID).Select(nodeID => _activeNodeLookup[nodeID]);
                nodesToHighlightAsQueued.ForAll(node => node.Highlight(AResearchTreeNode.HighlightMode.Queued));
            }
        }
    }

    /// <summary>
    /// Resets the highlight state of all uncompleted nodes to None.
    /// <remarks>Completed Nodes set their own highlight state when they complete.</remarks>
    /// </summary>
    private void ResetUncompletedNodeHighlights() {
        var uncompletedNodes = _activeNodeLookup.Values.Where(node => !node.UserResearchTask.IsCompleted);
        uncompletedNodes.ForAll(node => node.Highlight(AResearchTreeNode.HighlightMode.None));
    }

    protected override void ResetForReuse_Internal() {
        // Do nothing. A ResearchForm doesn't get new content once initially populated
        // except for FutureTech handled by HandleFtureTechRschCompleted
    }

    protected override void Cleanup() {
        if (_gameMgr != null) {
            _gameMgr.UserAIManager.ResearchMgr.futureTechRschCompleted -= FutureTechRschCompletedEventHandler;
        }

    }

}

