using System;
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
        public IActionResult Index()
        {
            Vehicles vehicles = null;
            
            if (Request.Cookies["tsla.works"] != null)
            {
                string cookievalue = Request.Cookies["tsla.works"];
                TeslaOAuthResponse response = JsonConvert.DeserializeObject<TeslaOAuthResponse>(cookievalue);

                vehicles = Task.Run<Vehicles>(async () => await TeslaCommands.GetVehicles(response.access_token)).Result;
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
            TeslaOAuthResponse authResponse = null;
            if (Request.Cookies["tsla.works"] != null)
            {
                string cookievalue = Request.Cookies["tsla.works"];
                authResponse = JsonConvert.DeserializeObject<TeslaOAuthResponse>(cookievalue);
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
            TeslaOAuthResponse authResponse = CookieCheck();

            bool result = Task.Run<bool>(async () => await TeslaCommands.Wake(authResponse.access_token, id)).Result;

            JsonResult json = new JsonResult(result);

            return json;
        }

        [HttpPost]
        public JsonResult IsAwake(string id)
        {
            TeslaOAuthResponse authResponse = CookieCheck();

            bool result = Task.Run<bool>(async () => await TeslaCommands.IsAwake(authResponse.access_token, id)).Result;

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
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
