// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiSliderBase.cs
// Base class for  Gui Sliders built with NGUI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Base class for  Gui Sliders built with NGUI. 
/// </summary>
public abstract class GuiSliderBase : MonoBehaviourBase, IDisposable {

    protected PlayerPrefsManager playerPrefsMgr;
    protected UISlider slider;
    public string tooltip = string.Empty;

    void Awake() {
        playerPrefsMgr = PlayerPrefsManager.Instance;
    }

    void Start() {
        Initialize();
    }

    /// <summary>
    /// Override to initialize the tooltip message. Remember base.Initialize();
    /// </summary>
    protected virtual void Initialize() {
        slider = gameObject.GetSafeMonoBehaviourComponent<UISlider>();
        slider.onValueChange += OnSliderValueChange;
    }

    protected abstract void OnSliderValueChange(float value);

    void OnTooltip(bool toShow) {
        if (Utility.CheckForContent(tooltip)) {
            if (toShow) {
                UITooltip.ShowText(tooltip);
            }
            else {
                UITooltip.ShowText(null);
            }
        }
    }

    #region IDiposable
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            slider.onValueChange -= OnSliderValueChange;
        }
        // free unmanaged resources here
        alreadyDisposed = true;
    }

    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}

