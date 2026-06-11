using System.Security.Cryptography;
using System.Text;

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
            var vnpayData = new Dictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _opts.TmnCode },
                { "vnp_Amount", (amountVnd * 100).ToString() },
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Thanh toan don hang {referenceNumber}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", _opts.ReturnUrl },
                { "vnp_TxnRef", referenceNumber }
            };

            // Build query string
            var signData = string.Join("&", vnpayData
                .OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

            // Calculate Hash
            var vnpSecureHash = ComputeHmacSha512(signData, _opts.HashSecret);

            return $"{_opts.Url}?{signData}&vnp_SecureHash={vnpSecureHash}";
        }

        public bool ValidateSignature(IQueryCollection query)
        {
            var vnpayData = new Dictionary<string, string>();
            string vnp_SecureHash = "";

            foreach (var key in query.Keys)
            {
                var value = query[key].ToString();
                if (key.StartsWith("vnp_"))
                {
                    if (key == "vnp_SecureHash")
                    {
                        vnp_SecureHash = value;
                    }
                    else
                    {
                        vnpayData.Add(key, value);
                    }
                }
            }

            var signData = string.Join("&", vnpayData
                .OrderBy(kv => kv.Key)
                .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

            var calculatedHash = ComputeHmacSha512(signData, _opts.HashSecret);

            return calculatedHash.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha512(string data, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA512(keyBytes);
            byte[] hash = hmac.ComputeHash(dataBytes);
            return Convert.ToHexString(hash).ToLower();
        }
    }
}
