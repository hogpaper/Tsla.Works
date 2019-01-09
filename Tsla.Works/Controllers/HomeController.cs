using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Tsla.Works.Models;

namespace Tsla.Works.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            TeslaOAuthResponse result = null;
            
            if (Request.Cookies["tsla.works"] != null)
            {
                string cookievalue = Request.Cookies["tsla.works"];
                TeslaOAuthResponse response = JsonConvert.DeserializeObject<TeslaOAuthResponse>(cookievalue);
            }
            else
            {
                result = Task.Run<TeslaOAuthResponse>(async () => await AccountController.Authenticate()).Result;
                string cookie = JsonConvert.SerializeObject(result);
                Response.Cookies.Append("tsla.works", cookie);
            }            

            return View();
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
        public JsonResult Wake()
        {
            TeslaOAuthResponse authResponse = CookieCheck();

            bool result = Task.Run<bool>(async () => await AccountController.Wake()).Result;

            JsonResult json = new JsonResult(null);

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
