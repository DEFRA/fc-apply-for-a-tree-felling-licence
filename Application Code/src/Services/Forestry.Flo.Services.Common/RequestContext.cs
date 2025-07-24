using Forestry.Flo.Services.Common.User;

namespace Forestry.Flo.Services.Common
{
    /// <summary>
    /// Class representing a request to the application.
    /// </summary>
    public class RequestContext
    {
        /// <summary>
        /// Gets and sets the identifier for the request.
        /// </summary>
        public string RequestId { get; }

        public ActorType ActorType { get; }

        /// <summary>
        /// Creates a new instance of a <see cref="RequestContext"/>.
        /// </summary>
        /// <param name="requestId">The identifier for the request.</param>
        /// <param name="requestUserModel">The user making the request</param>
        public RequestContext(string requestId, 
            RequestUserModel requestUserModel)
        {
            RequestId = requestId ?? throw new ArgumentNullException(nameof(requestId));
            ActorType = requestUserModel.ActorType;
        }
    }
}
