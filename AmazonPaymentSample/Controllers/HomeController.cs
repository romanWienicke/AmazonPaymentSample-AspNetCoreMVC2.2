using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AmazonPaymentSample.Models;
using Microsoft.Extensions.Options;
using AmazonPay.CommonRequests;
using AmazonPay;
using Microsoft.AspNetCore.Http;
using AmazonPay.StandardPaymentRequests;
using AmazonPay.Responses;
using System.Globalization;
using Newtonsoft.Json;

namespace AmazonPaymentSample.Controllers
{
    public class HomeController : Controller
    {
        const string AmazonClientSessionKey = nameof(AmazonClientSessionKey);
        const string AmazonOrderReferenceId = nameof(AmazonOrderReferenceId);
        const string AmazonOrderAmount = nameof(AmazonOrderAmount);

        private readonly AmazonPayCredentials _config;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private IList<string> amazonCaptureIdList = new List<string>();


        public HomeController(IOptions<AmazonPayCredentials> amazonOptions, IHttpContextAccessor httpContextAccessor)
        {
            _config = amazonOptions.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult SetPaymentDetails()
        {


            return View();
        }

        private Client _client;
        private Client AmazonClient
        {
            get
            {
                if (_client == null)
                {
                    var clientConfig = new Configuration();

                    var region = (Regions.supportedRegions)Enum.Parse(typeof(Regions.supportedRegions), _config.Region);

                    clientConfig.WithAccessKey(_config.Access_Key)
                        .WithSecretKey(_config.Secret_Key)
                        .WithMerchantId(_config.Merchant_Id)
                        .WithClientId(_config.Client_Id)
                        .WithSandbox(true)
                        .WithRegion(region);

                    _client = new Client(clientConfig);
                }
                return _client;

            }
        }

        [HttpPost]
        public IActionResult GetDetails(string orderReferenceId, string amount, string addressConsentToken = null)
        {
            var currencyCode = (Regions.currencyCode)Enum.Parse(typeof(Regions.currencyCode), _config.CurrencyCode);
            var amountValue = decimal.Parse(amount, CultureInfo.InvariantCulture);


            SetOrderReferenceDetailsRequest setRequestParameters = new SetOrderReferenceDetailsRequest();
            setRequestParameters.WithAmazonOrderReferenceId(orderReferenceId)
                .WithAmount(amountValue)
                .WithCurrencyCode(currencyCode)
                .WithSellerNote("testzahlung")
                .WithStoreName("AmzonPaymentSamply by Roman Wienicke")
                .WithCustomInformation($"{DateTime.Now}")
                .WithSellerOrderId($"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}")
                .WithSellerNote("Note");
            var setResponse = AmazonClient.SetOrderReferenceDetails(setRequestParameters);

            var response = setResponse.GetJson();

            if (setResponse.GetSuccess())
            {
                var session = _httpContextAccessor.HttpContext.Session;
                session.Set(AmazonOrderReferenceId, orderReferenceId);
                session.Set(AmazonOrderAmount, amountValue);

                GetOrderReferenceDetailsRequest getRequestParameters = new GetOrderReferenceDetailsRequest();
                getRequestParameters
                    .WithAccessToken(addressConsentToken)
                    .WithAmazonOrderReferenceId(orderReferenceId);

                OrderReferenceDetailsResponse getOrderReferenceDetailsResponse = AmazonClient.GetOrderReferenceDetails(getRequestParameters);
                response = getOrderReferenceDetailsResponse.GetJson();
            }

            return Json(response);
        }
        public IActionResult ConfirmPaymentAndAuthorize()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ConfirmAndAuthorize()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var orderReferenceId = session.Get<string>(AmazonOrderReferenceId);
            var amount = session.Get<decimal>(AmazonOrderAmount);

            var getRequestParameters = new ConfirmOrderReferenceRequest();
            getRequestParameters.WithAmazonOrderReferenceId(orderReferenceId);



            var response = (IResponse)AmazonClient.ConfirmOrderReference(getRequestParameters);

            if (response.GetSuccess())
            {
                var uniqueReferenceId = GenerateRandomUniqueString();
                var currencyCode = (Regions.currencyCode)Enum.Parse(typeof(Regions.currencyCode), _config.CurrencyCode);

                AuthorizeRequest authRequestParameters = new AuthorizeRequest();
                authRequestParameters.WithAmazonOrderReferenceId(orderReferenceId)
                    .WithAmount(amount)
                    .WithCurrencyCode(currencyCode)
                    .WithAuthorizationReferenceId(uniqueReferenceId)
                    .WithTransactionTimeout(0)
                    .WithCaptureNow(true)
                    .WithSellerAuthorizationNote("Note");

                response = AmazonClient.Authorize(authRequestParameters);

                var data = AmazonClient.GetOrderReferenceDetails(
                    new GetOrderReferenceDetailsRequest ()
                    .WithAmazonOrderReferenceId(orderReferenceId)
                    );

            }
            return Json(JsonConvert.DeserializeObject<dynamic>(response.GetJson()));
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public string GenerateRandomUniqueString()
        {
            Guid g = Guid.NewGuid();
            var GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            GuidString = GuidString.Replace("/", "");
            return GuidString;
        }
    }
}
