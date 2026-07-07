using System.ComponentModel;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill to enable C# 9 records and init-only properties in .netstandard2.0
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit { }