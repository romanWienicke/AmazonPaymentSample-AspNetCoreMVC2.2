using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AmazonPaymentSample.Models
{
    public class AmazonPayCredentials
    {
        public string Merchant_Id { get; set; }
        public string Access_Key { get; set; }
        public string Secret_Key { get; set; }
        public string Client_Id { get; set; }

        public string Client_Secret { get; set; }

        public string Region { get; set; }
        public string CurrencyCode { get; set; }
        public bool UseSandbox { get; set; }




    }
}
