using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tsla.Works.Models;
using Tsla.Works.Models.AccountViewModels;
using Tsla.Works.Services;

namespace Tsla.Works.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class AccountController : Controller
    {
        private static HttpClient client = new HttpClient();
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        private Microsoft.AspNetCore.Identity.SignInResult AuthenticateTeslaAccount(string email, string pwd)
        {
            Microsoft.AspNetCore.Identity.SignInResult signInResult = new Microsoft.AspNetCore.Identity.SignInResult();
            
            string str = (string)null;
            string content = string.Format(@"{
                                'grant_type': 'password',
                                'client_id': '81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384',
                                'client_secret': 'c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3',
                                'email': '{0}',
                                'password': '{1}'
                            }", email, pwd);
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "tsla.works");
                httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
                StringContent stringContent = new StringContent(content, Encoding.UTF8, "application/json");
                HttpResponseMessage result1 = httpClient.PostAsync("https://owner-api.teslamotors.com/oauth/token?grant_type=password", (HttpContent)stringContent).Result;
                if (result1.IsSuccessStatusCode)
                {
                    str = result1.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    string result2 = result1.Content.ReadAsStringAsync().Result;
                    throw new ApplicationException(string.Format("Connection Exception: {0}: {1}", (object)result1.StatusCode, (object)result2));
                }
            }
            return signInResult;
        }

        private static async Task<bool> ApiPost(Uri url)
        {
            bool success = false;
            HttpResponseMessage response = await AccountController.client.PostAsync(url, (HttpContent)null);
            if (response.IsSuccessStatusCode)
                success = true;
            return success;
        }

        private static async Task<TeslaOAuthResponse> ApiPost(
          Uri url,
          HttpContent content)
        {
            TeslaOAuthResponse oauth = (TeslaOAuthResponse)null;
            try
            {
                AccountController.client.DefaultRequestHeaders.Add("User-Agent", "Tsla.works");
                AccountController.client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
                HttpResponseMessage response = await AccountController.client.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    oauth = await response.Content.ReadAsAsync<TeslaOAuthResponse>().ConfigureAwait(false);
                    response = (HttpResponseMessage)null;
                }
                else
                {
                    string msg = string.Format("Uri: {0} \n\n Status Code: {1}\n", (object)response.RequestMessage.RequestUri, (object)response.StatusCode);
                    throw new HttpRequestException(msg);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return oauth;
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                TeslaCommands teslaCommands = new TeslaCommands();
                var result = await teslaCommands.Authenticate(model.Email, model.Password);// Task.Run<TeslaOAuthResponse>(async () => await Authenticate()).Result;

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                if (result.access_token.Length > 0)
                {
                    Microsoft.AspNetCore.Http.CookieOptions options = new Microsoft.AspNetCore.Http.CookieOptions
                    {
                        Expires = DateTimeOffset.Now.AddDays(1),
                        Secure = true,
                        HttpOnly = true
                    };

                    string cookie = JsonConvert.SerializeObject(result);
                    cookie = Encryption.Encrypt(cookie, teslaCommands.Configuration.GetSection("TeslaSettings:encryption_key").Value);                    
                    Response.Cookies.Append("tsla.works", cookie, options);
                    _logger.LogInformation("User logged in.");
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                // For more information on how to enable account confirmation and password reset please
                // visit https://go.microsoft.com/fwlink/?LinkID=532713
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);
                await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                   $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }


        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }

        #endregion
    }
}
