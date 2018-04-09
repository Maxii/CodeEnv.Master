// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ResearchTreeLink.cs
// Rudimentary script for sprites used in a ResearchTree that represent a link between ResearchTreeNodes.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Helper script for sprites used in a ResearchTree that represent a link between ResearchTreeNodes. Holds a LinkID.
/// <remarks>Used to help determine the links that should be showing/not showing in a ResearchTree.</remarks>
/// <remarks>IMPROVE Links could change their sprite depending on node state (completed, etc.), or
/// OPTIMIZE if not used script can be removed after links are activated/deactivated.</remarks>
/// </summary>
public class ResearchTreeLink : AMonoBase {

    private const string DebugNameFormat = "{0}[{1}]";

    public string DebugName { get { return DebugNameFormat.Inject(typeof(ResearchTreeLink).Name, LinkID.DebugName); } }

    [SerializeField]
    private TreeLinkID _linkID; // Note: Disabling this field via a custom editor doesn't show the its TreeNodeID values
    public TreeLinkID LinkID {
        get {
            D.AssertNotDefault(_linkID);
            D.AssertNotDefault(_linkID.FromNodeID);
            D.AssertNotDefault(_linkID.ToNodeID);
            return _linkID;
        }
    }

    public void AddFromNodeID(TreeNodeID fromNodeID) {
        _linkID = new TreeLinkID(fromNodeID, _linkID.ToNodeID);
    }

    public void AddToNodeID(TreeNodeID toNodeID) {
        _linkID = new TreeLinkID(_linkID.FromNodeID, toNodeID);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

}

