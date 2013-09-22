// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: D.cs
// Logger under Management with enabled button for use in controlling log messages
// within scripts. Use D.Log for all non-scripts.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Logger under Management with enabled button for use in controlling log messages
// within scripts. Use D.Log for all non-scripts.
/// </summary>
public class Logger : AMonoBehaviourBaseSingletonInstanceIdentity<Logger> {

    public bool enableScriptLogging;

    protected override void Awake() {
        base.Awake();
        if (TryDestroyExtraCopies()) {
            return;
        }
    }

    /// <summary>
    /// Ensures that no matter how many scenes this Object is
    /// in (having one dedicated to each scene may be useful for testing) there's only ever one copy
    /// in memory if you make a scene transition.
    /// </summary>
    /// <returns><c>true</c> if this instance is going to be destroyed, <c>false</c> if not.</returns>
    private bool TryDestroyExtraCopies() {
        if (_instance && _instance != this) {
            // avoid calling Log when the instance is being destroyed
            Debug.Log("{0}_{1} found as extra. Initiating destruction sequence.".Inject(this.name, InstanceID));
            Destroy(gameObject);
            return true;
        }
        else {
            DontDestroyOnLoad(gameObject);
            _instance = this;
            return false;
        }
    }

    /// <summary>
    /// Sends the specified message to the Unity.Debug log.
    /// </summary>
    /// <remarks>Use this when including MyObject.ToString() which contains {} that string.Format() doesn't like.</remarks>
    /// <param name="message">The string message.</param>
    public static void Log(string message) {
        if (_instance) {
            _instance.LogIfEnabled(message);
        }
    }

    private void LogIfEnabled(string message) {
        if (enableScriptLogging) {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// Sends the specified message object (typically a composite message string) to the Unity.Debug
    /// log.
    /// </summary>
    /// <param name="format">The message object, typically a composite message string.</param>
    /// <param name="paramList">The paramaters to insert into the composite message string.</param>
    public static void Log(object format, params object[] paramList) {
        if (_instance) {
            _instance.LogIfEnabled(format, paramList);
        }
    }

    private void LogIfEnabled(object format, params object[] paramList) {
        if (enableScriptLogging) {
            if (format is string) {
                Debug.Log(string.Format(format as string, paramList));
            }
            else {
                Debug.Log(format);
            }
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

