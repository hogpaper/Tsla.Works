﻿using Microsoft.Extensions.Configuration;
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
        public IConfigurationRoot Configuration { get; set; }
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
            SetupHeaders();

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

        public async Task<bool> Command(string url)
        {
            WakeUp(Id);

            return await ApiPost(url);
        }

        private void WakeUp(string id)
        {
            SetupHeaders();
            while (!Task.Run<bool>(async () => await IsAwake(id)).Result)
            {
                Task.Run(async () => await Wake(id));
            }
        }

        public async Task<bool> IsAwake(string id)
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

        private async Task<bool> ApiPost(string url)
        {
            SetupHeaders();

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
