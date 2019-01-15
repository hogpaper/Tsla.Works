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
        private HttpClient client = new HttpClient();
        IConfigurationRoot configuration;
        public IConfigurationRoot Configuration
        {
            get
            {
                if (configuration == null)
                {
                    GetConfig();
                }
                return configuration;
            }
        }
        private string Id;
        public string Token { get; set; }

        public async Task<Vehicles> GetVehicles()
        {
            Vehicles vehicles = null;

            Uri url = new Uri("https://owner-api.teslamotors.com/api/1/vehicles/");

            SetupHeaders();

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

        public async Task<bool> Wake(string id)
        {
            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/wake_up", id);
            return await ApiPost(url);
        }

        public async Task<bool> StopCharging(string id)
        {
            WakeUp(id);

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_stop", id);
            
            return await ApiPost(url);
        }

        public async Task<bool> StartCharging(string id)
        {
            WakeUp(id);

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_start", id);

            return await ApiPost(url);
        }

        public async Task<bool> Unlock(string id)
        {
            WakeUp(id);

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/door_unlock", id);

            return await ApiPost(url);
        }

        public async Task<bool> Lock(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/door_lock", Id);

            return await Command(url);
        }

        public async Task<bool> Honk(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/honk_horn", Id);

            return await Command(url);
        }

        public async Task<bool> FlashLight(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/flash_lights", Id);

            return await Command(url);
        }

        public async Task<bool> OpenChargePort(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_port_door_open", Id);

            return await Command(url);
        }

        public async Task<bool> CloseChargePort(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_port_door_close", Id);

            return await Command(url);
        }

        public async Task<bool> OpenTrunk(string id, string which_trunk)
        {
            Id = id;

            TrunkApi trunkApi = new TrunkApi()
            {
                which_trunk = which_trunk
            };

            StringContent stringContent = new StringContent(JsonConvert.SerializeObject((object)trunkApi), Encoding.UTF8, "application/json");

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/actuate_trunk", Id);

            return await Command(url, stringContent);
        }
        public async Task<bool> Command(string url, HttpContent httpContent=null)
        {
            WakeUp(Id);

            return await ApiPost(url, httpContent);
        }

        public TeslaState WakeUp(string id)
        {
            SetupHeaders();
            Task.Run(async () => await Wake(id));
            Tuple<bool, TeslaState> result = Task.Run<Tuple<bool, TeslaState>>(async () => await IsAwake(id)).Result;
            while (!result.Item1)
            {
                
            }

            return result.Item2;
        }

        public async Task<Tuple<bool, TeslaState>> IsAwake(string id)
        {
            bool awake = false;

            TeslaState state = await GetState(id);

            awake = state.response.state == "online" ? true : false;
            Tuple<bool, TeslaState> tuple = Tuple.Create(awake, state);

            return tuple;
        }

        public async Task<TeslaLocation> GetLocation(string id)
        {
            TeslaLocation teslaLocation = null;

            WakeUp(id);

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/data", id);

            Uri uri = new Uri(url);

            SetupHeaders();

            HttpResponseMessage response = await client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                TeslaState teslaState = await response.Content.ReadAsAsync<TeslaState>().ConfigureAwait(false);

                teslaLocation = new TeslaLocation()
                {
                    Latitude = teslaState.response.drive_state.latitude,
                    Longitude = teslaState.response.drive_state.longitude
                };

                response = (HttpResponseMessage)null;
            }
            else
            {
                string msg = string.Format("Uri: {0} \n\n Status Code: {1}\n", (object)response.RequestMessage.RequestUri, (object)response.StatusCode);
                throw new HttpRequestException(msg);
            }

            return teslaLocation;
        }
        private async Task<TeslaState> GetState(string id)
        {
            //DONT USE WAKE COMMAND HERE
            TeslaState state = null;
            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/data", id);
            Uri uri = new Uri(url);

            SetupHeaders();

            HttpResponseMessage response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                state = await response.Content.ReadAsAsync<TeslaState>().ConfigureAwait(false);
            }

            return state;
        }

        public async Task<bool> MobileEnabled(string id)
        {
            bool awake = false;
            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/mobile_enabled", id);
            Uri uri = new Uri(url);

            SetupHeaders();

            HttpResponseMessage response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                MobileEnabled mobile = await response.Content.ReadAsAsync<MobileEnabled>().ConfigureAwait(false);
                awake = mobile.response;
                mobile = (MobileEnabled)null;
            }
            return awake;
        }

        public async Task<TeslaOAuthResponse> Authenticate(string email, string pwd)
        {
            TeslaOAuthResponse teslaOauthResponse = null;
            try
            {
                Uri url = new Uri("https://owner-api.teslamotors.com/oauth/token?grant_type=password");
                
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

        private void GetConfig()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.Justin.json");

            configuration = builder.Build();
        }

        private void SetupHeaders()
        {
            client.DefaultRequestHeaders.Remove("Authorization");
            if (Token != null)
            {
                string bearer = string.Format("bearer {0}", Token);
                client.DefaultRequestHeaders.Add("Authorization", bearer);
            }
            client.DefaultRequestHeaders.Remove("User-Agent");
            client.DefaultRequestHeaders.Add("User-Agent", "tsla.works");
        }

        private async Task<bool> ApiPost(string url, HttpContent httpContent = null)
        {
            SetupHeaders();

            Uri uri = new Uri(url);

            bool success = false;
            HttpResponseMessage response = await client.PostAsync(uri, httpContent);
            if (response.IsSuccessStatusCode)
            {
                success = true;
            }

            return success;
        }
    }
}
