using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Collections.Concurrent;
namespace DocumentDBDemo1
{
    class Program
    {
        private const string EndpointUrl = "https://document-temp.documents.azure.com:443/";
        private const string AuthorizationKey =
           "1I0coHlBwKercLqxf8kS78PqnGuXX0iHSAmN6jg1w4UTWfjWDoHrjIRE6M9LJMJFCwLYzmSWH2Zx2zfn7uTupQ==";
        private static Database database;
        private static DocumentCollection collection;
        static void Main(string[] args)
        {
            try
            {
                CreateDocumentClient().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.InnerException);
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }

            //Console.ReadKey();
        }

        private static async Task CreateDocumentClient()
        {
            // Create a new instance of the DocumentClient
            using (var client = new DocumentClient(new Uri(EndpointUrl), AuthorizationKey))
            {
                
                string connectionString = ConfigurationManager.AppSettings["PremiumStorageConnectionString"];
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                Console.WriteLine("Creating Table if it doesn't exist...");
                CloudTable table = tableClient.GetTableReference("People");
                table.DeleteIfExists();
                table.CreateIfNotExists();

                //await CreateDatabase(client);
                database = client.CreateDatabaseQuery("SELECT * FROM c WHERE c.id = 'mynewdb1'").AsEnumerable().First();
                //await CreateCollection(client, "MyCollection");
                //database = client.CreateDatabaseQuery("SELECT * FROM c WHERE c.id = 'mynewdb'").AsEnumerable().First();
                collection = client.CreateDocumentCollectionQuery(database.CollectionsLink,"SELECT * FROM c WHERE c.id = 'MyCollection'").AsEnumerable().First();
                //await CreateDocuments(client);
                await QueryDocumentsWithPaging(client,table);
            }
        }
        private async static Task CreateDatabase(DocumentClient client)
        {
            Console.WriteLine();
            Console.WriteLine("******** Create Database *******");

            var databaseDefinition = new Database { Id = "mynewdb1" };
            var result = await client.CreateDatabaseAsync(databaseDefinition);
            var database = result.Resource;

            Console.WriteLine(" Database Id: {0}; Rid: {1}", database.Id, database.ResourceId);
            Console.WriteLine("******** Database Created *******");
        }
        public class CustomerEntity : TableEntity
        {
            public CustomerEntity(string lastName, string firstName)
            {
                this.PartitionKey = lastName;
                this.RowKey = firstName;
            }

            public CustomerEntity() { }

            public string Email { get; set; }

            public string PhoneNumber { get; set; }

            public string Bio { get; set; }
        }
        private async static Task CreateCollection(DocumentClient client, string collectionId,
        string offerType = "S1")
        {

            Console.WriteLine();
            Console.WriteLine("**** Create Collection {0} in {1} ****", collectionId, database.Id);

            var collectionDefinition = new DocumentCollection { Id = collectionId };
            var options = new RequestOptions { OfferType = offerType };
            var result = await client.CreateDocumentCollectionAsync(database.SelfLink,
               collectionDefinition, options);
            var collection = result.Resource;

            Console.WriteLine("Created new collection");
            ViewCollection(collection);
        }

        private static void ViewCollection(DocumentCollection collection)
        {
            Console.WriteLine("Collection ID: {0} ", collection.Id);
            Console.WriteLine("Resource ID: {0} ", collection.ResourceId);
            Console.WriteLine("Self Link: {0} ", collection.SelfLink);
            Console.WriteLine("Documents Link: {0} ", collection.DocumentsLink);
            Console.WriteLine("UDFs Link: {0} ", collection.UserDefinedFunctionsLink);
            Console.WriteLine("StoredProcs Link: {0} ", collection.StoredProceduresLink);
            Console.WriteLine("Triggers Link: {0} ", collection.TriggersLink);
            Console.WriteLine("Timestamp: {0} ", collection.Timestamp);
        }

        private async static Task CreateDocuments(DocumentClient client)
        {
            Console.WriteLine();
            Console.WriteLine("**** Create Documents ****");
            Console.WriteLine();

            dynamic document1Definition = new
            {
                name = "New Customer 1",
                address = new
                {
                    addressType = "Main Office",
                    addressLine1 = "123 Main Street",
                    location = new
                    {
                        city = "Brooklyn",
                        stateProvinceName = "New York"
                    },
                    postalCode = "11229",
                    countryRegionName = "United States"
                },
            };

            Document document1 = await CreateDocument(client, document1Definition);
            document1 = await CreateDocument(client, document1Definition);
            document1 = await CreateDocument(client, document1Definition);
            document1 = await CreateDocument(client, document1Definition);
            document1 = await CreateDocument(client, document1Definition);
            Console.WriteLine("Created document {0} from dynamic object", document1.Id);
            Console.WriteLine();
        }
        private async static Task<Document> CreateDocument(DocumentClient client,
   object documentObject)
        {

            var result = await client.CreateDocumentAsync(collection.SelfLink, documentObject);
            var document = result.Resource;

            Console.WriteLine("Created new document: {0}\r\n{1}", document.Id, document);
            return result;
        }
        private async static Task QueryDocumentsWithPaging(DocumentClient client,CloudTable table)
        {
            Console.WriteLine();
            Console.WriteLine("**** Query Documents (paged results) ****");
            Console.WriteLine();
            Console.WriteLine("Quering for all documents");

            var sql = "SELECT * FROM c";
            var query = client.CreateDocumentQuery(collection.SelfLink, sql).AsDocumentQuery();

            while (query.HasMoreResults)
            {
                var documents = await query.ExecuteNextAsync();

                foreach (var document in documents)
                {
                    CustomerEntity item = new CustomerEntity()
                    {
                        PartitionKey = document.id,
                        RowKey = "12345",
                        Email = "umair@contoso.com",
                        PhoneNumber = "425-555-0102",
                        Bio = document.name
                    };
                    TableOperation insertOperation = TableOperation.Insert(item);
                    table.Execute(insertOperation);
                    Console.WriteLine(" Id: {0}; Name: {1};", document.id, document.name);
                }
            }

            Console.WriteLine();



        }


    }
}
