using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace YogeshSriraman.DI
{
    public class DependencyResolver
    {

        #region ResolveScene (Call from external class)
        /// <summary>
        /// Resolve all depenencies in the entire scene.
        /// </summary>
        public void ResolveScene()
        {
            var allGameObjects = GameObject.FindObjectsOfType<GameObject>();
            Resolve(allGameObjects);
        }

        /// <summary>
        /// Resolve a subset of the hierarchy downwards from a particular object.
        /// </summary>
        public void Resolve(GameObject parent)
        {
            var gameObjects = new GameObject[] { parent };
            Resolve(gameObjects);
        }

        /// <summary>
        /// Resolve dependencies for the hierarchy downwards from a set of game objects.
        /// </summary>
        private void Resolve(IEnumerable<GameObject> gameObjects)
        {
            var injectables = new List<Component>();
            FindObjects(gameObjects, injectables); // Scan the scene for objects of interest!

            foreach (var injectable in injectables)
            {
                ResolveDependencies(injectable);
            }
        }
        #endregion

        #region Find Objects of interest
        /// <summary>
        /// Enumerate the scene and find objects of interest to the dependency resolver.
        /// 
        /// WARNING: This function can be expensive. Call it only once and cache the result if you need it.
        /// </summary>
        public void FindObjects(IEnumerable<GameObject> allGameObjects, List<Component> injectables)
        {
            foreach (var gameObject in allGameObjects)
            {
                foreach (var component in gameObject.GetComponents<Behaviour>())
                {
                    var componentType = component.GetType();
                    var hasInjectableProperties = componentType.GetProperties()
                        .Where(IsMemberInjectable)
                        .Any();
                    if (hasInjectableProperties)
                    {
                        injectables.Add(component);
                    }
                    else
                    {
                        var hasInjectableFields = componentType.GetFields()
                            .Where(IsMemberInjectable)
                            .Any();
                        if (hasInjectableFields)
                        {
                            injectables.Add(component);
                        }
                    }

                    if (componentType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(IsMemberInjectable)
                        .Any())
                    {
                        Debug.LogError("Private properties should not be marked with [Inject] atttribute!", component);
                    }

                    if (componentType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(IsMemberInjectable)
                        .Any())
                    {
                        Debug.LogError("Private fields should not be marked with [Inject] atttribute!", component);
                    }
                }
            }
        }

        /// <summary>
        /// Determine if a member requires dependency resolution, checks if it has the 'Inject' attribute.
        /// </summary>
        private bool IsMemberInjectable(MemberInfo member)
        {
            return member.GetCustomAttributes(true)
                .Where(attribute => attribute is InjectAttribute ||
                attribute is ForceInjectAttribute)
                .Count() > 0;
        }
        #endregion

        #region Resolve Dependencies
        /// <summary>
        /// Resolve dependenies for an 'injectable' object.
        /// </summary>
        private void ResolveDependencies(Component injectable)
        {
            var injectableProperties = FindInjectableMembers(injectable);
            foreach (var injectableMember in injectableProperties)
            {
                ResolveMemberDependency(injectable, injectableMember);
            }
        }

        /// <summary>
        /// Use C# reflection to find all members of an object that require dependency resolution and injection.
        /// </summary>
        private IEnumerable<IInjectableMember> FindInjectableMembers(Component injectable)
        {
            var type = injectable.GetType();
            var injectableProperties = type.GetProperties()
                .Where(IsMemberInjectable)
                .Select(property => new InjectableProperty(property))
                .Cast<IInjectableMember>();

            var injectableFields = type.GetFields()
                .Where(IsMemberInjectable)
                .Select(field => new InjectableField(field))
                .Cast<IInjectableMember>();

            return injectableProperties.Concat(injectableFields);
        }

        /// <summary>
        /// Resolve a member dependency and inject the resolved valued.
        /// </summary>
        private void ResolveMemberDependency(Component injectable, IInjectableMember injectableMember)
        {
            if (injectableMember.InjectFrom == InjectFrom.Above)
            {
                if (!ResolveMemberDependencyFromHierarchy(injectable, injectableMember, true))
                {
                    Debug.LogError(
                        "Failed to resolve dependency for " + injectableMember.Category + ". Member: " + injectableMember.Name + ", Behaviour: " + injectable.GetType().Name + ", GameObject: " + injectable.gameObject.name + "\r\n" +
                        "Failed to find a dependency that matches " + injectableMember.MemberType.Name + ".",
                        injectable
                    );
                }
            }
            else if (injectableMember.InjectFrom == InjectFrom.Below)
            {
                if (!ResolveMemberDependencyFromHierarchy(injectable, injectableMember, false))
                {
                    Debug.LogError(
                        "Failed to resolve dependency for " + injectableMember.Category + ". Member: " + injectableMember.Name + ", Behaviour: " + injectable.GetType().Name + ", GameObject: " + injectable.gameObject.name + "\r\n" +
                        "Failed to find a dependency that matches " + injectableMember.MemberType.Name + ".",
                        injectable
                    );
                }
            }
            else if (injectableMember.InjectFrom == InjectFrom.Anywhere)
            {
                if (!ResolveMemberDependencyFromAnywhere(injectable, injectableMember))
                {
                    Debug.LogError(
                        "Failed to resolve dependency for " + injectableMember.Category + ". Member: " + injectableMember.Name + ", Behaviour: " + injectable.GetType().Name + ", GameObject: " + injectable.gameObject.name + "\r\n" +
                        "Failed to find a dependency that matches " + injectableMember.MemberType.Name + ".",
                        injectable
                    );
                }
            }
            else if (injectableMember.InjectFrom == InjectFrom.Here)
            {
                if (!ResolveMemberDependencyFromHere(injectable, injectableMember))
                {
                    Debug.LogError(
                        "Failed to resolve dependency for " + injectableMember.Category + ". Member: " + injectableMember.Name + ", Behaviour: " + injectable.GetType().Name + ", GameObject: " + injectable.gameObject.name + "\r\n" +
                        "Failed to find a dependency that matches " + injectableMember.MemberType.Name + ".",
                        injectable
                    );
                }
            }
            else
            {
                throw new ApplicationException("Unexpected use of InjectFrom enum: " + injectableMember.InjectFrom);
            }
        }
        #endregion

        #region Resolve From Here
        private bool ResolveMemberDependencyFromHere(Component injectable,
            IInjectableMember injectableMember)
        {
            Component[] behaviours = injectable.GetComponentsInChildren<Component>(
                includeInactive: false);

            if(injectableMember.MemberType.IsArray)
            {
                return ResolveArrayDependencyInHere(injectable, injectableMember);
            }
            else
            {
                return ResolveObjectDependencyInHere(injectable, injectableMember);
            }
        }

        private bool ResolveArrayDependencyInHere(Component injectable,
            IInjectableMember injectableMember)
        {
            var elementType = injectableMember.MemberType.GetElementType();
            var toInject = FindDependenciesInHere(injectable, injectableMember);
            if (toInject != null && toInject.Length > 0)
            {
                try
                {
                    Debug.Log("Injecting array of " + toInject.Length + " elements into " + injectable.GetType().Name + " at " + injectableMember.Category + " " + injectableMember.Name + " on GameObject '" + injectable.name + "'.", injectable);

                    foreach (var component in toInject.Cast<Component>())
                    {
                        Debug.Log("> Injecting object " + component.GetType().Name + " (GameObject: '" + component.gameObject.name + "').", injectable);
                    }

                    // 
                    // Create an appropriately typed array so that we don't get a type error when setting the value.
                    //
                    var typedArray = Array.CreateInstance(elementType, toInject.Length);
                    Array.Copy(toInject, typedArray, toInject.Length);

                    injectableMember.SetValue(injectable, typedArray);
                    CallOnCompleteIfExists(injectable, injectableMember, toInject);

                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, injectable); // Bad type??!
                }

                return true;
            }
            else
            {
                //Failed to find matches
                return false;
            }
        }

        private bool ResolveObjectDependencyInHere(Component injectable,
            IInjectableMember injectableMember)
        {
            var toInject = FindDependencyInHere(injectable, injectableMember);
            if (toInject != null)
            {
                try
                {
                    Debug.Log("Injecting " + toInject.GetType().Name +
                        " from this (GameObject: '" + toInject.gameObject.name +
                        "') into " + injectable.GetType().Name + " at " +
                        injectableMember.Category + " " + injectableMember.Name +
                        " on GameObject '" + injectable.name + "'.", injectable);

                    injectableMember.SetValue(injectable, toInject);

                    CallOnCompleteIfExists(injectable, injectableMember, toInject);

                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, injectable); // Bad type??!
                }

                return true;
            }
            else
            {
                // Failed to find a match.
                return false;
            }
        }

        private Component FindDependencyInHere(Component injectable,
            IInjectableMember injectableMember)
        {
            var dependency = FindMatchingDependency(injectableMember,
                injectable.gameObject, injectable);
            if (dependency != null)
            {
                return dependency;
            }
            return null;
        }

        private Component[] FindDependenciesInHere(Component injectable,
            IInjectableMember injectableMember)
        {
            Component[] dependencies = FindMatchingDependendencies(injectableMember,
                injectable.gameObject).ToArray();
            if (dependencies != null)
            {
                return dependencies;
            }
            return null;
        }
        #endregion

        #region Resolve from Hierarchy
        /// <summary>
        /// Attempt to resolve a member dependency by scanning up the hiearchy for a Behaviour that mathces the injection type.
        /// </summary>
        private bool ResolveMemberDependencyFromHierarchy(Component injectable,
            IInjectableMember injectableMember, bool isAbove)
        {
            if(injectableMember.MemberType.IsArray)
            {
                return ResolveArrayDependencyInHierarchy(injectable, injectableMember, isAbove);
            }
            else
            {
                return ResolveObjectDependencyInHierarchy(injectable, injectableMember, isAbove);
            }
        }

        private bool ResolveArrayDependencyInHierarchy(Component injectable,
            IInjectableMember injectableMember, bool isAbove)
        {
            var elementType = injectableMember.MemberType.GetElementType();
            Component[] toInject = FindDependenciesInHierarchy(injectableMember, injectable, isAbove).ToArray();
            if(toInject != null && toInject.Length > 0)
            {
                try
                {
                    Debug.Log("Injecting array of " + toInject.Length + " elements into " + injectable.GetType().Name + " at " + injectableMember.Category + " " + injectableMember.Name + " on GameObject '" + injectable.name + "'.", injectable);

                    foreach (var component in toInject.Cast<Component>())
                    {
                        Debug.Log("> Injecting object " + component.GetType().Name + " (GameObject: '" + component.gameObject.name + "').", injectable);
                    }

                    // 
                    // Create an appropriately typed array so that we don't get a type error when setting the value.
                    //
                    var typedArray = Array.CreateInstance(elementType, toInject.Length);
                    Array.Copy(toInject, typedArray, toInject.Length);

                    injectableMember.SetValue(injectable, typedArray);
                    CallOnCompleteIfExists(injectable, injectableMember, toInject);

                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, injectable); // Bad type??!
                }

                return true;
            }
            else
            {
                //Failed to find matches
                return false;
            }
        }

        private bool ResolveObjectDependencyInHierarchy(Component injectable,
            IInjectableMember injectableMember, bool isAbove)
        {
            // Find a match in the hierarchy.
            var toInject = FindDependencyInHierarchy(injectableMember, injectable, isAbove);
            if (toInject != null)
            {
                try
                {
                    Debug.Log("Injecting " + toInject.GetType().Name +
                        " from hierarchy (GameObject: '" + toInject.gameObject.name +
                        "') into " + injectable.GetType().Name + " at " +
                        injectableMember.Category + " " + injectableMember.Name +
                        " on GameObject '" + injectable.name + "'.", injectable);

                    injectableMember.SetValue(injectable, toInject);

                    CallOnCompleteIfExists(injectable, injectableMember, toInject);

                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, injectable); // Bad type??!
                }

                return true;
            }
            else
            {
                // Failed to find a match.
                return PerformForceInjectObject(injectable, injectableMember);
            }
        }

        /// <summary>
        /// Walk up the hierarchy (towards the root) and find an injectable dependency that matches the specified type.
        /// </summary>
        private Component FindDependencyInHierarchy(IInjectableMember injectableMember,
            Component injectable, bool isAbove)
        {
            List<GameObject> objs = GetObjectsInHierarchy(injectable, isAbove);

            foreach (var obj in objs)
            {
                var dependency = FindMatchingDependency(injectableMember, obj, injectable);
                if (dependency != null)
                {
                    return dependency;
                }
            }

            return null;
        }

        private List<Component> FindDependenciesInHierarchy(IInjectableMember injectableMember,
            Component injectable, bool isAbove)
        {
            List<GameObject> objs = GetObjectsInHierarchy(injectable, isAbove);

            List<Component> dependencies = new List<Component>();

            foreach (var obj in objs)
            {
                var dependency = FindMatchingDependency(injectableMember, obj, injectable);
                if (dependency != null)
                {
                    dependencies.Add(dependency);
                }
            }
            return dependencies;
        }

        private List<GameObject> GetObjectsInHierarchy(Component injectable, bool isAbove)
        {
            List<GameObject> objs;

            if (isAbove)
            {
                objs = GetAncestors(injectable.gameObject);
            }
            else
            {
                objs = GetChildren(injectable.gameObject);
            }

            return objs;
        }

        /// <summary>
        /// Get the ancestors from a particular Game Object. This means the parent object, the grand parent and so on up to the root of the hierarchy.
        /// </summary>
        private List<GameObject> GetAncestors(GameObject fromGameObject)
        {
            List<GameObject> objs = new List<GameObject>();
            for (var parent = fromGameObject.transform.parent; parent != null; parent = parent.parent)
            {
                objs.Add(parent.gameObject); // Mmmmm... LINQ.
            }

            return objs;
        }

        /// <summary>
        /// Get the children from a particular Game Object. This means the child object, the grand child and so on down to the leaf of the hierarchy.
        /// </summary>
        private List<GameObject> GetChildren(GameObject fromGameObject,
            List<GameObject> objs = null)
        {
            if (objs == null)
            {
                objs = new List<GameObject>();
            }

            foreach (Transform child in fromGameObject.transform)
            {
                objs.Add(child.gameObject);
                GetChildren(child.gameObject, objs);
            }

            return objs;
        }

        /// <summary>
        /// Find a single matching dependency at a particular level in the hierarchy.
        /// Returns null if none or multiple were found.
        /// </summary>
        private Component FindMatchingDependency(IInjectableMember injectableMember,
            GameObject gameObject, Component injectable)
        {
            var matchingDependencies = FindMatchingDependendencies(injectableMember,
                gameObject).ToArray();
            if (matchingDependencies.Length == 1)
            {
                // A single matching dep was found.
                return matchingDependencies[0];
            }

            if (matchingDependencies.Length == 0)
            {
                // No deps were found.
                return null;
            }

            Debug.LogError(
                "Found multiple hierarchy dependencies that match injection type " +
                injectableMember.MemberType.Name + " to be injected into '" +
                injectable.name + "'. See following warnings.", injectable
            );

            foreach (var dependency in matchingDependencies)
            {
                Debug.LogWarning("  Duplicate dependencies: '" + dependency.name + "'.", dependency);
            }

            return null;
        }

        /// <summary>
        /// Find matching dependencies at a particular level in the hiearchy.
        /// </summary>
        private IEnumerable<Component> FindMatchingDependendencies(IInjectableMember injectableMember,
            GameObject gameObject)
        {
            Type comparisonType;
            if(injectableMember.MemberType.IsArray)
            {
                comparisonType = injectableMember.MemberType.GetElementType();
            }
            else
            {
                comparisonType = injectableMember.MemberType;
            }

            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (comparisonType.IsAssignableFrom(component.GetType()))
                {
                    switch (injectableMember.useType)
                    {
                        case UseType.None:
                            yield return component;
                            break;
                        case UseType.Name:
                            if (component.name == injectableMember.key)
                            {
                                yield return component;
                            }
                            break;
                        case UseType.Reflection:
                            if(component.name == injectableMember.Name)
                            {
                                yield return component;
                            }
                            break;
                            //TODO
                            //break;
                    }
                }
            }
        }
        #endregion

        #region Resolve from Anywhere
        /// <summary>
        /// Attempt to resolve a member dependency from anywhere in the scene.
        /// Returns false is no such dependency was found.
        /// </summary>
        private bool ResolveMemberDependencyFromAnywhere(Component injectable, IInjectableMember injectableMember)
        {
            if (injectableMember.MemberType.IsArray)
            {
                return ResolveArrayDependencyFromAnywhere(injectable, injectableMember);
            }
            else
            {
                return ResolveObjectDependencyFromAnywhere(injectable, injectableMember);
            }
        }

        #region Resolve Array Dependency from Anywhere
        /// <summary>
        /// Resolve an array dependency from objects anywhere in the scene.
        /// </summary>
        private bool ResolveArrayDependencyFromAnywhere(Component injectable, IInjectableMember injectableMember)
        {
            var elementType = injectableMember.MemberType.GetElementType();
            Component[] toInject = null;

            if (injectableMember.InjectType == InjectType.Class)
            {
                toInject = GetArrayFromClass(elementType);
            }
            else if (injectableMember.InjectType == InjectType.Interface)
            {
                toInject = GetArrayFromInterface(elementType);
            }

            if (toInject != null)
            {
                try
                {
                    Debug.Log("Injecting array of " + toInject.Length + " elements into " + injectable.GetType().Name + " at " + injectableMember.Category + " " + injectableMember.Name + " on GameObject '" + injectable.name + "'.", injectable);

                    foreach (var component in toInject.Cast<Behaviour>())
                    {
                        Debug.Log("> Injecting object " + component.GetType().Name + " (GameObject: '" + component.gameObject.name + "').", injectable);
                    }

                    // 
                    // Create an appropriately typed array so that we don't get a type error when setting the value.
                    //
                    var typedArray = Array.CreateInstance(elementType, toInject.Length);
                    Array.Copy(toInject, typedArray, toInject.Length);

                    injectableMember.SetValue(injectable, typedArray);
                    CallOnCompleteIfExists(injectable, injectableMember, toInject);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, injectable);
                }

                return true;
            }
            else
            {
                return PerformForceInjectArray(injectable, injectableMember);
            }
        }

        private static Component[] GetArrayFromClass(Type elementType)
        {
            return (Component[])GameObject.FindObjectsOfType(elementType);
        }

        private static Component[] GetArrayFromInterface(Type elementType)
        {
            var toInjectArr = GameObject.FindObjectsOfType<Component>();
            List<Component> toInject = new List<Component>();
            for (int i = 0; i < toInjectArr.Length; i++)
            {
                Component tmp = toInjectArr[i];

                if (elementType.IsAssignableFrom(tmp.GetType()))
                {
                    toInject.Add(tmp);
                }
            }

            return toInject.ToArray();
        }
        #endregion

        #region Resolve Object Dependency from Anywhere

        private Component GetObjectFromAnywhere(IInjectableMember injectableMember)
        {
            if(injectableMember.useType == UseType.None)
            {
                return (Component)GameObject.FindObjectOfType(injectableMember.MemberType);
            }

            //UseType is Reflection
            return GetObjectFromAnywhereWithReflection(injectableMember);
        }

        private Component GetInterfaceFromAnywhere(IInjectableMember injectableMember)
        {
            if(injectableMember.useType == UseType.None)
            {
                Type t = injectableMember.MemberType;
                Component toInject = null;
                var toInjectArr = GameObject.FindObjectsOfType<Component>();
                for (int i = 0; i < toInjectArr.Length; i++)
                {
                    Component tmp = toInjectArr[i];

                    if (t.IsAssignableFrom(tmp.GetType()))
                    {
                        toInject = tmp;
                        break;
                    }
                }
                return toInject;
            }

            //UseType is Reflection
            return GetInterfaceFromAnywhereWithReflection(injectableMember);
        }

        /// <summary>
        /// Resolve an object dependency from objects anywhere in the scene.
        /// </summary>
        private bool ResolveObjectDependencyFromAnywhere(Component injectable, IInjectableMember injectableMember)
        {

            Component toInject = null;

            if (injectableMember.InjectType == InjectType.Class)
            {
                //toInject = (Behaviour)GameObject.FindObjectOfType(injectableMember.MemberType);
                toInject = GetObjectFromAnywhere(injectableMember);
            }
            else if (injectableMember.InjectType == InjectType.Interface)
            {
                toInject = GetInterfaceFromAnywhere(injectableMember);
            }

            if (toInject != null)
            {
                try
                {
                    Debug.Log("Injecting object " + toInject.GetType().Name +
                        " (GameObject: '" + toInject.gameObject.name +
                        "') into " + injectable.GetType().Name + " at " +
                        injectableMember.Category + " " + injectableMember.Name +
                        " on GameObject '" + injectable.name + "'.", injectable);

                    injectableMember.SetValue(injectable, toInject);

                    CallOnCompleteIfExists(injectable, injectableMember, toInject);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex, injectable);
                }

                return true;
            }
            else
            {
                return PerformForceInjectObject(injectable, injectableMember);
            }
        }
        #endregion

        #endregion

        #region UseReflectionToInject
        private Component GetObjectFromAnywhereWithReflection(IInjectableMember injectableMember)
        {
            UnityEngine.Object[] array = GameObject.FindObjectsOfType(injectableMember.MemberType,
                                        includeInactive: false);

            if (array.Length == 0)
                return null;

            Component res = null;
            foreach (UnityEngine.Object obj in array)
            {
                Component tmp = obj as Component;
                if (tmp.name == injectableMember.Name)
                {
                    res = tmp;
                    break;
                }
            }
            return res;
        }

        private Component GetInterfaceFromAnywhereWithReflection(IInjectableMember injectableMember)
        {
            Type t = injectableMember.MemberType;
            var toInjectArr = GameObject.FindObjectsOfType<Component>(includeInactive: false);
            Component toInject = null;
            for (int i = 0; i < toInjectArr.Length; i++)
            {
                Component tmp = toInjectArr[i];

                if (t.IsAssignableFrom(tmp.GetType()))
                {
                    if(tmp.name == injectableMember.Name)
                    {
                        toInject = tmp;
                        break;
                    }
                }
            }

            return toInject;
        }
        #endregion

        #region OnInjectComplete
        /// <summary>
        /// Check if a method needs to be called after successful injection and call it.
        /// </summary>
        /// <param name="injectable"></param>
        /// <param name="injectableMember"></param>
        /// <param name="toInject"></param>
        private void CallOnCompleteIfExists(Component injectable,
            IInjectableMember injectableMember, Component toInject)
        {
            if (injectableMember.OnInjectComplete != "")
            {
                MethodInfo methodInfo = injectable.GetType().GetMethod(injectableMember.OnInjectComplete);

                ParameterInfo[] paramInfo = methodInfo.GetParameters();

                object[] methodParams = new object[] { };

                if (paramInfo.Length > 0)
                {
                    methodParams = new object[] { new object[] { toInject } };
                }

                methodInfo.Invoke(injectable, methodParams);
                //injectable.Invoke(injectableMember.OnInjectComplete, 0f);
            }

        }

        private void CallOnCompleteIfExists(Component injectable,
            IInjectableMember injectableMember, Component[] toInject)
        {
            if (injectableMember.OnInjectComplete != "")
            {
                MethodInfo methodInfo = injectable.GetType().GetMethod(injectableMember.OnInjectComplete);

                ParameterInfo[] paramInfo = methodInfo.GetParameters();

                object[] methodParams = new object[] { };

                if (paramInfo.Length > 0)
                {
                    methodParams = new object[] { toInject };
                }

                methodInfo.Invoke(injectable, methodParams);
                //injectable.Invoke(injectableMember.OnInjectComplete, 0f);
            }
        }
        #endregion

        #region ForceInject
        private bool IsMemberForceInjectable(MemberInfo member)
        {
            return member.GetCustomAttributes(true)
                .Where(attribute => attribute is ForceInjectAttribute)
                .Count() > 0;
        }

        private bool IsForceInjectable(Type type)
        {
            var injectableProperties = type.GetProperties()
                .Where(IsMemberForceInjectable);

            var injectableFields = type.GetFields()
                .Where(IsMemberForceInjectable);

            if (injectableProperties.Count() > 0 || injectableFields.Count() > 0)
                return true;

            return false;
        }

        private bool PerformForceInjectObject(Component injectable,
            IInjectableMember injectableMember)
        {
            if (IsForceInjectable(injectable.GetType()))
            {
                if (injectableMember.InjectType == InjectType.Interface)
                {
                    Debug.LogError("Cannot Force Inject interfaces");
                    return false;
                }

                //GameObject newObj = new GameObject();
                Type t = injectableMember.MemberType;
                GameObject newObj = new GameObject();
                newObj.AddComponent(t);
                newObj.name = injectableMember.Name;
                Component toInject = newObj.GetComponent(t);

                if(injectableMember.InjectFrom == InjectFrom.Below)
                {
                    newObj.transform.parent = injectable.gameObject.transform;
                }
                else if(injectableMember.InjectFrom == InjectFrom.Above)
                {
                    injectable.gameObject.transform.parent = newObj.transform;
                }

                Debug.Log("Instantiating and Injecting " + toInject.GetType().Name +
                        " (GameObject: '" + toInject.gameObject.name +
                        "') into " + injectable.GetType().Name + " at " +
                        injectableMember.Category + " " + injectableMember.Name +
                        " on GameObject '" + injectable.name + "'.", injectable);

                injectableMember.SetValue(injectable, toInject);
                CallOnCompleteIfExists(injectable, injectableMember, toInject);
                //newObj.AddComponent<Behaviour>();
                return true;
            }

            return false;
        }

        private bool PerformForceInjectArray(Component injectable,
            IInjectableMember injectableMember)
        {
            if (injectableMember.InjectType == InjectType.Interface)
            {
                Debug.LogError("Cannot Force Inject interfaces");
                return false;
            }

            if (IsForceInjectable(injectable.GetType()))
            {
                //GameObject newObj = new GameObject();
                Type t = injectableMember.MemberType;

                GameObject newObj = new GameObject();
                newObj.AddComponent(t);
                newObj.name = t.Name;
                Component[] toInject = new Component[]
                {
                    newObj.GetComponent(t)
                };
                injectableMember.SetValue(injectable, toInject);
                CallOnCompleteIfExists(injectable, injectableMember, toInject);
                //newObj.AddComponent<Behaviour>();
                return true;
            }
            return false;
        }
        #endregion
    }
}