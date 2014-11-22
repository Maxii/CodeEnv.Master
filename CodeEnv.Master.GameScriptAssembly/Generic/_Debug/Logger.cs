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
/// within scripts. Use D.Log for all non-scripts.
/// Obsolete. Replaced by Editor.PlayerSettings.ScriptingDefineValues
/// </summary>
[System.Obsolete]
public class Logger : AMonoSingleton<Logger> {

    public bool enableScriptLogging;

    protected override bool IsPersistentAcrossScenes { get { return true; } }

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
            Instance.LogIfEnabled(format, paramList);
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

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

