namespace BeyondAuth.Acl
{
    public static class AclClaimsMap
    {
        /// <summary>
        /// Name of the cliam containing the user's IdP
        /// </summary>
        public static string IdP { get; set; } = "iss";

        /// <summary>
        /// Name of the claim containing the user's id
        /// </summary>
        public static string UserId { get; set; } = "sub";

        /// <summary>
        /// Name of the claim containing the user's groups
        /// </summary>
        public static string Groups { get; set; } = "group";

        /// <summary>
        /// Name of the claim containing the client_id the request is made through
        /// </summary>
        public static string ClientId { get; set; } = "client_id";
    }
}
