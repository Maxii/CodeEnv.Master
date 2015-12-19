// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: JobRunner.cs
// Singleton. A MonoBehaviour-based proxy for executing Jobs as Coroutines.
// Derived from P31 Job Manager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;

/// <summary>
/// Singleton. A MonoBehaviour-based proxy for executing Jobs as Coroutines.
/// Derived from P31 Job Manager.
/// </summary>
public class JobRunner : AMonoSingleton<JobRunner>, IJobRunner {

    public override bool IsPersistentAcrossScenes { get { return true; } }

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        Job.jobRunner = Instance;
    }

    protected override void Cleanup() {
        Job.jobRunner = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


