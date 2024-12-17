using System;

namespace Argento.ReportingService.Utility.Utils
{
    public static class TypeUtil
    {
        public static object GetDefault(Type type)
        {
            object value = null;
            if (type.IsValueType) value = Activator.CreateInstance(type);
            return value;
        }
    }
}