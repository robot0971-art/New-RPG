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
        if (instance == null) return;
        var type = typeof(T);
        registry[type] = instance;
    }

    public T Resolve<T>() where T : UnityEngine.Object
    {
        var type = typeof(T);
        if (registry.TryGetValue(type, out var instance) && instance != null)
        {
            return (T)instance;
        }

        // 만약 등록되어 있지 않다면 씬에서 직접 찾아서 등록 시도 (순서 문제 해결)
        var found = UnityEngine.Object.FindFirstObjectByType<T>();
        if (found != null)
        {
            Register(found);
            return found;
        }

        Debug.LogWarning($"[DIContainer] Could not find or resolve type: {type.Name}");
        return default;
    }

    public static void ResetGlobal()
    {
        global = new DIContainer();
    }
}
