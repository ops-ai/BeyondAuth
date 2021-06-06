namespace BlackstarSolar.AspNetCore.Identity.PwnedPasswords
{
    public class PwnedPasswordsValidatorOptions
    {
        public string ErrorMessage { get; set; } =
            "This password has previously appeared in a data breach and should never be used. If you've ever used it anywhere before, change it immediately!";

        public string ApiKey { get; set; }

        public string UserAgent { get; set; }
    }
}