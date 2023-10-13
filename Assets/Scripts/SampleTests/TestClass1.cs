using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YogeshSriraman.DI;

public class TestClass1 : MonoBehaviour
{
    [Inject(InjectFrom.Anywhere, InjectType.Class, useType: UseType.Reflection)]
    public Transform test1TR;

    [Inject(InjectFrom.Below, InjectType.Class, useType: UseType.Name, key: "Test2Transform")]
    public Transform test2TR;

    [Inject(InjectFrom.Anywhere, InjectType.Class, useType: UseType.Reflection)]
    public TestClass2 testClass2;

    [Inject(InjectFrom.Anywhere, InjectType.Class)]
    public TestClass3[] testClass3;

    [Inject(InjectFrom.Anywhere, InjectType.Interface, useType: UseType.Reflection, OnInjectCompleteAction: "OnInterfaceInjected")]
    public ITest iTestInterfaceObj;


    [ForceInject(InjectFrom.Anywhere, InjectType.Class, useType: UseType.Reflection)]
    public Transform forcedObject;

    public void OnInterfaceInjected(object[] objs)
    {
        //The objs array is an array of 1 element. That element is the injected item.
        //In this case, it is iTestInterfaceObj. Returned as object, it can then be casted into the type we need.

        ITest test = objs[0] as ITest;

        string testName = test.MyName;
        string itestName = iTestInterfaceObj.MyName;

        Debug.Log("Are these two interfaces the same object?? ->" + (testName == itestName));
    }
}
