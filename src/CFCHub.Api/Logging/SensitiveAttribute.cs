using System;

namespace CFCHub.Api.Logging;

[AttributeUsage(AttributeTargets.Property)]
public class SensitiveAttribute : Attribute
{
}
