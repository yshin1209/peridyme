using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Gremlin.Net.Driver;
using Gremlin.Net.Structure.IO.GraphSON;
using System.Dynamic;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;


namespace peridymeAPI.Controllers
{
    [ApiController]
    public class peridymeAPIController : ControllerBase
    {
        private static string hostname = "yongjunshin2.gremlin.cosmosdb.azure.com";
        private static int port = 443;
        private static string authKey = "PGYEFyszpaHxkveIu2fiJI7KdSTSdbm5p1V3JOY4xwcLzd2euwYDtITUxJTKxmACdJddTuLvbf3HOwzdecc0pQ==";
        private static string database = "database";
        private static string collection = "graph";

        private static GremlinServer gremlinServer = new GremlinServer(hostname, port, enableSsl: true,
                                                        username: "/dbs/" + database + "/colls/" + collection,
                                                        password: authKey);

        /// <summary>
        /// Run any Gremlin command.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "command": "g.V()"
        ///     }
        /// 
        /// - "command": string
        /// - This sample command will return every vertex.
        /// </remarks>
        [HttpPost]
        [Route("run")]
        public async Task<dynamic> run([FromBody] JObject requestBody)
        {
            var command = (string)requestBody.SelectToken("command");

            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                var response = await gremlinClient.SubmitAsync<dynamic>($"{command}");
                return response;
            }
        }

