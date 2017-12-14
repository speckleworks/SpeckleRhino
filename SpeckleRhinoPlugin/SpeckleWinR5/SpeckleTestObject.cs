using Newtonsoft.Json;

namespace SpeckleRhino
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