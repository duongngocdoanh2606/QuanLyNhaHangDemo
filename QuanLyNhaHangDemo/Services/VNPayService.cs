using System.Security.Cryptography;
using System.Text;
using System.Net;

namespace QuanLyNhaHangDemo.Services
{
    public class VNPayOptions
    {
        public string TmnCode { get; set; } = "";
        public string HashSecret { get; set; } = "";
        public string Url { get; set; } = "";
        public string ReturnUrl { get; set; } = "";
        public string IpnUrl { get; set; } = "";
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = System.Globalization.CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, System.Globalization.CompareOptions.Ordinal);
        }
    }

    public class VNPayService
    {
        private readonly VNPayOptions _opts;

        public VNPayService(IConfiguration configuration)
        {
            _opts = configuration
                .GetSection("VNPay")
                .Get<VNPayOptions>() ?? new VNPayOptions();
        }

        public string CreatePaymentUrl(string ipAddress, string referenceNumber, long amountVnd)
        {
            var vnNow = DateTime.UtcNow.AddHours(7);

            var vnpayData = new SortedList<string, string>(new VnPayCompare());
            vnpayData.Add("vnp_Version", "2.1.0");
            vnpayData.Add("vnp_Command", "pay");
            vnpayData.Add("vnp_TmnCode", _opts.TmnCode);
            vnpayData.Add("vnp_Amount", (amountVnd * 100).ToString());
            vnpayData.Add("vnp_CreateDate", vnNow.ToString("yyyyMMddHHmmss"));
            vnpayData.Add("vnp_ExpireDate", vnNow.AddMinutes(15).ToString("yyyyMMddHHmmss"));
            vnpayData.Add("vnp_CurrCode", "VND");
            vnpayData.Add("vnp_IpAddr", ipAddress);
            vnpayData.Add("vnp_Locale", "vn");
            vnpayData.Add("vnp_OrderInfo", "Thanh toan don hang " + referenceNumber);
            vnpayData.Add("vnp_OrderType", "other");
            vnpayData.Add("vnp_ReturnUrl", _opts.ReturnUrl);
            vnpayData.Add("vnp_TxnRef", referenceNumber);

            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            string querystring = data.ToString();
            if (querystring.Length > 0)
            {
                querystring = querystring.Remove(querystring.Length - 1, 1);
            }

            var secureHash = ComputeHmacSha512(querystring, _opts.HashSecret);

            return $"{_opts.Url}?{querystring}&vnp_SecureHash={secureHash}";
        }

        public bool ValidateSignature(IQueryCollection query)
        {
            var vnpayData = new SortedList<string, string>(new VnPayCompare());
            string vnpSecureHash = "";

            foreach (var key in query.Keys)
            {
                if (key == "vnp_SecureHash" || key == "vnp_SecureHashType")
                {
                    if (key == "vnp_SecureHash")
                        vnpSecureHash = query[key].ToString();
                    continue;
                }
                if (key.StartsWith("vnp_"))
                    vnpayData.Add(key, query[key].ToString());
            }

            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in vnpayData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }
            string signData = data.ToString();
            if (signData.Length > 0)
            {
                signData = signData.Remove(signData.Length - 1, 1);
            }

            var calculatedHash = ComputeHmacSha512(signData, _opts.HashSecret);

            return calculatedHash.Equals(vnpSecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha512(string data, string key)
        {
            byte[] keyBytes  = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(dataBytes);
                return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
            }
        }
    }
}
