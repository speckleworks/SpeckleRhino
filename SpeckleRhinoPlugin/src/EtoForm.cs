using Eto.Drawing;
using Eto.Forms;
using System;
using Newtonsoft.Json;

namespace SpeckleRhino
{
    public class EtoForm : Form
    {
        public WebView Wv { get; private set; }
        public bool IndexLoaded = false;

        string index;
        public EtoForm()
        {
            this.ClientSize = new Size(600, 600);
            Wv = new WebView();

            Wv.DocumentLoading += E_DocumentLoading;
            Wv.DocumentLoaded += E_DocumentLoaded;

            var layout = new DynamicLayout();
            layout.Padding = new Padding(0);
            layout.BeginHorizontal();
            layout.Add(Wv, true, true);
            layout.EndHorizontal();
            Content = layout;

        }

        private void E_DocumentLoaded(object sender, WebViewLoadedEventArgs e)
        {
            if (e.Uri.AbsolutePath == index) IndexLoaded = true;
        }

        public void SetWVUrl(string url)
        {
            index = url.Replace("\\", "/");
            Wv.Url = new Uri(index);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void E_DocumentLoading(object sender, WebViewLoadingEventArgs e)
        {
            Rhino.RhinoApp.WriteLine(e.Uri.ToString());
            Rhino.RhinoApp.WriteLine(index);

            if (e.Uri.AbsolutePath != index && IndexLoaded)
            {
                e.Cancel = true;

                var result = "";
                var deserializedObject = new TestObject();

                if (e.Uri.ToString().Contains("sayhi"))
                {
                    result = Wv.ExecuteScript("SayHi(\"Luis\"); return payload;");
                    deserializedObject = JsonConvert.DeserializeObject<TestObject>(result);
                }

                if (e.Uri.ToString().Contains("returndata"))
                {
                    result = Wv.ExecuteScript("ReturnData(1000); return payload;");
                    deserializedObject = JsonConvert.DeserializeObject<TestObject>(result);
                }

                Rhino.RhinoApp.WriteLine(deserializedObject.ReturnValue);

                foreach(var num in deserializedObject.Numbers)
                    Rhino.RhinoApp.Write("{0}{1}", num, ",");

                Rhino.RhinoApp.WriteLine();

            }

        }
    }
}
