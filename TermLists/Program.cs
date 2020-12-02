using System;
using System.IO;
using System.Threading;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;

namespace ContentModerator
{
    class Program
    {
        /// <summary>
        /// The language of the terms in the term lists.
        /// </summary>
        private const string lang = "spa";
        /// <summary>
        /// The minimum amount of time, in milliseconds, to wait between calls
        /// to the Content Moderator APIs.
        /// </summary>
        private const int throttleRate = 3000;

        /// <summary>
        /// The number of minutes to delay after updating the search index before
        /// performing image match operations against the list.
        /// </summary>
        private const double latencyDelay = 0.5;
        static void Main(string[] args)
        {

            //Create Content Moderator Client            
            var client = new ContentModeratorClient(new ApiKeyServiceClientCredentials("<YOUR_MODERATION_API_KEY"))
            {
                Endpoint = "https://ACCOUNT_NAME.cognitiveservices.azure.com/"
            };

            try
            {
                //1. Create a term list
                //There is a maximum limit of 5 term lists with each list to not exceed 10,000 terms.
                var listId = CreateTermList(client);
                //2. Add terms in the term list
                using (var file = new StreamReader("Insultos.txt"))
                {
                    string line;

                    while ((line = file.ReadLine()) != null)
                        AddTerm(client, listId, line);

                }
                //3. Get all terms in a term list
                GetAllTerms(client, listId);

                Console.WriteLine("Done");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                throw;
            }

        }

        private static string CreateTermList(ContentModeratorClient client)
        {
            Console.WriteLine($"Creating term list.");

            //https://www.elespanol.com/social/20181227/maravillosa-lista-insultos-castellano-perdiendo/363964046_0.html
            var body = new Body("Lista de insultos", "La maravillosa lista de insultos que el castellano está perdiendo");
            var list = client.ListManagementTermLists.Create("application/json", body);

            if (!list.Id.HasValue)
                throw new Exception($"{nameof(list.Id)} value missing.");

            var listId = list.Id.Value.ToString();
            Console.Write($"Term list created. ID {listId}");
            Thread.Sleep(throttleRate); //Your Content Moderator service key has a requests-per-second (RPS) rate limit, and if you exceed the limit, the SDK throws an exception with a 429 error code. A free tier key has a one-RPS rate limit.

            return listId;
        }

        private static void AddTerm(ContentModeratorClient client, string listId, string term)
        {
            Console.WriteLine($"Adding {term} into the term list");
            client.ListManagementTerm.AddTerm(listId, term, lang);
            Thread.Sleep(throttleRate);
        }

        private static void GetAllTerms(ContentModeratorClient client, string listId)
        {
            Console.WriteLine($"Getting terms in term list with ID {listId}");
            var terms = client.ListManagementTerm.GetAllTerms(listId, lang);

            foreach (var term in terms.Data.Terms)
            {
                Console.WriteLine($"{term.Term}");
            }

            Thread.Sleep(throttleRate);

        }
    }
}
