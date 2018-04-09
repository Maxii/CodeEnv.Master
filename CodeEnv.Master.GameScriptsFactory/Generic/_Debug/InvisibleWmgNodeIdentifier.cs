// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: InvisibleWmgNodeIdentifier.cs
// Helper MonoBehaviour for use with GraphMaker Invisible Nodes.
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
/// Helper MonoBehaviour for use with GraphMaker Invisible Nodes. Allows an invisible node to 
/// be populated with a TreeNodeID either as a from Node or a to Node, but not both.
/// <remarks>Values can be populated either inside our outside TechTreeEditor. GraphMakerTreeHelper
/// uses this script to auto populate the links with their TreeLinkIDs.</remarks>
/// </summary>
public class InvisibleWmgNodeIdentifier : AMonoBase {

    public string DebugName { get { return GetType().Name; } }

    private bool IsFromNodeValid { get { return _fromTreeNodeID != default(TreeNodeID); } }

    private bool IsToNodeValid { get { return _toTreeNodeID != default(TreeNodeID); } }

    [SerializeField]
    private TreeNodeID _fromTreeNodeID = default(TreeNodeID);

    [SerializeField]
    private TreeNodeID _toTreeNodeID = default(TreeNodeID);

    public bool TryGetFromNode(out TreeNodeID fromNode) {
        bool isFromNodeValid = _fromTreeNodeID != default(TreeNodeID);
        D.Assert(isFromNodeValid != IsToNodeValid);

        fromNode = _fromTreeNodeID;
        return isFromNodeValid;
    }

    public bool TryGetToNode(out TreeNodeID toNode) {
        bool isToNodeValid = _toTreeNodeID != default(TreeNodeID);
        D.Assert(isToNodeValid != IsFromNodeValid);
        toNode = _toTreeNodeID;
        return isToNodeValid;
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

}

