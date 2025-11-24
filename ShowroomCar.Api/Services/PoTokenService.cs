using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace ShowroomCar.Api.Services
{
    public class PoTokenService
    {
        private readonly string _secret;

        public PoTokenService(IConfiguration config)
        {
            _secret = config["Mail:TokenSecret"] 
                      ?? throw new Exception("Mail:TokenSecret missing");
        }

        public string Generate(long poId)
        {
            var exp = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
            var raw = $"{poId}:{exp}";
            var sig = Sign(raw);

            return Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{raw}:{sig}")
            );
        }

        public bool Validate(string token, out long poId)
        {
            poId = 0;

            try
            {
                var decoded = Encoding.UTF8.GetString(
                    Convert.FromBase64String(token)
                );

                var parts = decoded.Split(':');
                if (parts.Length != 3) return false;

                poId = long.Parse(parts[0]);
                var exp = long.Parse(parts[1]);
                var sig = parts[2];

                var raw = $"{poId}:{exp}";
                var expectedSig = Sign(raw);

                if (sig != expectedSig) return false;
                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private string Sign(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
        }
    }
}
