using System;

/// <summary>
/// Simple Attribute that marks this component to be stripped, when a client version gets generated.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StripAttribute : Attribute
{
}