using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using System;
using System.Net;
using System.Web;
using System.Threading.Tasks;
using System.Linq;

public class Search
{
 private readonly ILogger _logger;
 private readonly SearchClient _searchClient;

 public Search(ILoggerFactory loggerFactory)
 {
     _logger = loggerFactory.CreateLogger<Search>();

     string serviceEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT");
     string indexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX_NAME");

#if DEBUG
     var credential = new AzureCliCredential();
#else
     var credential = new DefaultAzureCredential();
#endif

     _logger.LogInformation($"🔐 Using credential: {credential.GetType().Name}");
     _searchClient = new SearchClient(new Uri(serviceEndpoint), indexName, credential);
 }

 [Function("Search")]
 public async Task<HttpResponseData> Run(
     [HttpTrigger(AuthorizationLevel.Function, "get", Route = "search")] HttpRequestData req)
 {
     _logger.LogInformation("Processing a request to the search endpoint.");
     var queryParams = HttpUtility.ParseQueryString(req.Url.Query);
     var searchQuery = queryParams["query"] ?? string.Empty;

     var response = req.CreateResponse();

     response.Headers.Add("Access-Control-Allow-Origin", "*");
     response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
     response.Headers.Add("Access-Control-Allow-Methods", "GET");

     if (string.IsNullOrEmpty(searchQuery))
     {
         response.StatusCode = HttpStatusCode.BadRequest;
         await response.WriteStringAsync("Query parameter is required.");
         return response;
     }

     try
     {
         var options = new SearchOptions { Size = 3 };
         var resultsResponse = await _searchClient.SearchAsync<SearchDocument>(searchQuery, options);
         var simplifiedResults = resultsResponse.Value.GetResults()
             .Select(r => new
             {
                 BugID = r.Document.TryGetValue("BugID", out var bugId) ? bugId : null,
                 SubmissionID = r.Document.TryGetValue("SubmissionID", out var subId) ? subId : null,
                 RequirementNoFull = r.Document.TryGetValue("RequirementNoFull", out var reqNo) ? reqNo : null,
                 BugType = r.Document.TryGetValue("BugType", out var bugType) ? bugType : null
             });

         var resultObject = new
         {
             TotalCount = resultsResponse.Value.TotalCount,
             Results = simplifiedResults
         };

         await response.WriteAsJsonAsync(resultObject);
         return response;
     }
     catch (Exception ex)
     {
         _logger.LogError(ex, "Search operation failed.");
         response.StatusCode = HttpStatusCode.InternalServerError;
         await response.WriteStringAsync("Internal Server Error");
         return response;
     }
 }
}
