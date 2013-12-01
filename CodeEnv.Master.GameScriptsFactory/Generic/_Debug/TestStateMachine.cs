// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TestStateMachine.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class TestStateMachine : AMonoStateMachine<TestEnum> {

    //public event Action EventTest;

    protected override void Awake() {
        base.Awake();
        LogEvent();

        CurrentState = TestEnum.Test1;
    }

    protected override void Start() {
        base.Start();
        LogEvent();

        CurrentState = TestEnum.Test2;
    }


    IEnumerator Test1_EnterState() {
        LogEvent();
        yield return null;
    }

    IEnumerator Test1_ExitState() {
        LogEvent();
        yield return null;
    }

    IEnumerator Test2_EnterState() {
        LogEvent();
        //EventTest();  // Wiring has been disconnected
        yield return null;
    }

    IEnumerator Test2_ExitState() {
        LogEvent();
        yield return null;
    }

    void Test2_OnEventTest_StateMachineTestObject() {
        D.Log("EventTest event received on Test2_OnEventTest_StateMachineTestObject().");
        CurrentState = TestEnum.Test1;
    }

    //void Test2_OnEventTest() {
    //    D.Log("EventTest event received on Test2_OnEventTest().");
    //    CurrentState = TestEnum.Test1;
    //}


    //protected override IEnumerable<object> OnWireEvent(MonoStateMachine.EventDef eventInfo) {
    //    if (eventInfo.eventName == "EventTest") {
    //        var list = new List<object>(1);
    //        list.Add(gameObject);
    //        return list;
    //    }
    //    return null;
    //}


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

public enum TestEnum {

    Test1 = 0,

    Test2 = 1
}

