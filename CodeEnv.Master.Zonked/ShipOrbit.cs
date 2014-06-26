// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipOrbit.cs
// Simple marker class that makes it easy to find ShipOrbit gameObjects.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Simple marker class that makes it easy to find ShipOrbit gameObjects.  The GameObject
/// this script is attached too allows ships to parent themselves to it, effectively
/// putting the ship into orbit around the IOrbitable Item.
/// </summary>
public class ShipOrbit : AMonoBase {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

