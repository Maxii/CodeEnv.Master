// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterModel.cs
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
public class UniverseCenterModel : AItemModel, IUniverseCenterModel {

    public new ItemData Data {
        get { return base.Data as ItemData; }
        set { base.Data = value; }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

