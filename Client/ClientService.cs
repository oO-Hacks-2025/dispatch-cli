using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using testing.Models;
using Utils.Constants;

namespace Client
{
    public class ClientService
    {
        private readonly HttpClient client = new();

        #region Calls
        public async Task<Call> Next()
        {
            SetHeader();


            var stream = await client.GetStreamAsync(RequestPath.Next);

            var result = await JsonSerializer.DeserializeAsync<Call>(stream) ?? throw new Exception("Failed to deserialize the response.");

            return result;
        }

        public async Task<List<Call>> Queue()
        {
            SetHeader();

            var stream = await client.GetStreamAsync(RequestPath.Queue);

            var result = await JsonSerializer.DeserializeAsync<List<Call>>(stream) ?? throw new Exception("Failed to deserialize the response.");

            return result;
        }
        #endregion

        #region Control
        public async Task<HttpStatusCode> Reset(string seed = "default", int targetDispatches = 10_000, int maxActiveCalls = 100)
        {

            SetHeader();

            var stream = client.GetStreamAsync(RequestPath.Reset);

            var httpContent = new StringContent(JsonSerializer.Serialize(new { seed, targetDispatches, maxActiveCalls }));

            var result = await client.PostAsync(RequestPath.Reset, httpContent);

            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to reset the game. Status code: {result.StatusCode}");
            }

            return result.StatusCode;
        }

        public async Task<GameStatus> Stop()
        {
            SetHeader();

            var stream = client.GetStreamAsync(RequestPath.Stop);

            var result = await client.PostAsync(RequestPath.Stop, null);

            var test = await result.Content.ReadAsStringAsync();

            var statusStream = await JsonSerializer.DeserializeAsync<GameStatus>(result.Content.ReadAsStream()) ?? throw new Exception("Failed to deserialize the response.");

            return statusStream;
        }

        public async Task<GameStatus> Status()
        {
            SetHeader();

            var stream = await client.GetStreamAsync(RequestPath.Status);

            var statusStream = await JsonSerializer.DeserializeAsync<GameStatus>(stream) ?? throw new Exception("Failed to deserialize the response.");

            return statusStream;
        }
        #endregion

        #region Locations
        public async Task<List<City>> GetLocations()
        {
            SetHeader();


            var stream = await client.GetStreamAsync(RequestPath.GetLocations);

            var result = await JsonSerializer.DeserializeAsync<List<City>>(stream) ?? throw new Exception("Failed to deserialize the response.");

            return result;
        }
        #endregion

        #region Medical
        public async Task<List<Availability>> Search()
        {
            SetHeader();

            var stream = await client.GetStreamAsync(RequestPath.Search);

            var result = await JsonSerializer.DeserializeAsync<List<Availability>>(stream) ?? throw new Exception("Failed to deserialize the response.");

            return result;
        }

        public async Task<string> SearchByCity(string county, string city)
        {
            SetHeader();

            var result = await client.GetStringAsync(RequestPath.SearchByCity + string.Concat("?county=", county, "&city=", city));

            //var result = await JsonSerializer.DeserializeAsync<Availability>(stream) ?? throw new Exception("Failed to deserialize the response.");

            return result;
        }

        public async Task<string> Dispatch(string sourceCounty, string sourceCity, string targetCounty, string targetCity, int quantity)
        {
            SetHeader();
            var httpContent = new StringContent(JsonSerializer.Serialize(new Dispatch(sourceCounty, sourceCity, targetCounty, targetCity, quantity)));

            var result = await client.PostAsync(RequestPath.Dispatch, httpContent);

            if (!result.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to dispatch the game. Status code: {result.StatusCode}");
            }

            return result.Content.ToString();
        }
        #endregion

        #region Private Methods
        private void SetHeader()
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }
        #endregion
    }
}