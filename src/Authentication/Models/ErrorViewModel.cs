using IdentityServer4.Models;

namespace Authentication.Models
{
    public class ErrorViewModel
    {
        public ErrorViewModel()
        {
        }

        public ErrorViewModel(string error) => Error = new ErrorMessage { Error = error };

        /// <summary>
        /// IdentityServer error
        /// </summary>
        public ErrorMessage Error { get; set; }

        /// <summary>
        /// Support email to display
        /// </summary>
        public string SupportEmail { get; set; } = "support@beyondauth.io";

        /// <summary>
        /// Current app version and instance id
        /// </summary>
        public string Version { get; set; }
    }
}
