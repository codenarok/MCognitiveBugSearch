using System.Threading.Tasks;
using System.Linq;
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

public class Search
{
    private readonly ILogger _logger;
    private readonly SearchClient _searchClient;

    public Search(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Search>();

        // Load configuration from environment variables
        string serviceEndpoint = Environment.GetEnvironmentVariable("AZURE_SEARCH_SERVICE_ENDPOINT");
        string indexName = Environment.GetEnvironmentVariable("AZURE_SEARCH_INDEX_NAME");
        string userAssignedClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");

        var credential = new DefaultAzureCredential(
            new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = userAssignedClientId
            });

        _searchClient = new SearchClient(new Uri(serviceEndpoint), indexName, credential);
    }

    [Function("Search")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "search")] HttpRequestData req)
    {
        _logger.LogInformation("Processing a request to the search endpoint.");

        // Retrieve the query string (e.g., ?query=bugs)
        var queryParams = HttpUtility.ParseQueryString(req.Url.Query);
        var searchQuery = queryParams["query"] ?? string.Empty;

        if (string.IsNullOrEmpty(searchQuery))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Query parameter is required.");
            return badRequest;
        }

        try
        {
            // Limit results to 3 by using SearchOptions with Size = 3
            var options = new SearchOptions { Size = 3 };

            var resultsResponse = await _searchClient.SearchAsync<SearchDocument>(searchQuery, options);
            var simplifiedResults = resultsResponse.Value.GetResults()
                .Select(r => new
                {
                    BugID = r.Document.ContainsKey("BugID") ? r.Document["BugID"] : null,
                    SubmissionID = r.Document.ContainsKey("SubmissionID") ? r.Document["SubmissionID"] : null,
                    RequirementNoFull = r.Document.ContainsKey("RequirementNoFull") ? r.Document["RequirementNoFull"] : null,
                    BugType = r.Document.ContainsKey("BugType") ? r.Document["BugType"] : null
                });

            var resultObject = new
            {
                TotalCount = resultsResponse.Value.TotalCount,
                Results = simplifiedResults
            };

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(resultObject);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search operation failed.");

            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Search operation failed.");
            return errorResponse;
        }
    }
}
