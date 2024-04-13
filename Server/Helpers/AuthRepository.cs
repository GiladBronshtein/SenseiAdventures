using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using SenseiAdventures.Server.Data;
using SenseiAdventures.Server.Models;

namespace SenseiAdventures.Server.Helpers
{
	public class AuthRepository
	{
        private readonly DbRepository _db;
        private readonly IConfiguration _config;
        private readonly IHttpContextAccessor _httpContext;
        private readonly TokenService _tokenService;
        private readonly HttpClient _http;

        public AuthRepository(DbRepository db, IConfiguration config, IHttpContextAccessor httpContext, TokenService tokenService)
        {
            _db = db;
            _config = config;
            _httpContext = httpContext;
            _tokenService = tokenService;
            _http = new HttpClient();
        }

        public async Task<KeyValuePair<string, string>> CheckAuth(string currentDomain)
        {
            var configData = _config.GetSection("AuthData");
            AuthData data = new AuthData()
            {
                SystemId = configData.GetValue("SystemId", 0),
                Url = configData.GetValue("Url", string.Empty),
                Domain = configData.GetValue("Domain", string.Empty)
            };

            if (data.SystemId == 0 || string.IsNullOrEmpty(data.Url))
            {
                return new KeyValuePair<string, string>("no data found", "");
            }



            if (!currentDomain.ToLower().Contains(data.Domain))
            {
                AuthUser devUser = new AuthUser() { FirstName = "בדיקה", LastName = "פורטלמ" };
                devUser.PortelemId = -1;
                int devUserId = await loginFunc(devUser);
                if (devUserId == 0)
                    return new KeyValuePair<string, string>("dev user insert fail", "");

                return new KeyValuePair<string, string>("user Id", devUserId.ToString());
            }

            var PortelemResponse = await _http.GetAsync(data.Url + "api/Services/status/" + data.SystemId);

            if (!PortelemResponse.IsSuccessStatusCode)
                return new KeyValuePair<string, string>("no service found", data.Url);


            string systemStatus = PortelemResponse.Content.ReadAsStringAsync().Result;

            if (systemStatus != "QA" && systemStatus != "Complete") //complete or QA - need cookie check
            {
                AuthUser devUser = new AuthUser() { FirstName = "בדיקה", LastName = "פורטלמ" };
                devUser.PortelemId = -1;
                int devUserId = await loginFunc(devUser);
                if (devUserId == 0)
                    return new KeyValuePair<string, string>("dev user insert fail", "");

                return new KeyValuePair<string, string>("user Id", devUserId.ToString());
            }

            var token = _httpContext.HttpContext.Request.Cookies["token"];
            if (token == null)
                return new KeyValuePair<string, string>("no token found", data.Url + "login/" + data.SystemId);

            IEnumerable<Claim> claims = ParseClaimsFromJwt(token);
            long exp = (long)Convert.ToDouble(claims.SingleOrDefault(c => c.Type == "exp").Value);
            long now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

            //if token is expired - redirect to portelem login
            if (exp <= now)
                return new KeyValuePair<string, string>("expired token", data.Url + "login/" + data.SystemId);


            int portelemId = Convert.ToInt32(claims.SingleOrDefault(c => c.Type == "nameid").Value);

            //sent Http request to the portelem -> check if the user is logged in to portelem
            HttpResponseMessage checkPortelem;
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, data.Url + "api/services/students/" + data.SystemId + "/" + portelemId))
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                checkPortelem = await _http.SendAsync(requestMessage);
            }

            //if Unauthorize/no user - redirect to portelem login
            if (!checkPortelem.IsSuccessStatusCode)
                return new KeyValuePair<string, string>("unauthorize user", data.Url + "login/" + data.SystemId);


            AuthUser user = checkPortelem.Content.ReadFromJsonAsync<AuthUser>().Result;
            int userId = await loginFunc(user);
            if (userId == 0)
                return new KeyValuePair<string, string>("new user insert fail", "");

            return new KeyValuePair<string, string>("user Id", userId.ToString());
        }

        private async Task<int> loginFunc(AuthUser user)
        {
            object getParam = new
            {
                portelemId = user.PortelemId
            };
            //Change base on your tables
            string getQuery = "SELECT ID FROM Users WHERE PortelemId = @portelemId";
            var getRecords = await _db.GetRecordsAsync<int>(getQuery, getParam);
            int userId = getRecords.FirstOrDefault();
            if (userId == 0)
            {
                string insertQuery = "INSERT INTO Users (FirstName, LastName, PortelemId) VALUES (@FirstName, @LastName, @PortelemId)";
                userId = await _db.InsertReturnIdAsync(insertQuery, user);
                if (userId == 0)
                    return 0;
            }

            string authToken = _tokenService.CreateToken(userId);

            _httpContext.HttpContext.Response.Cookies.Append("userToken", authToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.Now.AddHours(1)
            });

            return userId;
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];

            var jsonBytes = ParseBase64WithoutPadding(payload);

            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            ExtractRolesFromJWT(claims, keyValuePairs);

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));

            return claims;
        }

        private void ExtractRolesFromJWT(List<Claim> claims, Dictionary<string, object> keyValuePairs)
        {
            keyValuePairs.TryGetValue(ClaimTypes.Role, out object roles);
            if (roles != null)
            {
                var parsedRoles = roles.ToString().Trim().TrimStart('[').TrimEnd(']').Split(',');
                if (parsedRoles.Length > 1)
                {
                    foreach (var parsedRole in parsedRoles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, parsedRole.Trim('"')));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, parsedRoles[0]));
                }
                keyValuePairs.Remove(ClaimTypes.Role);
            }
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }


}

