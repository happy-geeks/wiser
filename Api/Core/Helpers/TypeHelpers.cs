using System;

namespace Api.Core.Helpers
{
    public class TypeHelpers
    {
        public static bool IsNumericType(Type type)
        {
            return CheckIsNumericType(Nullable.GetUnderlyingType(type)) || CheckIsNumericType(type);
        }

        private static bool CheckIsNumericType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
