// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICoroutineManager.cs
// Interface that allows Job to hold a reference to the MonoBehaviour CoroutineManager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Interface that allows Job to hold a reference to the MonoBehaviour CoroutineManager.
    /// </summary>
    public interface ICoroutineManager {

        Coroutine StartCoroutine(IEnumerator coroutine);

    }
}

