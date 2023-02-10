using System.Security.Cryptography.X509Certificates;

namespace AuthorizationServer.Enrollment
{
    public interface IAuthorizationServerEnrollmentService
    {
        Task<bool> IsEnrolled();

        Task Enroll(string tenantId, string serverKey, X509Certificate2 certificate);
    }
}
