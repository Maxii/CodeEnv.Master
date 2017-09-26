// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MyDragDropItem.cs
// My version of UIDragDropItem that includes an event that fires when DragDrop has ended.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;

/// <summary>
/// My version of UIDragDropItem that adds an event that fires when DragDrop has ended.
/// </summary>
public class MyDragDropItem : UIDragDropItem {

    public event EventHandler dragDropEnded;

    public string DebugName { get { return GetType().Name; } }

    #region Event and Property Change Handlers

    protected override void OnDragDropEnd() {
        base.OnDragDropEnd();
        OnDragDropEnded();
    }

    private void OnDragDropEnded() {
        if (dragDropEnded != null) {
            dragDropEnded(this, EventArgs.Empty);
        }
    }

    #endregion

    public override string ToString() {
        return DebugName;
    }

}

