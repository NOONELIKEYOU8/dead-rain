using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GenericNotImplementedError
{
    public static T TryGet<T>(ref T value, string name) where T : CoreComponent
    {
        if (value) return value;
        Debug.LogError(typeof(T) + " not implemented on " + name);
        return null;
    }

    public static T TryGet<T>(ref T value, string checkName, string name)where T : Component
    {
        if (value) return value;
        Debug.LogError(checkName + " not implemented on " + name);
        return null;
    }
}
