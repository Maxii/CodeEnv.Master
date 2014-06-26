// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: MoonModel.cs
// The data-holding class for all moons in the game.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The data-holding class for all moons in the game.  
/// </summary>
public class MoonModel : APlanetoidModel, IMoonModel /*, IShipOrbitable */{

    public new MoonData Data {
        get { return base.Data as MoonData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

