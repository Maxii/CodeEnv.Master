// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DynamicTrackingLabels.cs
// Singleton for easy access to the DynamicTrackingLabels folder in the Scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;


/// <summary>
/// Singleton for easy access to the DynamicTrackingLabels folder in the Scene.
/// </summary>
public class DynamicTrackingLabels : MonoBehaviour {

    private static string folderName = typeof(DynamicTrackingLabels).Name;

    /// <summary>
    /// Gets the DynamicTrackingLabels folder.
    /// </summary>
    public static Transform Folder {
        get {
            if (Instance.gameObject.name != folderName) {
                D.Error("Expecting folder {0} but got {1}.", folderName, Instance.gameObject.name);
            }
            return Instance.transform;
        }
    }

    #region Custom MonoBehaviour Singleton Pattern
    private static DynamicTrackingLabels instance = null;
    public static DynamicTrackingLabels Instance {
        get {
            if (instance == null) {
                // value is required for the first time, so look for it
                instance = GameObject.FindObjectOfType(typeof(DynamicTrackingLabels)) as DynamicTrackingLabels;
                if (instance == null) {
                    // no instance created yet, so create one
                    GameObject dynamicGameObjectHUDsFolder = GameObject.Find(folderName);
                    if (dynamicGameObjectHUDsFolder == null) {
                        // No folder of this name is present in the scene. No point in creating one as we couldn't place it
                        // properly in the heirarchy so just report the error and return
                        D.Error("No {0} folder found.", folderName);
                        return null;
                    }
                    // our destination folder exists, so add our newly created instance to it
                    instance = dynamicGameObjectHUDsFolder.AddComponent<DynamicTrackingLabels>();
                }
                instance.Initialize();
            }
            return instance;
        }
    }

    void Awake() {
        // If no other MonoBehaviour has requested values in an Awake() call executing
        // before this one, then we are it. There is no reason to search for an object as we must be attached to it.
        if (instance == null) {
            instance = this;
            instance.Initialize();
        }
    }

    // Make sure the instance isn't referenced anymore
    void OnApplicationQuit() {
        instance = null;
    }
    #endregion

    private void Initialize() {
        // do any required initialization here as you would normally do in Awake()
        D.Log("A {0} instance is being initialized.", this.name);
    }

    void OnDestroy() {
        D.Log("A {0} instance is being destroyed.", this.name);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

