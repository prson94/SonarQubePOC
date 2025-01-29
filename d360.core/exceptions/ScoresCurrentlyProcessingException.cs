using System.Net;

using d360.core.resources;

namespace d360.core.exceptions
{
    public class ScoresCurrentlyProcessingException : BaseException
    {
        public ScoresCurrentlyProcessingException()
            : base(HttpStatusCode.Conflict, Error.ScoreAlreadyProcessed, Error.ScoreAlreadyProcessed)
        {
        }
    }
}
