namespace Authentication.Models.Messages
{
    /// <summary>
    /// Information for reset password email
    /// </summary>
    public class ResetPasswordEmailMessage : EmailMessageModel
    {
        /// <summary>
        /// The link for the user to click to reset his password
        /// </summary>
        public string CallbackUrl { get; set; }

        public string SupportEmail { get; set; }
    }
}
