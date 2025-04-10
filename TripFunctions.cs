using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Xml.Serialization;

public static class TripFunctions
{
    // [FunctionName("GetTripDetails")]
    // public static IActionResult GetTripDetails(
    //     [HttpTrigger(AuthorizationLevel.Function, "get", Route = "trip/{id}")] HttpRequest req,
    //     string id,
    //     ILogger log)
    // {
    //     log.LogInformation($"Fetching details for trip ID: {id}");

    //     // Example response
    //     var tripDetails = new
    //     {
    //         Id = id,
    //         Destination = "Paris",
    //         StartDate = "2023-10-01",
    //         EndDate = "2023-10-10"
    //     };

    //     return new OkObjectResult(tripDetails);
    // }

    [FunctionName("GetTripsByDateRouteDirection")]
    public static async Task<IActionResult> GetTripsByDateRouteDirection(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "trips")] HttpRequest req,
        ILogger log)
    {
        log.LogInformation("Fetching trips by date, route, and direction");

        string date = req.Query["date"];
        string route = req.Query["route"];
        string direction = req.Query["direction"];

        if (string.IsNullOrEmpty(date) || string.IsNullOrEmpty(route) || string.IsNullOrEmpty(direction))
        {
            return new BadRequestObjectResult("Missing required query parameters: date, route, or direction");
        }

        using (var httpClient = new HttpClient())
        {
            try
            {
                var apiUrl = $"https://goapi.metrolinx.com/GoData/GODataAPIService.svc/web/Schedule/Timetable/Route/{date}/{route}/{direction}?key=10020115";
                log.LogInformation($"API URL: {apiUrl}");
                //string apiUrl = $"https://api.example.com/trips?date={date}&route={route}&direction={direction}";
                var response = await httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    log.LogError($"Failed to fetch trips. Status code: {response.StatusCode}");
                    return new StatusCodeResult((int)response.StatusCode);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                log.LogInformation($"Response content: {responseContent}");
                var serializer = new XmlSerializer(typeof(AllSchedules));
                using var reader = new StringReader(responseContent);
                AllSchedules schedules = (AllSchedules)serializer.Deserialize(reader);

                //var trips = JsonConvert.DeserializeObject<dynamic>(responseContent);

                return new OkObjectResult(schedules);
            }
            catch (Exception ex)
            {
                log.LogError($"Error fetching trips: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}