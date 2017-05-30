﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationStationPlaceholder.cs
// Placeholder for a formation station. 
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
/// Placeholder for a formation station. 
/// </summary>
public class FormationStationPlaceholder : AFormationStation {

    public string DebugName { get { return GetType().Name; } }

    public FormationStationSlotID slotID = FormationStationSlotID.None; // public so FormationGridOrganizer can change it in edit mode

    [SerializeField]
    private bool _isHQ = false;

    [SerializeField]
    private bool _isReserve = false;

    public FormationStationSlotID SlotID { get { return slotID; } }

    public bool IsHQ { get { return _isHQ; } }

    public bool IsReserve { get { return _isReserve; } }

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
    }

    protected override void ValidateOnAwake() {
        base.ValidateOnAwake();
        D.AssertNotDefault((int)SlotID);
        D.AssertNotNull(gameObject.GetSingleComponentInChildren<MeshRenderer>());
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

}

