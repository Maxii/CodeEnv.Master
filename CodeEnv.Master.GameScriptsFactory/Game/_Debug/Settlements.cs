// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Settlements.cs
//  Easy access to Settlements folder in Scene.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
///  Easy access to Settlements folder in Scene.  
/// </summary>
public class Settlements : AFolderAccess<Settlements> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

