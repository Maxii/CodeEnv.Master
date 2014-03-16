﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Fleets.cs
// Easy access to Fleets folder in Scene. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Easy access to Fleets folder in Scene. 
/// </summary>
public class Fleets : AFolderAccess<Fleets> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}
