using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class DIContainer
{
    private static DIContainer global;
    public static DIContainer Global => global ??= new DIContainer();

    private readonly Dictionary<Type, object> registry = new Dictionary<Type, object>();

    public void Register<T>(T instance)
    {
        if (instance == null)
        {
            return;
        }

        registry[typeof(T)] = instance;
    }

    public bool TryResolve<T>(out T value)
    {
        var type = typeof(T);
        if (registry.TryGetValue(type, out var instance) && instance is T typedInstance)
        {
            value = typedInstance;
            return true;
        }

        foreach (var registeredInstance in registry.Values)
        {
            if (registeredInstance is T assignableInstance)
            {
                value = assignableInstance;
                return true;
            }
        }

        if (typeof(UnityEngine.Object).IsAssignableFrom(type))
        {
            var found = UnityEngine.Object.FindFirstObjectByType(type);
            if (found is T foundTyped)
            {
                Register(foundTyped);
                value = foundTyped;
                return true;
            }
        }

        value = default;
        return false;
    }

    public T Resolve<T>()
    {
        if (TryResolve(out T value))
        {
            return value;
        }

        Debug.LogWarning($"[DIContainer] Could not find or resolve type: {typeof(T).Name}");
        return default;
    }

    public static void ResetGlobal()
    {
        global = new DIContainer();
    }
}
