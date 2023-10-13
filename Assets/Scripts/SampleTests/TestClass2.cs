using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YogeshSriraman.DI;

public class TestClass2 : MonoBehaviour, ITest
{
    [Inject(InjectFrom.Below, InjectType.Class, useType: UseType.Reflection, OnInjectCompleteAction: "OnComplete")]
    public Transform childGO;

    public void IAmTesting()
    {
        Debug.Log("I am an instance of TestClass2.\nI implement ITest interface");
    }

    public string MyName
    { 
        get => gameObject.name;
    }

    public string MyClass
    {
        get { return "TestClass2"; }
    }

    public void OnComplete(object[] objs)
    {
        //The objs array is an array of 1 element. That element is the injected item.
        //In this case, it is childGO. Returned as object, it can then be casted into the type we need.

        Transform childObj = objs[0] as Transform;

        Debug.Log("Are Injected Tranform and returned Object the same???? ->" + (childObj == childGO));
    }
}
