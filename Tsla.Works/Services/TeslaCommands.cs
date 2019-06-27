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

        #region Get Requests
        public async Task<Vehicles> GetVehicles()
        {
            Vehicles vehicles = null;

            string url = "https://owner-api.teslamotors.com/api/1/vehicles/";

            HttpResponseMessage response = await ApiGet(url);

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

        public TeslaState WakeUp(string id)
        {
            Id = id;

            bool wakeSent = false;
            Tuple<bool, TeslaState> result = Task.Run<Tuple<bool, TeslaState>>(async () => await IsAwake(id)).Result;
            while (!result.Item1)
            {
                if (!wakeSent)
                {
                    Task.Run(async () => await Wake(id));
                }

                result = Task.Run<Tuple<bool, TeslaState>>(async () => await IsAwake(id)).Result;
            }

            return result.Item2;
        }

        public async Task<Tuple<bool, TeslaState>> IsAwake(string id)
        {
            bool awake = false;

            Id = id;

            TeslaState state = await GetState(id);

            if (state != null)
            {
                awake = state.response.state == "online" ? true : false;

            }
            else
            {
                TimeSpan timeSpan = new TimeSpan(0, 0, 0, 5);
                await Task.Delay(timeSpan);
            }
            Tuple<bool, TeslaState> tuple = Tuple.Create(awake, state);

            return tuple;
        }

        public TeslaLocation GetLocation(string id)
        {
            Id = id;

            TeslaLocation teslaLocation = null;

            TeslaState teslaState = WakeUp(id);

            teslaLocation = new TeslaLocation()
            {
                Latitude = teslaState.response.drive_state.latitude,
                Longitude = teslaState.response.drive_state.longitude
            };

            return teslaLocation;
        }
        private async Task<TeslaState> GetState(string id)
        {
            Id = id;

            //DONT USE WAKE COMMAND HERE
            TeslaState state = null;
            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/data", id);
            
            HttpResponseMessage response = await ApiGet(url);
            if (response.IsSuccessStatusCode)
            {
                state = await response.Content.ReadAsAsync<TeslaState>().ConfigureAwait(false);
            }

            return state;
        }

        public async Task<bool> MobileEnabled(string id)
        {
            Id = id;

            bool awake = false;
            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/mobile_enabled", id);

            HttpResponseMessage response = await ApiGet(url);
            if (response.IsSuccessStatusCode)
            {
                MobileEnabled mobile = await response.Content.ReadAsAsync<MobileEnabled>().ConfigureAwait(false);
                awake = mobile.response;
                mobile = (MobileEnabled)null;
            }
            return awake;
        }

        private async Task<HttpResponseMessage> ApiGet(string url)
        {
            SetupHeaders();

            Uri uri = new Uri(url);
            HttpResponseMessage response = await client.GetAsync(url);

            return response;
        }
        #endregion //Get Requests

        #region Post Requests
        public async Task<bool> Wake(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/wake_up", id);
            return await PostCommand(url);
        }

        public async Task<bool> StopCharging(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_stop", id);
            
            return await PostCommandAwake(url);
        }

        public async Task<bool> StartCharging(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_start", id);

            return await PostCommandAwake(url);
        }

        public async Task<bool> Unlock(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/door_unlock", id);

            return await PostCommandAwake(url);
        }

        public async Task<bool> Lock(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/door_lock", Id);

            return await PostCommandAwake(url);
        }

        public async Task<bool> Honk(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/honk_horn", Id);

            return await PostCommandAwake(url);
        }

        public async Task<bool> FlashLight(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/flash_lights", Id);

            return await PostCommandAwake(url);
        }

        public async Task<bool> OpenChargePort(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_port_door_open", Id);

            return await PostCommandAwake(url);
        }

        public async Task<bool> CloseChargePort(string id)
        {
            Id = id;

            string url = string.Format("https://owner-api.teslamotors.com/api/1/vehicles/{0}/command/charge_port_door_close", Id);

            return await PostCommandAwake(url);
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

            return await PostCommandAwake(url, stringContent);
        }

        public async Task<bool> PostCommandAwake(string url , HttpContent httpContent=null)
        {
            WakeUp(Id);

            return await PostCommand(url, httpContent);
        }

        public async Task<bool> PostCommand(string url, HttpContent httpContent=null)
        {
            HttpResponseMessage httpResponse = await ApiPost(url, httpContent);

            bool success = false;

            if (httpResponse.IsSuccessStatusCode)
            {
                success = true;
            }

            return success;
        }

        private async Task<HttpResponseMessage> ApiPost(string url, HttpContent httpContent = null)
        {
            SetupHeaders();

            Uri uri = new Uri(url);
            HttpResponseMessage response = await client.PostAsync(uri, httpContent);

            return response;
        }

        public async Task<TeslaOAuthResponse> Authenticate(string email, string pwd)
        {
            TeslaOAuthResponse teslaOauthResponse = null;
            try
            {
                string url = "https://owner-api.teslamotors.com/oauth/token?grant_type=password";

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

                StringContent stringContent = new StringContent(JsonConvert.SerializeObject((object)oAuth), Encoding.UTF8, "application/json");
                HttpResponseMessage response = await ApiPost(url, stringContent);

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
        #endregion //Post Requests

        private void GetConfig()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.Justin.json");

            configuration = builder.Build();
        }

        private void SetupHeaders()
        {
            client.DefaultRequestHeaders.Clear();
            //client.DefaultRequestHeaders.Remove("Authorization");
            if (Token != null)
            {
                string bearer = string.Format("bearer {0}", Token);
                client.DefaultRequestHeaders.Add("Authorization", bearer);
            }
            client.DefaultRequestHeaders.Remove("User-Agent");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:64.0) Gecko/20100101 Firefox/64.0");
        }

        
    }
}
