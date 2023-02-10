namespace AuthorizationServer.Enrollment
{
    public class EnrollmentRequest
    {
        /// <summary>
        /// BeyondAuth Server Key
        /// </summary>
        public string ServerKey { get; set; }

        /// <summary>
        /// Public key of certificate in base64 format
        /// </summary>
        public string Certificate { get; set; }
        
        /// <summary>
        /// Random string
        /// </summary>
        public string Nonce { get; set; }

        /// <summary>
        /// Time of request
        /// </summary>
        public long Timestamp { get; set; }
    }
}
