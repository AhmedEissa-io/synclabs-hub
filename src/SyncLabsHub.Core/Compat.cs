// Enables C# `init` accessors when targeting netstandard2.0 (used by Revit/Rhino net48 plugins).
// No effect on net5.0+ which already defines this type.
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}
#endif
