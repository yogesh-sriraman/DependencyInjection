using System;

namespace YogeshSriraman.DI
{
    /// <summary>
    /// Represents a member (property or field) of an object that have a dependency injected.
    /// </summary>
    public interface IInjectableMember
    {
        /// <summary>
        /// The one thing we want to do is set the value of the member.
        /// </summary>
        void SetValue(object owner, object value);

        /// <summary>
        /// Get the name  of the member.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Get the type of the member.
        /// </summary>
        Type MemberType { get; }

        /// <summary>
        /// The category of the member (field or property).
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Specifies where the dependency is allowed to be injected from.
        /// </summary>
        InjectFrom InjectFrom { get; }

        /// <summary>
        /// Specifies what type of the dependency is allowed to be injected.
        /// </summary>
        InjectType InjectType { get; }

        UseType useType { get; }
        string key { get; }

        string OnInjectComplete { get; }
    }
}