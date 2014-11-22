// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IJobRunner.cs
// Interface that allows Job to hold a reference to the MonoBehaviour JobRunner.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Interface that allows Job to hold a reference to the MonoBehaviour JobRunner.
    /// </summary>
    public interface IJobRunner {

        Coroutine StartCoroutine(IEnumerator coroutine);

    }
}

