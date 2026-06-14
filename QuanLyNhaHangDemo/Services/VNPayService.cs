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

        /// <summary>
        /// Tạo URL thanh toán VNPay đúng chuẩn:
        /// 1. Sort key A-Z
        /// 2. Build chuỗi ký RAW (key=value, không encode)
        /// 3. HMAC-SHA512 chuỗi raw
        /// 4. Build URL cuối có encode value
        /// </summary>
        public string CreatePaymentUrl(string ipAddress, string referenceNumber, long amountVnd)
        {
            // Đảm bảo thời gian đúng múi giờ Việt Nam UTC+7
            var vnNow = DateTime.UtcNow.AddHours(7);

            var vnpayData = new SortedDictionary<string, string>
            {
                { "vnp_Version",    "2.1.0" },
                { "vnp_Command",    "pay" },
                { "vnp_TmnCode",    _opts.TmnCode },
                { "vnp_Amount",     (amountVnd * 100).ToString() },
                { "vnp_CreateDate", vnNow.ToString("yyyyMMddHHmmss") },
                { "vnp_ExpireDate", vnNow.AddMinutes(15).ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode",   "VND" },
                { "vnp_IpAddr",     ipAddress },
                { "vnp_Locale",     "vn" },
                { "vnp_OrderInfo",  $"Thanh toan don hang {referenceNumber}" },
                { "vnp_OrderType",  "other" },
                { "vnp_ReturnUrl",  _opts.ReturnUrl },
                { "vnp_TxnRef",     referenceNumber }
            };

            // Bước 1: Chuỗi ký RAW – key=value&key=value (KHÔNG encode)
            var rawData = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={kv.Value}"));

            // Bước 2: Tính HMAC-SHA512 trên chuỗi raw
            var secureHash = ComputeHmacSha512(rawData, _opts.HashSecret);

            // Bước 3: Build URL cuối – value cần encode (chuẩn RFC 3986 %20)
            var queryString = string.Join("&", vnpayData
                .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

            return $"{_opts.Url}?{queryString}&vnp_SecureHash={secureHash}";
        }

        /// <summary>
        /// Xác thực chữ ký IPN / Return URL từ VNPay.
        /// Loại bỏ vnp_SecureHash và vnp_SecureHashType, sort còn lại,
        /// build chuỗi ký RAW (không encode), so sánh hash.
        /// </summary>
        public bool ValidateSignature(IQueryCollection query)
        {
            var vnpayData = new SortedDictionary<string, string>();
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
                    vnpayData[key] = query[key].ToString();
            }

            // Chuỗi ký RAW – KHÔNG encode (giống bên tạo URL)
            var rawData = string.Join("&", vnpayData.Select(kv => $"{kv.Key}={kv.Value}"));
            var calculatedHash = ComputeHmacSha512(rawData, _opts.HashSecret);

            return calculatedHash.Equals(vnpSecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string ComputeHmacSha512(string data, string key)
        {
            byte[] keyBytes  = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac   = new HMACSHA512(keyBytes);
            return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLower();
        }
    }
}
