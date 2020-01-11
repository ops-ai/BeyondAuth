namespace Blockchain
{
    public enum AuthorizationDecisions
    {
        /// <summary>
        /// Access to the resource was granted
        /// </summary>
        Granted,

        /// <summary>
        /// Access to the resource was rejected
        /// </summary>
        Rejected,

        /// <summary>
        /// Not enough information available to make decision, requesting more info
        /// </summary>
        MoreContextRequired
    }
}
