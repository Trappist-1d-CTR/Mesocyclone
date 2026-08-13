using System;
using System.Runtime.CompilerServices;

public static class ExtEnumInitializer
{
    public static void InitTypes<T>() where T : ExtEnum<T>
    {
        RuntimeHelpers.RunClassConstructor(typeof(T).TypeHandle);
    }
}