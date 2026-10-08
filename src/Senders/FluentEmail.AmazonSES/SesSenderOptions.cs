namespace FluentEmail.AmazonSES
{
    /// <summary>
    /// Options for <see cref="SesSender"/>.
    /// </summary>
    public class SesSenderOptions
    {
        /// <summary>
        /// AWS region the SES client talks to, e.g. "us-east-1". Defaults to us-east-1.
        /// </summary>
        public string Region { get; set; } = "us-east-1";

        /// <summary>
        /// Optional explicit AWS access key. When null/empty the SDK's default
        /// credential chain is used (environment variables, shared credentials
        /// file, or the IAM role of the host — e.g. EC2/Lambda).
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Optional explicit AWS secret key. See <see cref="AccessKey"/>.
        /// </summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// Optional SES configuration set applied to every message.
        /// </summary>
        public string ConfigurationSetName { get; set; }
    }
}
