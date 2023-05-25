using Celery.Utils;
using System.Threading.Tasks;
using System.Web;

namespace Celery.Controls
{
    public class AceEditor : Editor
    {
        public AceEditor(string text) : base("Ace", text)
        { }

        public override async void SetText(string text)
        {
            await CoreWebView2.ExecuteScriptAsync("editor.setValue(\"" + HttpUtility.JavaScriptStringEncode(text) + "\")");
        }

        public override async Task<string> GetText()
        {
            if (IsDoMLoaded)
            {
                await ExecuteScriptAsync("window.chrome.webview.postMessage(editor.getValue())");
                while (!NewMessage) await Task.Delay(10);
                NewMessage = false;
                return LatestRecievedText;
            }
            return string.Empty;
        }
    }
}
