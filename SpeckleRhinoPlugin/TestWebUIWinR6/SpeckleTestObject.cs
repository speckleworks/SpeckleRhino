using Newtonsoft.Json;

namespace TestWebUI
{
    public class TestObject
    {
        [JsonProperty("returnValue")]
        public string ReturnValue { get; set; }

        [JsonProperty("numbers")]
        public float[] Numbers { get; set; }

        public TestObject() { }
    }
}