using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Egnx.Tools
{
    public static class TypeHelper
    {
        public static bool IsNullable(this Type type)
        {
            if (!type.IsGenericType)
                return false;

            return type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}
