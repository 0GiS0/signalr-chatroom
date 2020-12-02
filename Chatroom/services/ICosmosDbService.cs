using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICosmosDbService
{
    Task<IEnumerable<Message>> GetMessagesAsync(string query);
    Task<Message> GetMessageAsync(string id);
    Task AddMessageAsync(Message message);
}