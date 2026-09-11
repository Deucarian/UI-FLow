using System;

namespace Deucarian.UIFlow
{
    /// <summary>Marks an authoritative set of named ScreenKey fields or properties for the Inspector.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ScreenKeySetAttribute : Attribute { }
}
