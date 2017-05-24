// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AMount.cs
// Abstract base class for a mount on a hull.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for a mount on a hull.
/// </summary>
public abstract class AMount : AMonoBase {

    public virtual string DebugName { get { return transform.name; } }

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        Validate();
    }

    protected virtual void InitializeValuesAndReferences() { }

    protected virtual void Validate() { }

    public sealed override string ToString() {
        return DebugName;
    }

}

