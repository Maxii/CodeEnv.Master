// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterItem.cs
// The Item at the center of the universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The Item at the center of the universe.
/// </summary>
public class UniverseCenterItem : AItem {

    public new Data Data {
        get { return base.Data as Data; }
        set { base.Data = value; }
    }

    protected override void SubscribeToDataValueChanges() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

