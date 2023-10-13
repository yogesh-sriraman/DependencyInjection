using System;

namespace YogeshSriraman.DI
{
    public enum InjectFrom
    {
        Above,
        Below,
        Here,

        Anywhere
    }

    public enum InjectType
    {
        Class,

        Interface
    }

    public enum UseType
    {
        Name,
        Reflection,
        None
    }

    /// <summary>
    /// Attribute that identifies dependencies to be injected
    /// </summary>
    public class InjectAttribute : Attribute
    {
        public InjectFrom InjectFrom { get; private set; }

        public InjectType InjectType { get; private set; }

        public UseType useType { get; private set; }
        public string key { get; private set; }

        public string OnInjectComplete { get; private set; }

        public InjectAttribute(InjectFrom injectFrom, string OnInjectCompleteAction = "")
        {
            this.InjectFrom = injectFrom;
            this.InjectType = InjectType.Class;
            this.useType = UseType.None;
            this.OnInjectComplete = OnInjectCompleteAction;
        }

        #region Ambiguity Fix
        /*public InjectAttribute(InjectFrom injectFrom, InjectType injectType, string OnInjectCompleteAction = "")
        {
            this.InjectFrom = injectFrom;
            this.InjectType = injectType;

            this.useType = UseType.None;
            this.OnInjectComplete = OnInjectCompleteAction;
        }*/
        #endregion

        //The above commented region causes ambiguity with the constructor below.

        public InjectAttribute(InjectFrom injectFrom, InjectType injectType,
            UseType useType = UseType.None,
            string key = "", string OnInjectCompleteAction = "")
        {
            this.InjectFrom = injectFrom;
            this.InjectType = injectType;
            this.OnInjectComplete = OnInjectCompleteAction;


            if(injectFrom == InjectFrom.Anywhere && useType == UseType.Name)
            {
                throw new Exception("Cannot use name for dependencies other than hierarchy");
                /*this.useType = UseType.None;
                this.key = "";*/
            }

            this.useType = useType;
            this.key = key;
            
        }
    }
}