using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Tsla.Works.Models;

namespace Tsla.Works.Services
{
    public class TeslaCommands
    {
        private static HttpClient client = new HttpClient();

        public static async Task<Vehicles> GetVehicles(string token)
        {
            Vehicles vehicles = null;

            Uri url = new Uri("https://owner-api.teslamotors.com/api/1/vehicles/");

            SetupHeaders(token);

            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                vehicles = await response.Content.ReadAsAsync<Vehicles>().ConfigureAwait(false);
                response = (HttpResponseMessage)null;
            }
            else
            {
                string msg = string.Format("Uri: {0} \n\n Status Code: {1}\n", (object)response.RequestMessage.RequestUri, (object)response.StatusCode);
                throw new HttpRequestException(msg);
            }

            return vehicles;
        }

        public static async Task<bool> Wake(string token, string id)
        {
            bool response = false;
            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/wake_up", id);
            Uri uri = new Uri(url);

            SetupHeaders(token);

            response = await ApiPost(uri);
            return response;
        }

        public static async Task<bool> IsAwake(string token, string id)
        {
            bool awake = false;
            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/wake_up", id);
            Uri uri = new Uri(url);

            SetupHeaders(token);

            HttpResponseMessage response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                MobileEnabled mobile = await response.Content.ReadAsAsync<MobileEnabled>().ConfigureAwait(false);
                awake = mobile.response;
                mobile = (MobileEnabled)null;
            }
            return awake;
        }

        private static void SetupHeaders(string token)
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            string bearer = string.Format("bearer {0}", token);
            client.DefaultRequestHeaders.Add("Authorization", bearer);
            client.DefaultRequestHeaders.Remove("User-Agent");
            client.DefaultRequestHeaders.Add("User-Agent", "tsla.works");
        }

        private static async Task<bool> ApiPost(Uri url)
        {
            bool success = false;
            HttpResponseMessage response = await client.PostAsync(url, (HttpContent)null);
            if (response.IsSuccessStatusCode)
                success = true;
            return success;
        }
    }
}
