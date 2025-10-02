using System;
using System.Linq;
using System.Reflection;

namespace OfdrwNet.Converter.Tests.Contracts;

internal static class ContractReflection
{
    public static Type? FindContractType(string fullTypeName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Select(a => SafeGetType(a, fullTypeName))
            .FirstOrDefault(t => t is not null);
    }

    private static Type? SafeGetType(Assembly assembly, string name)
    {
        try
        {
            return assembly.GetType(name, throwOnError: false, ignoreCase: false);
        }
        catch
        {
            return null;
        }
    }
}
