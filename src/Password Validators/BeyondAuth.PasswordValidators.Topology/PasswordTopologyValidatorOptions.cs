namespace BeyondAuth.PasswordValidators.Topology
{
    public class PasswordTopologyValidatorOptions
    {
        public string ErrorMessage { get; set; } = "The password topology is too common.";

        /// <summary>
        /// The maximum number of passwords allowed per topology
        /// </summary>
        public long Threshold { get; set; }

        public int RollingHistoryInMonths { get; set; }

        public string TopologyDocumentName { get; set; } = "PasswordTopologies";
    }
}