namespace Authentication.Models
{
    /// <summary>
    /// Email variable
    /// </summary>
    public class TemplateVariable
    {
        /// <summary>
        /// Variable name as used in template
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Contents of variable
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Value is sensitive and should not be saved
        /// </summary>
        public bool Sensitive { get; set; } = false;
    }
}
