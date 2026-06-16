using System.Collections.Generic;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;
using CFCHub.Domain.Shared;

namespace CFCHub.Api.Logging;

public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        var type = value.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        bool hasSensitive = false;
        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<SensitiveAttribute>() != null)
            {
                hasSensitive = true;
                break;
            }
        }

        if (!hasSensitive)
        {
            result = null!;
            return false;
        }

        var logProperties = new List<LogEventProperty>();

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<SensitiveAttribute>() != null)
            {
                logProperties.Add(new LogEventProperty(property.Name, new ScalarValue("[REDACTED]")));
            }
            else
            {
                var propertyValue = property.GetValue(value);
                logProperties.Add(new LogEventProperty(property.Name, propertyValueFactory.CreatePropertyValue(propertyValue, destructureObjects: true)));
            }
        }

        result = new StructureValue(logProperties, type.Name);
        return true;
    }
}
