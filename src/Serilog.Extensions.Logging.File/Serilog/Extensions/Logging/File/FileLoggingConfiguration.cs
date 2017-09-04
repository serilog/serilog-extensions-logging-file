namespace Serilog.Extensions.Logging.File
{
    /// <summary>
    /// Configuration for the Serilog file logging.
    /// </summary>
    internal class FileLoggingConfiguration
    {
        internal const long DefaultFileSizeLimitBytes = 1024 * 1024 * 1024;
        internal const int DefaultRetainedFileCountLimit = 31;

        /// <summary>
        /// Filname to write. The filename may include <c>{Date}</c> to specify
        /// how the date portion of the filename is calculated. May include
        /// environment variables.
        /// </summary>
        public string PathFormat
        { get; set; }

        /// <summary>
        /// If <c>true</c>, the log file will be written in JSON format.
        /// </summary>
        public bool Json
        { get; set; }

        /// <summary>
        /// The maximum size, in bytes, to which any single log file will be
        /// allowed to grow. For unrestricted growth, pass <c>null</c>. The
        /// default is 1 GiB.
        /// </summary>
        public long? FileSizeLimitBytes
        { get; set; } = DefaultFileSizeLimitBytes;

        /// <summary>
        /// The maximum number of log files that will be retained, including
        /// the current log file. For unlimited retention, pass <c>null</c>.
        /// The default is 31.
        /// </summary>
        public int? RetainedFileCountLimit
        { get; set; } = DefaultRetainedFileCountLimit;
    }
}
