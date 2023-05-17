using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Celery.Utils
{
    public static class JsonParser
    {
        public static T FromJson<T>(this string json)
        {
            return (T)ParseValue(typeof(T), json.UnformatJson());
        }

        public static string UnformatJson(this string json)
        {
            string unformatted = "";
            for (int i = 0; i < json.Length; i++)
            {
                if (!char.IsWhiteSpace(json[i])) unformatted += json[i];
                if (json[i] == '"')
                {
                    for (int i2 = i + 1; i2 < json.Length; i2++)
                    {
                        unformatted += json[i2];
                        if (json[i2] == '\\')
                        {
                            unformatted += json[i2 + 1];
                            i2++;
                        }
                        else if (json[i2] == '"')
                        {
                            i = i2;
                            break;
                        }
                    }
                }
            }
            return unformatted;
        }

        public static string FormatJson(this string json)
        {
            json = json.UnformatJson();
            string formatted = "";
            int depth = 0;
            for (int i = 0; i < json.Length; i++)
            {
                if (json[i] == '[' || json[i] == '{')
                {
                    depth++;
                    formatted += json[i] + "\n" + new string(' ', 4 * depth);
                }
                else if (json[i] == ']' || json[i] == '}')
                {
                    depth--;
                    formatted += "\n" + new string(' ', 4 * depth) + json[i];
                }
                else if (json[i] == ',') formatted += ",\n" + new string(' ', 4 * depth);
                else if (json[i] == ':') formatted += ": ";
                else formatted += json[i];
            }
            return formatted;
        }

        private static List<string> BreakDown(string json)
        {
            List<string> result = new List<string>();
            if (json.Length == 2) return result;
            int depth = 0;
            string value = "";
            for (int i = 1; i < json.Length - 1; i++)
            {
                if (json[i] == '{' || json[i] == '[') depth++;
                else if (json[i] == '}' || json[i] == ']') depth--;
                else if ((json[i] == ':' || json[i] == ',') && depth == 0)
                {
                    result.Add(value);
                    value = "";
                    continue;
                }
                else if (json[i] == '\"')
                {
                    value += json[i];
                    for (int i2 = i + 1; i2 < json.Length; i2++)
                    {
                        value += json[i2];
                        if (json[i2] == '\\')
                        {
                            value += json[i2 + 1];
                            i2++;
                        }
                        else if (json[i2] == '"')
                        {
                            i = i2;
                            break;
                        }
                    }
                    continue;
                }
                value += json[i];
            }
            result.Add(value);
            return result;
        }

        private static object ParseValue(Type type, string value)
        {
            if (value == "null") return null;
            else if (type == typeof(string))
            {
                if (value[0] == '"' && value[value.Length - 1] == '"') return ParseString(value);
                else return null;
            }
            else if (type.IsPrimitive)
            {
                try
                {
                    return Convert.ChangeType(value, type, CultureInfo.InvariantCulture); // Boolean, Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64, IntPtr, UIntPtr, Char, Double, and Single.
                }
                catch (InvalidCastException)
                {
                    return null;
                }
            }
            else if (type == typeof(decimal))
            {
                if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal result)) return result;
                else return null;
            }
            else if (type == typeof(DateTime))
            {
                if (DateTime.TryParse(value.Replace("\"", ""), CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result)) return result;
                else return null;
            }
            else if (type.IsEnum)
            {
                if (value[0] == '"' && value[value.Length - 1] == '"') value = value.Substring(1, value.Length - 2);
                try
                {
                    return Enum.Parse(type, value, false);
                }
                catch
                {
                    return 0;
                }
            }
            else if (type.IsArray)
            {
                if (value[0] == '"' && value[value.Length - 1] == '"') return null;
                Type arrayType = type.GetElementType();
                List<string> values = BreakDown(value);
                Array array = Array.CreateInstance(arrayType, values.Count);
                for (int i = 0; i < values.Count; i++) array.SetValue(ParseValue(arrayType, values[i]), i);
                return array;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (value[0] != '[' || value[value.Length - 1] != ']') return null;
                Type valueType = type.GetGenericArguments()[0];
                Type listType = typeof(List<>).MakeGenericType(valueType);
                IList list = (IList)Activator.CreateInstance(listType);
                foreach (string val in BreakDown(value))
                {
                    Console.WriteLine(ParseValue(valueType, val) + " " + valueType);
                    listType.GetMethod("Add").Invoke(list, new object[] { ParseValue(valueType, val) });
                }
                return list;
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                if (value[0] != '{' || value[value.Length - 1] != '}' || type.GetGenericArguments()[0] != typeof(string)) return null;
                List<string> values = BreakDown(value);
                if (values.Count % 2 == 1) return null;
                Type valueType = type.GetGenericArguments()[1];
                Type dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
                IDictionary dict = (IDictionary)Activator.CreateInstance(dictType);
                for (int i = 0; i < values.Count; i += 2) dictType.GetMethod("Add").Invoke(dict, new object[] { (string)ParseValue(typeof(string), values[i]), ParseValue(valueType, values[i + 1]) });
                return dict;
            }
            else if (type == typeof(object)) return ParseUnkownValue(value);
            else if (value[0] == '{' && value[value.Length - 1] == '}') return ParseObject(type, value);
            return null;
        }

        private static object ParseUnkownValue(string value)
        {
            if (value.Length == 0 || value == "null") return null;
            else if (value == "true") return true;
            else if (value == "false") return false;
            else if (value[0] == '\"' && value[value.Length - 1] == '\"') return ParseString(value);
            else if ("-0123456789".Contains(value[0].ToString()))
            {
                if (value.Contains("."))
                {
                    if (double.TryParse(value, out double num)) return num;
                    else return null;
                }
                else
                {
                    if (int.TryParse(value, out int num)) return num;
                    else return null;
                }
            }
            else if (value[0] == '[' && value[value.Length - 1] == ']')
            {
                List<object> output = new List<object>();
                foreach (string val in BreakDown(value)) output.Add(ParseUnkownValue(val));
                Type listType = typeof(List<>).MakeGenericType(!output.Contains(null) && output.Any(o => o != null && o.GetType() != output[0].GetType()) ? typeof(object) : output[0].GetType());
                object listInstance = Activator.CreateInstance(listType);
                foreach (object val in output) listType.GetMethod("Add").Invoke(listInstance, new object[] { val });
                return listInstance;
            }
            else if (value[0] == '{' && value[value.Length - 1] == '}')
            {
                List<string> dict = BreakDown(value);
                Dictionary<string, object> output = new Dictionary<string, object>();
                for (int i = 0; i < dict.Count; i += 2) output.Add((string)ParseUnkownValue(dict[i]), ParseUnkownValue(dict[i + 1]));
                Type dictType = typeof(Dictionary<,>).MakeGenericType(typeof(string), output.Any(o => o.Value != null && o.Value.GetType() != output.First().Value.GetType()) ? typeof(object) : output.First().Value.GetType());
                object dictInstance = Activator.CreateInstance(dictType);
                foreach (KeyValuePair<string, object> val in output) dictType.GetMethod("Add").Invoke(dictInstance, new object[] { val.Key, val.Value });
                return dictInstance;
            }
            return null;
        }

        private static string ParseString(string str)
        {
            string s = "";
            bool escape = false;
            for (int i = 1; i < str.Length; i++)
            {
                if (escape)
                {
                    escape = false;
                    if (str[i] == 'u')
                    {
                        s += (char)Convert.ToInt16(str.Substring(i + 1, 4), 16);
                        i += 4;
                    }
                    else if ("\"\\/bfnrt".Contains(str[i].ToString())) s += "\"\\/\b\f\n\r\t"["\"\\/bfnrt".IndexOf(str[i])];
                    else s += str[i];
                }
                else
                {
                    if (str[i] == '\"') break;
                    if (str[i] == '\\') escape = true;
                    else s += str[i];
                }
            }
            return s;
        }

        private static object ParseObject(Type type, string json)
        {
            object instance = Activator.CreateInstance(type);
            foreach (KeyValuePair<string, object> pair in (Dictionary<string, object>)ParseUnkownValue(json))
            {
                try
                {
                    type.GetProperty(pair.Key).SetValue(instance, pair.Value);
                }
                catch (ArgumentException ex)
                {
                    throw ex;
                }
            }
            return instance;
        }
    }
}
