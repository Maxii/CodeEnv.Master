﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGuiEnumSliderBase.cs
// Base class for  Gui Sliders built with NGUI.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using System.Reflection;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Generic base class for Gui Sliders that select enum values built with NGUI.
/// </summary>
/// <typeparam name="T">The enum type.</typeparam>
public abstract class AGuiEnumSliderBase<T> : GuiTooltip where T : struct {

    protected GameEventManager _eventMgr;
    private UISlider _slider;
    private float[] _orderedSliderStepValues;
    private T[] _orderedTValues;

    protected override void Awake() {
        base.Awake();
        _eventMgr = GameEventManager.Instance;
        _slider = gameObject.GetSafeMonoBehaviourComponent<UISlider>();
        InitializeSlider();
        InitializeSliderValue();
        GameStatus.Instance.onIsRunningOneShot += OnIsRunning;
    }

    private void InitializeSlider() {
        T[] tValues = Enums<T>.GetValues().Except<T>(default(T)).ToArray<T>();
        int numberOfSliderSteps = tValues.Length;
        _slider.numberOfSteps = numberOfSliderSteps;
        _orderedTValues = tValues.OrderBy(tv => tv).ToArray<T>();    // assumes T has assigned values in ascending order
        //D.Log("T is {0}. OrderedTValues = {1}.", typeof(T).Name, _orderedTValues.Concatenate());
        _orderedSliderStepValues = MyNguiUtilities.GenerateOrderedSliderStepValues(numberOfSliderSteps);
        //D.Log("OrderedSliderSteps = {0}.", _orderedSliderStepValues.Concatenate());
    }

    private void InitializeSliderValue() {
        PropertyInfo[] propertyInfos = typeof(PlayerPrefsManager).GetProperties();
        PropertyInfo propertyInfo = propertyInfos.SingleOrDefault<PropertyInfo>(p => p.PropertyType == typeof(T));
        if (propertyInfo != null) {
            Func<T> propertyGet = (Func<T>)Delegate.CreateDelegate(typeof(Func<T>), PlayerPrefsManager.Instance, propertyInfo.GetGetMethod());
            T tPrefsValue = propertyGet();
            int tPrefsValueIndex = _orderedTValues.FindIndex<T>(tValue => (tValue.Equals(tPrefsValue)));
            float sliderValueAtTPrefsValueIndex = _orderedSliderStepValues[tPrefsValueIndex];
            _slider.value = sliderValueAtTPrefsValueIndex;
            //D.Log("{0}.sliderValue initialized to {1}.", GetType().Name, _slider.value);
        }
        else {
            _slider.value = _orderedSliderStepValues[_orderedSliderStepValues.Length - 1];
            D.Warn("No PlayerPrefsManager property found for {0}, so initializing slider to : {1}.".Inject(typeof(T), _slider.value));
        }
    }

    private void OnIsRunning() {
        // defer connecting slider value change events until running
        // Note: UIProgressBar automatically sends a value change event on Start() if the delegate isn't null
        EventDelegate.Add(_slider.onChange, OnSliderValueChange);
    }

    private void OnSliderValueChange() {
        float tolerance = 0.05F;
        float sliderValue = UISlider.current.value;
        int index = _orderedSliderStepValues.FindIndex<float>(v => Mathfx.Approx(sliderValue, v, tolerance));
        Arguments.ValidateNotNegative(index);
        T tValue = _orderedTValues[index];
        //D.Log("{0}.index = {1}, TValue = {2}.", GetType().Name, index, tValue);
        OnSliderTValueChange(tValue);
    }

    protected abstract void OnSliderTValueChange(T value);

    // IDisposable Note: No reason to remove Ngui event currentListeners OnDestroy() as the EventListener or
    // Delegate to be removed is attached to this same GameObject that is being destroyed. In addition,
    // execution is problematic as the gameObject may have already been destroyed.

}

