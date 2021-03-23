using Microsoft.AspNetCore.SignalR;
using System;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;
using Microsoft.Extensions.Options;

public class Chat : Hub
{
    private readonly ICosmosDbService _cosmosDbService;
    private readonly IOptions<LUISConfiguration> _luisConfiguration;
    private readonly IOptions<ContentModeratorConfiguration> _contentModeratorConfiguration;

    public Chat(ICosmosDbService cosmosDbService, IOptions<LUISConfiguration> luisConfiguration, IOptions<ContentModeratorConfiguration> contentModeratorConfiguration)
    {
        _cosmosDbService = cosmosDbService;
        _luisConfiguration = luisConfiguration;
        _contentModeratorConfiguration = contentModeratorConfiguration;
    }

    //Broadcast the message to all clients
    public void BroadcastMessage(string name, string message, decimal currentTime, DateTime date)
    {
        var ugly = false;
        var terms = new List<string>();

        //Content Moderator
        //Check if the content of the message is offensive
        var client = new ContentModeratorClient(new ApiKeyServiceClientCredentials(_contentModeratorConfiguration.Value.ApiKey))
        {
            Endpoint = _contentModeratorConfiguration.Value.Endpoint
        };


        try
        {
            var result = client.TextModeration.ScreenText("text/plain", new MemoryStream(Encoding.UTF8.GetBytes(message)), listId: "91", language: "spa");
            ugly = result.Terms != null && result.Terms.Count > 0;

            if (ugly) //If it's ugly with content moderator I don't call LUIS
            {
                foreach (var term in result.Terms)
                {
                    Console.WriteLine($"Ugly term: {term.Term}");
                    terms.Add(term.Term);
                }

                //Clients is an interface that gives you access to all connected clients
                Clients.All.SendAsync("broadcastMessage", name, message, currentTime, ugly, terms);
            }
            else
            { //but if content moderator says it's ok I'll do a double check with LUIS

                var httpClient = new HttpClient();
                var queryString = HttpUtility.ParseQueryString(string.Empty);

                //The request header contains your subscription key
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _luisConfiguration.Value.ApiKey);

                //The "q" parameter contains the uterance to send to LUIS
                queryString["query"] = message;

                // These optional request parameters are set to their default values
                queryString["verbose"] = "true";
                queryString["show-all-intents"] = "true";
                queryString["staging"] = "false";
                queryString["timezoneOffset"] = "0";
                queryString["log"] = "true";

                var predictionEndpointUri = String.Format("{0}luis/prediction/v3.0/apps/{1}/slots/production/predict?{2}", _luisConfiguration.Value.PredictionEndpoint, _luisConfiguration.Value.AppId, queryString);

                Console.WriteLine("endpoint: " + _luisConfiguration.Value.PredictionEndpoint);
                Console.WriteLine("appId: " + _luisConfiguration.Value.AppId);
                Console.WriteLine("queryString: " + queryString);
                Console.WriteLine("endpointUri: " + predictionEndpointUri);

                var response = httpClient.GetAsync(predictionEndpointUri).GetAwaiter().GetResult();

                var strResponseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                // Display the JSON result from LUIS.
                Console.WriteLine(strResponseContent.ToString());

                dynamic data = JsonConvert.DeserializeObject(strResponseContent);

                Console.WriteLine($"This sentence is {data.prediction.topIntent}");

                if (data.prediction.topIntent == "Offensive")
                    //Clients is an interface that gives you access to all connected clients
                    Clients.Client(Context.ConnectionId).SendAsync("echo", name, "[PRIVADO] Este tipo de mensajes no están permitidos", currentTime, false, null, true);
                else
                    //Clients is an interface that gives you access to all connected clients
                    Clients.All.SendAsync("broadcastMessage", name, message, currentTime, false, null);

            }

        }
        catch (Exception error)
        {
            Console.WriteLine($"Error: {error}");
        }

        //Save in CosmosDb
        _cosmosDbService.AddMessageAsync(new Message
        {
            Id = Guid.NewGuid().ToString(),
            ChatRoomId = "chatroom1",
            Date = date,
            UserName = name,
            VideoTime = currentTime,
            Text = message,
            Ugly = ugly,
            Terms = terms
        });
    }

    //Sends the message back to the caller
    public void Echo(string name, string message)
    {
        Clients.Client(Context.ConnectionId).SendAsync("echo", name, message + " (echo from server)");
    }
}
