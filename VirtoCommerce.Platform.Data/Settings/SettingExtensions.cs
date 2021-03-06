using System;
using System.Linq;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Settings;

namespace VirtoCommerce.Platform.Data.Settings
{
    public static class SettingExtensions
    {
        public static SettingEntry ToSettingEntry(this ModuleSetting moduleSetting)
        {
            var result = AbstractTypeFactory<SettingEntry>.TryCreateInstance();
            result.Name = moduleSetting.Name;
            result.DefaultValue = moduleSetting.DefaultValue;
            result.RawDefaultValue = moduleSetting.RawDefaultValue();
            result.Value = result.DefaultValue;
            result.RawValue = result.RawDefaultValue;
            result.AllowedValues = moduleSetting.AllowedValues;
            result.IsArray = moduleSetting.IsArray;
            result.RestartRequired = moduleSetting.RestartRequired;
            result.ArrayValues = moduleSetting.ArrayValues;
            if (moduleSetting.ArrayValues != null)
            {
                result.RawArrayValues = moduleSetting.ArrayValues.Select(x => moduleSetting.RawValue(x)).ToArray();
            }

            result.ValueType = ToSettingValueType(moduleSetting.ValueType);
            return result;
        }

        public static SettingValueType ToSettingValueType(this Type valueType)
        {
            var retVal = SettingValueType.ShortText;
            if (valueType == typeof(bool))
            {
                retVal = SettingValueType.Boolean;
            }
            else if (valueType == typeof(int))
            {
                retVal = SettingValueType.Integer;
            }
            else if (valueType == typeof(decimal))
            {
                retVal = SettingValueType.Decimal;
            }
            else if (valueType == typeof(DateTime))
            {
                retVal = SettingValueType.DateTime;
            }
            return retVal;
        }

        public static SettingValueType ToSettingValueType(this string moduleSettingValueType)
        {
            var retVal = SettingValueType.ShortText;
            if (moduleSettingValueType.EqualsInvariant(ModuleSetting.TypeBoolean))
            {
                retVal = SettingValueType.Boolean;
            }
            else if (moduleSettingValueType.EqualsInvariant(ModuleSetting.TypeInteger))
            {
                retVal = SettingValueType.Integer;
            }
            else if (moduleSettingValueType.EqualsInvariant(ModuleSetting.TypeDecimal))
            {
                retVal = SettingValueType.Decimal;
            }
            else if (moduleSettingValueType.EqualsInvariant(ModuleSetting.TypeSecureString))
            {
                retVal = SettingValueType.SecureString;
            }
            else if (moduleSettingValueType.EqualsInvariant(ModuleSetting.TypeText))
            {
                retVal = SettingValueType.LongText;
            }
            else if (moduleSettingValueType.EqualsInvariant(ModuleSetting.TypeDateTime))
            {
                retVal = SettingValueType.DateTime;
            }
            else if (moduleSettingValueType.EqualsInvariant(ModuleSetting.TypeJson))
            {
                retVal = SettingValueType.Json;
            }
            return retVal;
        }
    }
}
