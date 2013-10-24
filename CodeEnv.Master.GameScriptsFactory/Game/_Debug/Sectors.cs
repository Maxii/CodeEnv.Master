// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Sectors.cs
// Easy access to the Sectors folder.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Easy access to the Sectors folder.
/// </summary>
public class Sectors : AFolderAccess<Sectors> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

