// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RowElementIndexer.cs
// Debug class used with <c>RowOrganizer</c> to automate the positioning of elements 
// within a row according to the index setting contained here. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Debug class used with <c>RowOrganizer</c> to automate the positioning of elements 
/// within a row according to the index setting contained here. 
/// It is safe to leave on an element as it is only used by RowOrganizer which itself can only
/// be activated in edit mode.
/// </summary>
public class RowElementIndexer : AMonoBase {

    [Tooltip("Index of the element within the row beginning on the left at 0")]
    public int index = -1;

    protected override void Cleanup() { }

    public override string ToString() {
        return GetType().Name + " Index: " + index;
    }

}