        /// <summary>
        /// Add a vertex.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "label": "patient",
        ///        "id": "pt1234"  
        ///     }
        /// 
        /// - "label" (vertex label): string
        /// - "id" (vertex id): string (must be unique)
        /// - This example will add a new vertex to the graph.
        /// - Make sure to use a vertex id that does not already exist. Otherwise, you may get "Error: Resource with specified id or name already exists".
        /// </remarks>
        [HttpPost]
        [Route("addV")]
        public async Task<dynamic> AddV([FromBody] JObject requestBody)
        {
            var label = (string)requestBody.SelectToken("label");
            var id = (string)requestBody.SelectToken("id");

            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                var response = await gremlinClient.SubmitAsync<dynamic>($"g.addV('{label}').property('id', '{id}')");
                return response;
            }
        }

        /// <summary>
        /// Remove a vertex.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "id": "pt1234"
        ///     }
        /// 
        /// - "id" (vertex id): string
        /// - This example will remove the vertex with the specified id from the graph.
        /// </remarks>
        [HttpPost]
        [Route("removeV")]
        public async Task<dynamic> RemoveV([FromBody] JObject requestBody)
        {
            var id = (string)requestBody.SelectToken("id");

            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                await gremlinClient.SubmitAsync<dynamic>($"g.V('{id}').drop()");
                return "ok";
            }
        }

        /// <summary>
        /// Set the value of a vertex property.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "id": "pt1234",
        ///        "key": "age",
        ///        "value": 35,
        ///        "publish": "yes"
        ///     }
        ///     
        /// - "id" (vertex id): string (make sure to use an existing vertex id)
        /// - "key" (vertex property key): string
        /// - "value" (vertex property value): string, int, float, bool
        /// - "publish" ("yes" or "no"): string (if "yes", the property is published to the Azure Event Grid topic)
        /// - If the property dose not exist, it will be created.
        /// </remarks>
        [HttpPost]
        [Route("setVProperty")]
        public async Task<dynamic> SetVProperty([FromBody] JObject requestBody)
        {
            var id = (string)requestBody.SelectToken("id");
            var key = (string)requestBody.SelectToken("key");
            JTokenType valueType = requestBody.SelectToken("value").Type;
            dynamic value = string.Empty; // initialize dynamic type value
            switch (valueType)
            {
                case JTokenType.Float:
                    value = (float)requestBody.SelectToken("value");
                    break;
                case JTokenType.Boolean:
                    value = (bool)requestBody.SelectToken("value");
                    break;
                case JTokenType.Integer:
                    value = (int)requestBody.SelectToken("value");
                    break;
                case JTokenType.String:
                    value = "'" + (string)requestBody.SelectToken("value") + "'";
                    break;
            }

            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                var response = await gremlinClient.SubmitAsync<dynamic>($"g.V('{id}').property('{key}', {value})");
                var publish = (string)requestBody.SelectToken("publish");
                if (publish == "yes")
                {
                    string AzureEventGridTopicEndPoint = "https://topic.eastus-1.eventgrid.azure.net/api/events?api-version=2018-01-01";
                    //string AzureEventGridTopicEndPoint = "https://topic.eastus-1.eventgrid.azure.net/api/events";
                    string AzureEventGridTopicAccessKey = "bHLip04YkH3Ysh0WvISAEUINVk3BWcPGTqGB6t/0iQw=";
                    // [Warning] Be careful not to add "/" at the end of publisherBaseUri
                    string publisherBaseUri = "https://peridymeapi2.azurewebsites.net";
                    string uri = AzureEventGridTopicEndPoint;
                    //Create topicSubject (to be used for event filtering) using propery key and value
                    string topicSubject = id + "-" + value;
                    requestBody.Add("publisherBaseUri", publisherBaseUri);

                    // Event data schema (Azure Event Grid)
                    // https://docs.microsoft.com/en-us/azure/event-grid/post-to-custom-topic#event-data

                    dynamic publishBody = new ExpandoObject();
                    publishBody.id = "notSet";
                    publishBody.eventType = "notSet";
                    publishBody.subject = topicSubject; // e.g., bloodSodium_104
                    publishBody.eventTime = DateTime.Now;
                    publishBody.data = requestBody;
                    publishBody.dataVersion = "v1";
                    List<dynamic> publishBodyArray = new List<dynamic>();
                    publishBodyArray.Add(publishBody);

                    using (HttpClient client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("aeg-sas-key", AzureEventGridTopicAccessKey);
                        var publishRequest = new HttpRequestMessage(HttpMethod.Post, uri);
                        string jsonPublishBody = JsonConvert.SerializeObject(publishBodyArray);
                        publishRequest.Content = new StringContent(jsonPublishBody, Encoding.UTF8, "application/json");
                        await client.SendAsync(publishRequest);
                    }
                }
                return response;
            }
        }

        /// <summary>
        /// Get the value of a vertex property.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "id": "pt1234",
        ///        "key": "age"
        ///     }
        ///     
        /// - "id" (vertex id): string (make sure to use an existing vertex id)
        /// - "key" (property key): string
        /// </remarks>

        [HttpPost]
        [Route("getVProperty")]
        public async Task<dynamic> GetVProperty([FromBody] JObject requestBody)
        {
            var id = (string)requestBody.SelectToken("id"); // Vertex ID
            var key = (string)requestBody.SelectToken("key"); // Property key

            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                var response = await gremlinClient.SubmitAsync<dynamic>($"g.V('{id}').values('{key}')");
                return response;
            }
        }

        /// <summary>
        /// Remove a vertex property.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "id": "pt1234",
        ///        "key": "age"
        ///     }
        ///     
        /// - "id" (vertex id): string (make sure to use an existing vertex id)
        /// - "key" (property key): string
        /// </remarks>

        [HttpPost]
        [Route("removeVProperty")]
        public async Task<dynamic> RemoveVProperty([FromBody] JObject requestBody)
        {
            var id = (string)requestBody.SelectToken("id"); // Vertex ID
            var key = (string)requestBody.SelectToken("key"); // Property key

            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                await gremlinClient.SubmitAsync<dynamic>($"g.V('{id}').properties('{key}').drop()");
                return "ok";
            }
        }

        /// <summary>
        /// Creates an edge between two vertices.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///  {  
        ///    "outV": "pt1234-bloodGlucose",
        ///    "label": "controlledBy",
        ///    "inV": "pt1234-PID",
        ///    "subscribe": "yes" --> make sure the quotation marks are correct when copied!
        ///  }
        ///     
        /// - "outV" (outgoing vertex id): string (make sure to use an existing vertex id)
        /// - "label" (edge label): string 
        /// - "inV" (incoming vertex id): string (make sure to use an existing vertex id)
        /// - "subscribe" ({inV} subscribes to {outV} update events): string ("yes" or "no")
        /// </remarks>

        [HttpPost]
        [Route("addE")]
        public async Task<dynamic> AddE([FromBody] JObject requestBody)
        {

            var outV = (string)requestBody.SelectToken("outV");
            var edgeLabel = (string)requestBody.SelectToken("label");
            var inV = (string)requestBody.SelectToken("inV");
            var subscribe = (string)requestBody.SelectToken("subscribe");

            if (subscribe == "yes")
            {
                // Event Grid subscription
                await RunSubscription(outV, inV, edgeLabel);
            }
            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                var response = await gremlinClient.SubmitAsync<dynamic>($"g.V('{outV}').addE('{edgeLabel}').to(g.V('{inV}'))");
                return response;
            }
        }

        /// <summary>
        /// Remove an edge between two vertices.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "outV": "pt1234",
        ///        "label": "has",
        ///        "inV": "hemoglobin"
        ///     }
        ///     
        /// - "outV" (outgoing vertex id): string (make sure to use an existing vertex id)
        /// - "label" (edge label): string 
        /// - "inV" (incoming vertex id): string (make sure to use an existing vertex id)
        /// </remarks>
        [HttpPost]
        [Route("removeE")]
        public async Task<dynamic> RemoveE([FromBody] JObject requestBody)
        {
            var outV = (string)requestBody.SelectToken("outV");
            var label = (string)requestBody.SelectToken("label");
            var inV = (string)requestBody.SelectToken("inV");
            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                await gremlinClient.SubmitAsync<dynamic>($"g.V('{outV}').outE().where(otherV().hasId('{inV}')).drop()");
                return "ok";
            }
        }

        /// <summary>
        /// Set the value of an edge property.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "outV": "person1",
        ///        "label": "knows",
        ///        "inV": "person2",
        ///        "key": "weight",
        ///        "value": 0.4,
        ///     }
        ///     
        /// 
        /// - "outV" (outgoing vertex id): string (make sure to use an existing vertex id)
        /// - "label" (edge label): string 
        /// - "inV" (incoming vertex id): string (make sure to use an existing vertex id)
        /// - "key" (edge property key): string
        /// - "value" (edge property value): string, int, float, bool
        /// - If the property dose not exist, it will be created.
        /// </remarks>
        [HttpPost]
        [Route("setEProperty")]
        public async Task<dynamic> SetEProperty([FromBody] JObject requestBody)
        {
            var outV = (string)requestBody.SelectToken("outV");
            var label = (string)requestBody.SelectToken("label");
            var inV = (string)requestBody.SelectToken("inV");
            var key = (string)requestBody.SelectToken("key");
            JTokenType valueType = requestBody.SelectToken("value").Type;
            dynamic value = string.Empty; // initialize dynamic type value
            switch (valueType)
            {
                case JTokenType.Float:
                    value = (float)requestBody.SelectToken("value");
                    break;
                case JTokenType.Boolean:
                    value = (bool)requestBody.SelectToken("value");
                    break;
                case JTokenType.Integer:
                    value = (int)requestBody.SelectToken("value");
                    break;
                case JTokenType.String:
                    value = "'" + (string)requestBody.SelectToken("value") + "'";
                    break;
            }

            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                string commandString = $"g.V('{outV}').outE('{label}').as('e').inV().has('id', '{inV}').select('e').property('{key}', {value})";
                var response = await gremlinClient.SubmitAsync<dynamic>(commandString);
                return response;
            }
        }

        /// <summary>
        /// Get the value of an edge property.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "outV": "person1",
        ///        "label": "knows",
        ///        "inV": "person2",
        ///        "key": "weight"
        ///     }
        ///     
        /// 
        /// - "outV" (outgoing vertex id): string (make sure to use an existing vertex id)
        /// - "label" (edge label): string 
        /// - "inV" (incoming vertex id): string (make sure to use an existing vertex id)
        /// - "key" (edge property key): string
        /// </remarks>
        [HttpPost]
        [Route("getEProperty")]
        public async Task<dynamic> GetEProperty([FromBody] JObject requestBody)
        {
            var outV = (string)requestBody.SelectToken("outV");
            var label = (string)requestBody.SelectToken("label");
            var inV = (string)requestBody.SelectToken("inV");
            var key = (string)requestBody.SelectToken("key");
            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                string commandString = $"g.V('{outV}').outE('{label}').as('e').inV().has('id', '{inV}').select('e').values('{key}')";
                var response = await gremlinClient.SubmitAsync<dynamic>(commandString);
                return response;
            }
        }

        /// <summary>
        /// Remove an edge property.
        /// </summary>
        /// <remarks>
        /// 1. Click [Try it out] button (white).
        /// 2. Type your request body into "Example Value  | Model" textbox (white). A sample request body is shown above.
        /// 3. Click [Execute] button (blue).
        /// 4. Check [Response body] below.
        /// 
        /// 
        ///     {
        ///        "outV": "person1",
        ///        "label": "knows",
        ///        "inV": "person2",
        ///        "key": "weight"
        ///     }
        ///     
        /// 
        /// - "outV" (outgoing vertex id): string (make sure to use an existing vertex id)
        /// - "label" (edge label): string 
        /// - "inV" (incoming vertex id): string (make sure to use an existing vertex id)
        /// - "key" (edge property key): string
        /// </remarks>

        [HttpPost]
        [Route("removeEProperty")]
        public async Task<dynamic> RemoveEProperty([FromBody] JObject requestBody)
        {
            var outV = (string)requestBody.SelectToken("outV");
            var label = (string)requestBody.SelectToken("label");
            var inV = (string)requestBody.SelectToken("inV");
            var key = (string)requestBody.SelectToken("key");
            using (var gremlinClient = new GremlinClient(gremlinServer, new GraphSON2Reader(), new GraphSON2Writer(), GremlinClient.GraphSON2MimeType))
            {
                string commandString = $"g.V('{outV}').outE('{label}').as('e').inV().has('id', '{inV}').select('e').properties('{key}').drop()";
                await gremlinClient.SubmitAsync<dynamic>(commandString);
                return "ok";
            }
        }

        [HttpPost]
        [Route("botRequest")]
        public async Task<dynamic> botRequest([FromBody] JObject botRequestBody)
        {
            var topIntent = (string)botRequestBody.SelectToken("topIntent");
            dynamic requestBody = new ExpandoObject();
            string jsonRequestBody = string.Empty;
            if (topIntent == "AddV")
            {
                string labelFound = String.Empty;
                string idFound = String.Empty;
                string json = JsonConvert.SerializeObject(botRequestBody);

                var labelJToken = JObject.Parse(json)["entities"]["label"];
                labelFound = Trim(labelJToken);
                var idJToken = JObject.Parse(json)["entities"]["id"];
                idFound = Trim(idJToken);

                requestBody.label = labelFound;
                requestBody.id = idFound;
                jsonRequestBody = JsonConvert.SerializeObject(requestBody);
                var jObjectRequestBody = JsonConvert.DeserializeObject<dynamic>(jsonRequestBody);
                await AddV(jObjectRequestBody);
            } else if (topIntent == "SetVProperty")
            {
                string idFound = String.Empty;
                string keyFound = String.Empty;
                string valueFound = String.Empty;
                string publishFound = String.Empty;
                string json = JsonConvert.SerializeObject(botRequestBody);

                var idJToken = JObject.Parse(json)["entities"]["id"];
                idFound = Trim(idJToken);
                var keyJToken = JObject.Parse(json)["entities"]["key"];
                keyFound = Trim(keyJToken);
                var valueJToken = JObject.Parse(json)["entities"]["value"];
                valueFound = Trim(valueJToken);
                var publishJToken = JObject.Parse(json)["entities"]["publish"];
                publishFound = Trim(publishJToken);

                requestBody.id = idFound;
                requestBody.key = keyFound;
                requestBody.value= valueFound;
                requestBody.publish = publishFound;

                jsonRequestBody = JsonConvert.SerializeObject(requestBody);
                var jObjectRequestBody = JsonConvert.DeserializeObject<dynamic>(jsonRequestBody);
                await SetVProperty(jObjectRequestBody);
            }
            return jsonRequestBody;
        }
        public static async Task RunSubscription(string outV, string inV, string edgeLabel)
        {
            string requestTokenUrl = "https://login.microsoftonline.com/17f1a87e-2a25-4eaa-b9df-9d439034b080/oauth2/token";

            string accessToken = String.Empty;

            using (HttpClient client = new HttpClient())
            {
                var requestTokenBody = new Dictionary<string, string>();
                requestTokenBody.Add("grant_type", "client_credentials");
                requestTokenBody.Add("client_id", "a2216a94-90c8-48a0-a256-0ae1756e0de0");
                requestTokenBody.Add("client_secret", "zz0bHBHo2MfGiEzsNEKpS17OGDt1JzS5BT8o4vuY+T4=");
                requestTokenBody.Add("resource", "https://management.core.windows.net/");
                var request = new HttpRequestMessage(HttpMethod.Post, requestTokenUrl) { Content = new FormUrlEncodedContent(requestTokenBody) };

                HttpResponseMessage response = await client.SendAsync(request);
                string responseJson = await response.Content.ReadAsStringAsync();
                JObject jobj = JsonConvert.DeserializeObject<dynamic>(responseJson);
                accessToken = (string)jobj.SelectToken("access_token");
            }
            string subscriptionId = "f269bf17-a2eb-423a-a35d-fe0a4c7184b1";
            string resourceGroupName = "nanoservice";
            string topicName = "topic";
            string scope = $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.EventGrid/topics/{topicName}";
            string eventSubscriptionName = outV + "-" + inV;
            string subscribeUrl = $"https://management.azure.com/{scope}/providers/Microsoft.EventGrid/eventSubscriptions/{eventSubscriptionName}?api-version=2018-09-15-preview";
            string subscribeRequestBodyJson = @"{
                'properties': {
                    'destination': {
                        'endpointType': 'WebHook',
                        'properties': {
                            'endpointUrl': ''
                        }
                    },
                    'filter': {
                        'isSubjectCaseSensitive': false,
                        'subjectBeginsWith': '',
                        'subjectEndsWith': ''
                    }
                }
            }";

            JObject subscribeRequestBody = JsonConvert.DeserializeObject<dynamic>(subscribeRequestBodyJson);
            if (outV.EndsWith("PID"))
            {
                //PID Azure Function
                subscribeRequestBody["properties"]["destination"]["properties"]["endpointUrl"] = "https://peridymefunction2.azurewebsites.net/runtime/webhooks/EventGrid?functionName=PID&code=ccnu7MIgTPm50G03evUEKl28TaUY3CzdQRJuiWBCDZnuRLvtwnoYaA==";
                subscribeRequestBody["properties"]["filter"]["subjectBeginsWith"] = inV;
                subscribeRequestBodyJson = JsonConvert.SerializeObject(subscribeRequestBody);
            }

            if (edgeLabel == "activates")
            {
                subscribeRequestBody["properties"]["destination"]["properties"]["endpointUrl"] = "https://peridymefunction2.azurewebsites.net/runtime/webhooks/EventGrid?functionName=EGActivation&code=ccnu7MIgTPm50G03evUEKl28TaUY3CzdQRJuiWBCDZnuRLvtwnoYaA==";
                // subscribeRequestBody["properties"]["filter"]["subjectBeginsWith"] = outV;
                subscribeRequestBodyJson = JsonConvert.SerializeObject(subscribeRequestBody);
            }


            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
                var subscribeRequest = new HttpRequestMessage(HttpMethod.Put, subscribeUrl);
                subscribeRequest.Content = new StringContent(subscribeRequestBodyJson, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.SendAsync(subscribeRequest);
                string responseJson = await response.Content.ReadAsStringAsync();
            }
        }

        public static string Trim (JToken jToken)
        {
            var json = JsonConvert.SerializeObject(jToken);
            json = json.TrimStart('[').TrimEnd(']');
            return json.TrimStart('"').TrimEnd('"');
        }
    }
}