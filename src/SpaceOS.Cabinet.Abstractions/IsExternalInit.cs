// Polyfill required for C# 9+ record types on netstandard2.1 / netstandard2.0.
// The compiler emits init-only setters that depend on this type; it is present
// in .NET 5+ BCL but absent in netstandard2.x runtimes.
#if NETSTANDARD2_1 || NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

/// <summary>Marks a method as an init-only setter (compiler polyfill for netstandard2.x).</summary>
internal static class IsExternalInit { }
#endif
