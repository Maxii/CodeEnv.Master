// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CoroutineManager.cs
// Singleton. Coroutine Manager derived from P31 Job Manager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Singleton. Coroutine Manager derived from P31 Job Manager.
/// </summary>
public class CoroutineManager : AMonoBehaviourBaseSingleton<CoroutineManager> {

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

