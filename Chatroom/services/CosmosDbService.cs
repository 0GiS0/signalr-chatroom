using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

public class CosmosDbService : ICosmosDbService
{
    private Container _container;

    public CosmosDbService(CosmosClient dbClient, string databaseName, string containerName)
    {
        this._container = dbClient.GetContainer(databaseName, containerName);
    }

    public async Task AddMessageAsync(Message message)
    {
        await this._container.CreateItemAsync<Message>(message, new PartitionKey(message.ChatRoomId));
    }

    public async Task<IEnumerable<Message>> GetMessagesAsync(string queryString)
    {
        var query = this._container.GetItemQueryIterator<Message>(new QueryDefinition(queryString));
         var results = new List<Message>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                
                results.AddRange(response.ToList());
            }

            return results;
    }

    public Task<Message> GetMessageAsync(string id)
    {
        throw new System.NotImplementedException();
    }
}