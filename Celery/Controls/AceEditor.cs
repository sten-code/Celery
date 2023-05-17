using Celery.Utils;
using System.Threading.Tasks;
using System.Web;

namespace Celery.Controls
{
    public class AceEditor : Editor
    {
        public override async void SetText(string text)
        {
            while (!IsLoaded) await Task.Delay(10);
            await CoreWebView2.ExecuteScriptAsync("editor.setValue(\"" + HttpUtility.JavaScriptStringEncode(text) + "\")");
        }

        public override async Task<string> GetText()
        {
            while (!IsLoaded) await Task.Delay(10);
            return (await CoreWebView2.ExecuteScriptAsync("editor.getValue()")).FromJson<string>(); // The string gets returned as "while true do\r\n\r\nend" instead of an already parsed string, json has the exact same rules so a json parser can convert it to a normal string.
        }

        public AceEditor(string text) : base("Ace")
        {
            SetText(text);
        }
    }
}
