using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YogeshSriraman.DI
{
    public class ForceInjectAttribute : InjectAttribute
    {
        public ForceInjectAttribute(InjectFrom injectFrom, string OnInjectCompleteAction = "")
            : base(injectFrom, OnInjectCompleteAction)
        {
        }

        #region Ambiguity Fix
        /*public ForceInjectAttribute(InjectFrom injectFrom, InjectType injectType,
            string OnInjectCompleteAction = "")
            : base(injectFrom, injectType, OnInjectCompleteAction)
        {
        }*/
        #endregion

        //The above commented region causes ambiguity with the constructor below.
        public ForceInjectAttribute(InjectFrom injectFrom, InjectType injectType,
            UseType useType = UseType.None, string key = "", string OnInjectCompleteAction = "")
            : base(injectFrom, injectType, useType, key, OnInjectCompleteAction)
        {
        }
    }
}
