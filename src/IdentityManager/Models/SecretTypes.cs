namespace IdentityManager.Models
{
    public enum SecretTypes
    {
        SharedSecret,
        X509Thumbprint,
        X509Name,
        X509CertificateBase64,
        JWK
    }
}
