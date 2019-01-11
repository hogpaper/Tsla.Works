using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Tsla.Works.Models;

namespace Tsla.Works.Services
{
    public class TeslaCommands
    {
        private static HttpClient client = new HttpClient();
        public static IConfigurationRoot Configuration { get; set; }

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
            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/wake_up", id);
            return await ApiPost(token, url);
        }

        public static async Task<bool> StopCharging(string token, string id)
        {
            WakeUp(token, id);

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_stop", id);
            
            return await ApiPost(token, url);
        }

        public static async Task<bool> StartCharging(string token, string id)
        {
            WakeUp(token, id);

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_start", id);

            return await ApiPost(token, url);
        }

        public static async Task<bool> Unlock(string token, string id)
        {
            WakeUp(token, id);

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/door_unlock", id);

            return await ApiPost(token, url);
        }

        public static async Task<bool> Lock(string token, string id)
        {
            WakeUp(token, id);

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/door_lock", id);

            return await ApiPost(token, url);
        }

        private static void WakeUp(string token, string id)
        {
            while (!Task.Run<bool>(async () => await IsAwake(token, id)).Result)
            {
                Task.Run(async () => await Wake(token, id));
            }
        }

        public static async Task<bool> IsAwake(string token, string id)
        {
            bool awake = false;
            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/mobile_enabled", id);
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

        public static async Task<TeslaOAuthResponse> Authenticate(string email, string pwd)
        {
            TeslaOAuthResponse teslaOauthResponse = null;
            try
            {
                Uri url = new Uri("https://owner-api.teslamotors.com/oauth/token?grant_type=password");

                var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");

                Configuration = builder.Build();

                string client_id = Configuration.GetSection("TeslaSettings:client_id").Value;
                string client_secret = Configuration.GetSection("TeslaSettings:client_secret").Value;

                TeslaOAuth oAuth = new TeslaOAuth()
                {
                    grant_type = "password",
                    client_id = client_id,
                    client_secret = client_secret,
                    email = email,
                    password = pwd
                };

                SetupHeaders();

                StringContent stringContent = new StringContent(JsonConvert.SerializeObject((object)oAuth), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(url, stringContent);

                if (response.IsSuccessStatusCode)
                {
                    teslaOauthResponse = await response.Content.ReadAsAsync<TeslaOAuthResponse>().ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                throw;
            }

            return teslaOauthResponse;
        }

        private static void SetupHeaders(string token=null)
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            if (token != null)
            {
                string bearer = string.Format("bearer {0}", token);
                client.DefaultRequestHeaders.Add("Authorization", bearer);
            }
            client.DefaultRequestHeaders.Remove("User-Agent");
            client.DefaultRequestHeaders.Add("User-Agent", "tsla.works");
        }

        private static async Task<bool> ApiPost(string token, string url)
        {
            SetupHeaders(token);

            Uri uri = new Uri(url);

            bool success = false;
            HttpResponseMessage response = await client.PostAsync(uri, (HttpContent)null);
            if (response.IsSuccessStatusCode)
            {
                success = true;
            }

            return success;
        }
    }
}
