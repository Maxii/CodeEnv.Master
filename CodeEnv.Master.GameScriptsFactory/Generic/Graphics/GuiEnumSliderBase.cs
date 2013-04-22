// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiEnumSliderBase.cs
// Base class for  Gui Sliders built with NGUI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Generic base class for Gui Sliders that select enum values built with NGUI.
/// </summary>
/// <typeparam name="T">The enum type.</typeparam>
public abstract class GuiEnumSliderBase<T> : GuiTooltip where T : struct {

    protected GameEventManager eventMgr;
    private UISlider slider;
    private float[] orderedSliderStepValues;
    private T[] orderedTValues;

    void Awake() {
        InitializeOnAwake();
    }

    protected virtual void InitializeOnAwake() {
        eventMgr = GameEventManager.Instance;
        slider = gameObject.GetSafeMonoBehaviourComponent<UISlider>();
        InitializeSlider();
        InitializeSliderValue();
        // don't receive events until initializing is complete
        slider.onValueChange += OnSliderValueChange;
    }

    private void InitializeSlider() {
        T[] tValues = Enums<T>.GetValues().Except<T>(default(T)).ToArray<T>();
        int numberOfSliderSteps = tValues.Length;
        slider.numberOfSteps = numberOfSliderSteps;
        orderedTValues = tValues.OrderBy(tv => tv).ToArray<T>();    // assumes T has assigned values in ascending order
        orderedSliderStepValues = MyNguiUtilities.GenerateOrderedSliderStepValues(numberOfSliderSteps);
    }

    private void InitializeSliderValue() {
        PropertyInfo[] propertyInfos = typeof(PlayerPrefsManager).GetProperties();
        PropertyInfo propertyInfo = propertyInfos.SingleOrDefault<PropertyInfo>(p => p.PropertyType == typeof(T));
        if (propertyInfo != null) {
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            T tPrefsValue = propertyGet();
            int tPrefsValueIndex = orderedTValues.FindIndex<T>(tValue => (tValue.Equals(tPrefsValue)));
            float sliderValueAtTPrefsValueIndex = orderedSliderStepValues[tPrefsValueIndex];
            slider.sliderValue = sliderValueAtTPrefsValueIndex;
        }
        else {
            slider.sliderValue = orderedSliderStepValues[orderedSliderStepValues.Length - 1];
            Debug.LogWarning("No PlayerPrefsManager property found for {0}, so initializing slider to : {1}.".Inject(typeof(T), slider.sliderValue));
        }
    }

    // Note: UISlider automatically sends out an event to this method on Start()
    private void OnSliderValueChange(float sliderValue) {
        float tolerance = 0.05F;
        int index = orderedSliderStepValues.FindIndex<float>(v => Mathfx.Approx(sliderValue, v, tolerance));
        Arguments.ValidateNotNegative(index);
        T tValue = orderedTValues[index];
        OnSliderValueChange(tValue);
    }

    protected abstract void OnSliderValueChange(T value);

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

}

