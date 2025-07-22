using System.Reflection;

namespace Forestry.Flo.Tests.Common;

/// <summary>
/// Provides utility methods for test scenarios to access or modify protected members. Use with caution.
/// </summary>
/// <remarks>
/// Modifying protected/private members can breach OOP principles and result in unrealistic test scenarios. 
/// </remarks>
public static class TestUtils
{
    /// <summary>
    /// Sets the value of a specified property on an object. Useful for protected or private properties in tests.
    /// </summary>
    /// <typeparam name="T">The type of the object on which the property value will be set.</typeparam>
    /// <param name="obj">The object on which the property value will be set.</param>
    /// <param name="propertyName">The name of the property to be set.</param>
    /// <param name="value">The value to set on the specified property.</param>
    /// <exception cref="ArgumentException">Thrown when the specified property does not exist on the provided object.</exception>
    /// <remarks>
    /// It's designed for exceptional cases where alternatives are limited, not as a routine tool for writing tests.
    /// </remarks>
    public static void SetProtectedProperty<T>(T obj, string propertyName, object value)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (prop == null) throw new ArgumentException($"Property {propertyName} was not found in type {typeof(T)}", nameof(propertyName));
        prop.SetValue(obj, value, null);
    }
}