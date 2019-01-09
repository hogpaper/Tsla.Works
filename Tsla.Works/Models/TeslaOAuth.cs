using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tsla.Works.Models
{
    public class TeslaOAuth
    {
        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string email { get; set; }
        public string password { get; set; }
    }

    public class TeslaOAuthResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public string expires_in { get; set; }
        public string refresh_token { get; set; }
        public string created_at { get; set; }
    }
}
