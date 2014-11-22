// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: CoroutineManager.cs
// Singleton. Coroutine Manager, a MonoBehaviour-based platform for launching Coroutines.
// Derived from P31 Job Manager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Singleton. Coroutine Manager, a MonoBehaviour-based platform for launching Coroutines.
/// Derived from P31 Job Manager.
/// </summary>
public class CoroutineManager : AMonoBaseSingleton<JobRunner>, IJobRunner {

    protected override void Awake() {
        base.Awake();
        Job.jobRunner = Instance;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

