using System.Net;

namespace d360.core.exceptions
{
    public class ScoresCurrentlyProcessingException : BaseException
    {
        public ScoresCurrentlyProcessingException()
            :base(HttpStatusCode.Conflict, "Scores Message Already Being Processed", "Scores Message Already Being Processed")
        {
        }
    }
}
