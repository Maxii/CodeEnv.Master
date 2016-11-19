// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFormationStation.cs
// Abstract base class for a formation station.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

/// <summary>
/// Abstract base class for a formation station.
/// </summary>
public abstract class AFormationStation : AMonoBase {

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        Validate();
    }

    protected virtual void InitializeValuesAndReferences() { }

    protected virtual void Validate() { }


}

