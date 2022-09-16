namespace Api.Modules.Pusher.Models
{
    //TODO Verify comments
    /// <summary>
    /// A model for a Wiser pusher message request.
    /// </summary>
    public class PusherMessageRequestModel
    {
        /// <summary>
        /// Gets or sets the channel to send the message on.
        /// </summary>
        public string Channel { get; set; }

        /// <summary>
        /// Gets or sets the event data of the message.
        /// </summary>
        public object EventData { get; set; }

        /// <summary>
        /// Gets or sets the cluster to send the message in.
        /// </summary>
        public string Cluster { get; set; }

        /// <summary>
        /// Gets or sets the ID of the user to send the message to.
        /// </summary>
        public ulong UserId { get; set; }

        /// <summary>
        /// Whether the message is not meant for a specific user, but for all connected users.
        /// </summary>
        public bool IsGlobalMessage { get; set; }
    }
}
