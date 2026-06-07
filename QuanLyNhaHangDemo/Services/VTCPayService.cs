using System.Security.Cryptography;
using System.Text;

namespace QuanLyNhaHangDemo.Services
{
    public class VTCPayOptions
    {
        public string WebsiteId { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public string CheckoutUrl { get; set; } = "";
        public string ReturnUrl { get; set; } = "";
        public string IpnUrl { get; set; } = "";
    }

    public class VTCPayService
    {
        private readonly VTCPayOptions _opts;

        public VTCPayService(IConfiguration configuration)
        {
            _opts = configuration
                .GetSection("VTCPay")
                .Get<VTCPayOptions>() ?? new VTCPayOptions();
        }

        /// <summary>
        /// Tạo URL checkout VTCPay với chữ ký HMAC-SHA512
        /// </summary>
        public string BuildCheckoutUrl(string referenceNumber, long amountVnd)
        {
            // Chuỗi ký theo tài liệu VTCPay:
            // website_id + amount + reference_number + secretKey
            string rawSign = $"{_opts.WebsiteId}{amountVnd}{referenceNumber}{_opts.SecretKey}";
            string signature = ComputeHmacSha512(rawSign, _opts.SecretKey);

            var query = new Dictionary<string, string>
            {
                ["website_id"]       = _opts.WebsiteId,
                ["amount"]           = amountVnd.ToString(),
                ["currency"]         = "VND",
                ["reference_number"] = referenceNumber,
                ["return_url"]       = _opts.ReturnUrl,
                ["ipn_url"]          = _opts.IpnUrl,
                ["signature"]        = signature
            };

            string queryString = string.Join("&",
                query.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

            return $"{_opts.CheckoutUrl}?{queryString}";
        }

        /// <summary>
        /// Xác thực chữ ký IPN callback từ VTCPay
        /// </summary>
        public bool VerifyIpnSignature(
            string websiteId,
            string referenceNumber,
            string amount,
            string status,
            string receivedSignature)
        {
            // Chuỗi xác thực theo tài liệu VTCPay:
            // website_id + reference_number + amount + status + secretKey
            string raw = $"{websiteId}{referenceNumber}{amount}{status}{_opts.SecretKey}";
            string expected = ComputeHmacSha512(raw, _opts.SecretKey);
            return string.Equals(expected, receivedSignature, StringComparison.OrdinalIgnoreCase);
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
