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
using CodeEnv.Master.GameContent;

/// <summary>
/// AForm that displays the interactive Technology Tree in the ResearchWindow. 
/// </summary>
public class ResearchForm : AForm {

    public override FormID FormID { get { return FormID.Research; } }

#pragma warning disable 0414
    private UIScrollView _tableScrollView;
    private GameManager _gameMgr;
#pragma warning restore 0414

    protected override void InitializeValuesAndReferences() {
        _gameMgr = GameManager.Instance;
        _tableScrollView = GetComponentInChildren<UIScrollView>();
        // TODO
    }

    public sealed override void PopulateValues() {
        AssignValuesToMembers();
    }

    protected override void AssignValuesToMembers() {
        // TODO
    }

    protected override void ResetForReuse_Internal() {
        // TODO
    }

    protected override void Cleanup() { }

}

