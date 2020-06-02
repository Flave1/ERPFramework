﻿using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System; 
using System.Text;
using System.Threading.Tasks; 
using Polly.Retry;
using Polly;
using System.Net.Http;
using Newtonsoft.Json; 
using System.Net.Http.Headers; 
using Microsoft.AspNetCore.Http; 
using Puchase_and_payables.AuthHandler;
using GOSLibraries.GOS_Financial_Identity;
using GOSLibraries.Options;
using Puchase_and_payables.Data;
using GOSLibraries.GOS_Error_logger.Service;
using Puchase_and_payables.Contracts.V1;
using GOSLibraries.GOS_API_Response;
using Puchase_and_payables.Contracts.Response;
using GOSLibraries;

namespace App.AuthHandler.Inplimentation
{
    public class IdentityService : IIdentityService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JwtSettings _jwtSettings;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly DataContext _dataContext;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILoggerService _logger; 
        private readonly AsyncRetryPolicy _retryPolicy;
        private const int maxRetryTimes = 4;
        private readonly IHttpClientFactory _httpClientFactory;
        private AuthenticationResult _authResponse = null;
        public IdentityService(IHttpContextAccessor httpContextAccessor, JwtSettings jwtSettings, TokenValidationParameters tokenValidationParameters,
            DataContext dataContext, RoleManager<IdentityRole> roleManager, ILoggerService loggerService, IHttpClientFactory httpClientFactory)
        {
            _jwtSettings = jwtSettings;
            _httpClientFactory = httpClientFactory;
            _tokenValidationParameters = tokenValidationParameters;
            _dataContext = dataContext;
            _roleManager = roleManager;
            _logger = loggerService;
            _httpContextAccessor = httpContextAccessor;
            _retryPolicy = Policy.Handle<HttpRequestException>()

                .WaitAndRetryAsync(maxRetryTimes, times =>

                TimeSpan.FromSeconds(times * 2));
        }

        public async Task<AuthenticationResult> LoginAsync(string userName, string password)
        {
            try
            {

                var loginRquest = new UserLoginReqObj
                {
                    UserName = userName,
                    Password = password,
                };
                var gosGatewayClient = _httpClientFactory.CreateClient("GOSDEFAULTGATEWAY");

                var jsonContent = JsonConvert.SerializeObject(loginRquest);
                var buffer = Encoding.UTF8.GetBytes(jsonContent);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var result = await gosGatewayClient.PostAsync(ApiRoutes.Identity.LOGIN, byteContent);

                var accountInfo = await result.Content.ReadAsStringAsync();
                _authResponse = JsonConvert.DeserializeObject<AuthenticationResult>(accountInfo);
                if(_authResponse.Token != null)
                {

                    return new AuthenticationResult
                    {
                        Token = _authResponse.Token,
                        RefreshToken = _authResponse.RefreshToken
                    };
                }

                return new AuthenticationResult
                {
                    Status = new APIResponseStatus
                    {
                        IsSuccessful = _authResponse.Status.IsSuccessful,
                        Message = new APIResponseMessage
                        {
                            TechnicalMessage = _authResponse.Status?.Message?.TechnicalMessage,
                            FriendlyMessage = _authResponse.Status?.Message?.FriendlyMessage
                        }
                    }
                };
                


            }
            catch (Exception ex)
            {
                #region Log error 
                var errorCode = ErrorID.Generate(4);
                _logger.Error($"ErrorID : LoginAsync{errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}");
               
                return new AuthenticationResult
                {

                    Status = new APIResponseStatus
                    {
                        Message = new APIResponseMessage
                        {
                            FriendlyMessage = "Error occured!! Please tyr again later",
                            MessageId = errorCode,
                            TechnicalMessage = $"ErrorID : LoginAsync{errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}"
                        }
                    }
                };
                #endregion
            }
        }

        public async Task<UserDataResponseObj> UserDataAsync()
        {
            try
            {
                var currentUserId = _httpContextAccessor.HttpContext.User?.FindFirst(x => x.Type == "userId").Value;
                var gosGatewayClient = _httpClientFactory.CreateClient("GOSDEFAULTGATEWAY");
                string authorization = _httpContextAccessor.HttpContext.Request.Headers["Authorization"];

                if (string.IsNullOrEmpty(authorization))
                {
                    return new UserDataResponseObj
                    {
                        Status = new APIResponseStatus
                        {
                            IsSuccessful = false,
                            Message = new APIResponseMessage
                            {
                                FriendlyMessage = "Error Occurred ! Please Contact Systems Administrator"
                            }
                        }
                    };
                }
                gosGatewayClient.DefaultRequestHeaders.Add("Authorization", authorization);
                var result = await gosGatewayClient.GetAsync(ApiRoutes.Identity.FETCH_USERDETAILS);
                var accountInfo = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<UserDataResponseObj>(accountInfo);

            }
            catch (Exception ex)
            {
                #region Log error 
                var errorCode = ErrorID.Generate(4);
                _logger.Error($"ErrorID : LoginAsync{errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}");

                return new UserDataResponseObj
                {
                    Status = new APIResponseStatus
                    {
                        Message = new APIResponseMessage
                        {
                            FriendlyMessage = "Error occured!! Please tyr again later",
                            MessageId = errorCode,
                            TechnicalMessage = $"ErrorID : LoginAsync{errorCode} Ex : {ex?.Message ?? ex?.InnerException?.Message} ErrorStack : {ex?.StackTrace}"
                        }
                    }
                };
                #endregion
            }
        }
    }
}