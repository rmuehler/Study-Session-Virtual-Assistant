// <auto-generated>
// Code generated by LUISGen C:\Users\rober\Source\Repos\rmuehler\Virtual-Assistant-for-Finding-Study-Sessions\VirtualAssistant\VirtualAssistant\Deployment\Resources\LU\en\searchskill.luis -cs Luis.searchskillLuis -o C:\Users\rober\Source\Repos\rmuehler\Virtual-Assistant-for-Finding-Study-Sessions\VirtualAssistant\VirtualAssistant\Services
// Tool github: https://github.com/microsoft/botbuilder-tools
// Changes may cause incorrect behavior and will be lost if the code is
// regenerated.
// </auto-generated>
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
namespace Luis
{
    public partial class searchskillLuis: IRecognizerConvert
    {
        [JsonProperty("text")]
        public string Text;

        [JsonProperty("alteredText")]
        public string AlteredText;

        public enum Intent {
            None, 
            Search_by_Subject, 
            Search_by_Tutor
        };
        [JsonProperty("intents")]
        public Dictionary<Intent, IntentScore> Intents;

        public class _Entities
        {
            // Simple entities
            public string[] subject;

            // Built-in entities
            public DateTimeSpec[] datetime;

            public string[] email;

            public string[] personName;

            // Instance
            public class _Instance
            {
                public InstanceData[] datetime;
                public InstanceData[] email;
                public InstanceData[] personName;
                public InstanceData[] subject;
            }
            [JsonProperty("$instance")]
            public _Instance _instance;
        }
        [JsonProperty("entities")]
        public _Entities Entities;

        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, object> Properties {get; set; }

        public void Convert(dynamic result)
        {
            var app = JsonConvert.DeserializeObject<searchskillLuis>(JsonConvert.SerializeObject(result, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
            Text = app.Text;
            AlteredText = app.AlteredText;
            Intents = app.Intents;
            Entities = app.Entities;
            Properties = app.Properties;
        }

        public (Intent intent, double score) TopIntent()
        {
            Intent maxIntent = Intent.None;
            var max = 0.0;
            foreach (var entry in Intents)
            {
                if (entry.Value.Score > max)
                {
                    maxIntent = entry.Key;
                    max = entry.Value.Score.Value;
                }
            }
            return (maxIntent, max);
        }
    }
}