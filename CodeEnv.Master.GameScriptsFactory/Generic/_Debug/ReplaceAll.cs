// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ReplaceAll.cs
// Example auto replacement script for use in the editor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Example auto replacement script for use in the editor.
/// <remarks>This particular version was used to replace all Ngui UIPlaySound scripts (hundreds)
/// with my own version with some rudimentary initialization and warnings.</remarks>
/// </summary>
public class ReplaceAll : AMonoBase {

    public AudioClip tap;

    public string DebugName { get { return GetType().Name; } }

    [ContextMenu("Execute")]
    private void Replace() {
        D.Log("{0}.Replace() called.", DebugName);
        UIPlaySound[] componentsToReplace = GetComponentsInChildren<UIPlaySound>();
        D.Log("{0} found {1} components.", DebugName, componentsToReplace.Length);
        IList<GameObject> impactedGameObjects = new List<GameObject>();
        foreach (var comp in componentsToReplace) {
            impactedGameObjects.Add(comp.gameObject);
            if (comp.audioClip != tap) {
                D.Warn("{0}: GameObject named {1} uses an audioClip called {2}.", DebugName, comp.gameObject.name, comp.audioClip.name);
            }
            DestroyImmediate(comp);
        }

        foreach (var go in impactedGameObjects) {
            var addedPlaySFX = go.AddMissingComponent<MyPlaySFX>();
            addedPlaySFX.trigger = MyPlaySFX.Trigger.OnClick;
            addedPlaySFX.sfxClipID = SfxClipID.Tap;
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return DebugName;
    }

}

