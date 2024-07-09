using System.Globalization;
using System.Text;

namespace Celery.Utils
{
    public class HttpUtils
    {
        private static bool JavaScriptEncodeAmpersand = false;
        
        private static bool CharRequiresJavaScriptEncoding(char c)
        {
            if (c >= ' ' && c != '"' && c != '\\' && c != '\'' && c != '<' && c != '>' && (c != '&' || !JavaScriptEncodeAmpersand) && c != '\u0085' && c != '\u2028')
            {
                return c == '\u2029';
            }

            return true;
        }
        
        public static string JavaScriptStringEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            
            StringBuilder stringBuilder = null;
            int startIndex = 0;
            int num = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (CharRequiresJavaScriptEncoding(c))
                {
                    if (stringBuilder == null)
                        stringBuilder = new StringBuilder(value.Length + 5);
                    
                    if (num > 0)
                    {
                        stringBuilder.Append(value, startIndex, num);
                    }

                    startIndex = i + 1;
                    num = 0;
                }

                switch (c)
                {
                    case '\r':
                        stringBuilder.Append("\\r");
                        continue;
                    case '\t':
                        stringBuilder.Append("\\t");
                        continue;
                    case '"':
                        stringBuilder.Append("\\\"");
                        continue;
                    case '\\':
                        stringBuilder.Append("\\\\");
                        continue;
                    case '\n':
                        stringBuilder.Append("\\n");
                        continue;
                    case '\b':
                        stringBuilder.Append("\\b");
                        continue;
                    case '\f':
                        stringBuilder.Append("\\f");
                        continue;
                }

                if (CharRequiresJavaScriptEncoding(c))
                {
                    stringBuilder.Append("\\u");
                    stringBuilder.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                }
                else
                {
                    num++;
                }
            }

            if (stringBuilder == null)
            {
                return value;
            }

            if (num > 0)
            {
                stringBuilder.Append(value, startIndex, num);
            }

            return stringBuilder.ToString();
        }

    }
}