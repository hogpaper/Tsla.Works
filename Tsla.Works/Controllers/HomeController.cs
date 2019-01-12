﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Tsla.Works.Models;
using Tsla.Works.Services;

namespace Tsla.Works.Controllers
{
    public class HomeController : Controller
    {
        private TeslaOAuthResponse authResponse;
        TeslaOAuthResponse AuthResponse {
            get
            {
                if (authResponse == null)
                {
                    authResponse =  CookieCheck();
                }

                return authResponse;
            }
        }

        public TeslaCommands teslaCommands;
        TeslaCommands TeslaCommands {
            get
            {
                if (teslaCommands == null)
                {
                    teslaCommands = new TeslaCommands
                    {
                        Token = AuthResponse.access_token
                    };
                }

                return teslaCommands;
            }
        }

        public IActionResult Index()
        {
            Vehicles vehicles = null;
            
            if (AuthResponse != null)
            {
                vehicles = Task.Run<Vehicles>(async () => await TeslaCommands.GetVehicles()).Result;
            }
            else
            {
                return RedirectToAction("Login", "Account");
                //result = Task.Run<TeslaOAuthResponse>(async () => await AccountController.Authenticate()).Result;
                //string cookie = JsonConvert.SerializeObject(result);
                //Response.Cookies.Append("tsla.works", cookie);
            }            

            return View(vehicles);
        }

        private TeslaOAuthResponse CookieCheck()
        {
            if (Request.Cookies["tsla.works"] != null)
            {
                string cookievalue = Request.Cookies["tsla.works"];
                authResponse = JsonConvert.DeserializeObject<TeslaOAuthResponse>(cookievalue);
                TeslaCommands.Token = authResponse.access_token;
            }
            else
            {
                //RedirectToAction("Login", "Account");
            }

            return authResponse;
        }

        [HttpPost]
        public JsonResult Wake(string id)
        {
            bool result = Task.Run<bool>(async () => await TeslaCommands.Wake(id)).Result;

            JsonResult json = new JsonResult(result);

            return json;
        }

        [HttpPost]
        public JsonResult StopCharging(string id)
        {
            bool result = Task.Run<bool>(async () => await TeslaCommands.StopCharging(id)).Result;

            JsonResult json = new JsonResult(result);

            return json;
        }

        [HttpPost]
        public JsonResult StartCharging(string id)
        {
            bool result = Task.Run<bool>(async () => await TeslaCommands.StartCharging(id)).Result;

            JsonResult json = new JsonResult(result);

            return json;
        }

        [HttpPost]
        public JsonResult Unlock(string id)
        {
            bool result = Task.Run<bool>(async () => await TeslaCommands.Unlock(id)).Result;

            JsonResult json = new JsonResult(result);

            return json;
        }

        [HttpPost]
        public JsonResult Lock(string id)
        {
            bool result = Task.Run<bool>(async () => await TeslaCommands.Lock(id)).Result;

            JsonResult json = new JsonResult(result);

            return json;
        }

        [HttpPost]
        public JsonResult IsAwake(string id)
        {
            bool result = Task.Run<bool>(async () => await TeslaCommands.IsAwake(id)).Result;

            JsonResult json = new JsonResult(result);

            return json;
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Contact";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
