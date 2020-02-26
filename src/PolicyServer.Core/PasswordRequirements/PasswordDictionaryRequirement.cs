namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Dictionary lookup requirement
    /// Prevents a single dictionary word from being used as the root of a password
    /// </summary>
    public class PasswordDictionaryRequirement : PasswordRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "dictionary";

        /// <summary>
        /// Prevent dictionary words with common subsctitutions
        /// </summary>
        public bool CommonSubstitutions { get; set; }

        /// <summary>
        /// Prevent dictionary words with less common subsctitutions
        /// </summary>
        public bool UncommonSubstitutions { get; set; }

        /// <summary>
        /// Apply fuzzy matching on dictionary words
        /// </summary>
        public bool FuzzyCharacters { get; set; }
    }
}
