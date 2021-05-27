namespace Authentication.Models
{
    /// <summary>
    /// Base model for email message models
    /// </summary>
    public class EmailMessageModel
    {
        /// <summary>
        /// Email receipient
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; }
    }
}
