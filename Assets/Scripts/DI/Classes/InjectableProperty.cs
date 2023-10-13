using System;
using System.Linq;
using System.Reflection;

namespace YogeshSriraman.DI
{
    /// <summary>
    /// Represents a property of an object that have a dependency injected.
    /// </summary>
    public class InjectableProperty : IInjectableMember
    {
        private PropertyInfo propertyInfo;

        public InjectableProperty(PropertyInfo propertyInfo)
        {
            this.propertyInfo = propertyInfo;
            var injectAttribute = propertyInfo.GetCustomAttributes(typeof(InjectAttribute), false)
                .Cast<InjectAttribute>()
                .Single();
            this.InjectFrom = injectAttribute.InjectFrom;
            this.InjectType = injectAttribute.InjectType;
            this.useType = injectAttribute.useType;
            this.key = injectAttribute.key;
            this.OnInjectComplete = injectAttribute.OnInjectComplete;
        }

        /// <summary>
        /// The one thing we want to do is set the value of the member.
        /// </summary>
        public void SetValue(object owner, object value)
        {
            propertyInfo.SetValue(owner, value, null);
        }

        /// <summary>
        /// Get the name of the member.
        /// </summary>
        public string Name
        {
            get
            {
                return propertyInfo.Name;
            }
        }

        /// <summary>
        /// Get the type of the member.
        /// </summary>
        public Type MemberType
        {
            get
            {
                return propertyInfo.PropertyType;
            }
        }

        /// <summary>
        /// The category of the member (field or property).
        /// </summary>
        public string Category
        {
            get
            {
                return "property";
            }
        }

        /// <summary>
        /// Specifies where the dependency is allowed to be injected from.
        /// </summary>
        public InjectFrom InjectFrom
        {
            get;
            private set;
        }

        /// <summary>
        /// Specifies what type of the dependency is allowed to be injected.
        /// </summary>
        public InjectType InjectType
        {
            get;
            private set;
        }

        public UseType useType
        {
            get;
            private set;
        }

        public string key
        {
            get;
            private set;
        }

        public string OnInjectComplete
        {
            get;
            private set;
        }
    }
}