using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;

namespace AuthorizationServer.Enrollment
{
    public class AuthorizationServerEnrollmentService : IAuthorizationServerEnrollmentService
    {
        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private bool _enrolled = false;

        public AuthorizationServerEnrollmentService(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
        {
            _logger = loggerFactory.CreateLogger<AuthorizationServerEnrollmentService>();
            _httpClientFactory = httpClientFactory;
        }

        public async Task Enroll(string tenantId, string serverKey, X509Certificate2 certificate)
        {
            while (true)
            {
                using var httpClient = _httpClientFactory.CreateClient("beyondauth");

                var enrollmentRequest = new EnrollmentRequest
                {
                    ServerKey = serverKey,
                    Certificate = Convert.ToBase64String(certificate.Export(X509ContentType.Cert)),
                    Nonce = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                };
                var serializedEnrollmentRequest = JsonConvert.SerializeObject(enrollmentRequest);
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"api/v1/{tenantId}/authorizatzion/enroll")
                {
                    Content = new StringContent(serializedEnrollmentRequest, Encoding.UTF8, "application/json")
                };

                var privateKey = certificate.GetECDsaPrivateKey();
                if (privateKey == null)
                    throw new Exception("Private key not found in certificate");
                
                var signature = privateKey.SignData(Encoding.ASCII.GetBytes(serializedEnrollmentRequest), HashAlgorithmName.SHA256);
                var authorizationHeader = $"SHA256withECDSA {signature}";

                requestMessage.Headers.Add("Authorization", authorizationHeader);

                var response = await httpClient.SendAsync(requestMessage);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _enrolled = true;
                    var responseString = await response.Content.ReadAsStringAsync();

                    break;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    _enrolled = false;
                    await Task.Delay(5000);
                }
                else
                {
                    await Task.Delay(5000);
                }
            }
        }

        public Task<bool> IsEnrolled()
        {
            return Task.FromResult(_enrolled);
        }
    }
}
