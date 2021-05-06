using System.Net;

namespace d360.core.exceptions
{
    public class InvalidScoreMeasure : BaseException
    {
        public InvalidScoreMeasure(string description)
            :base(HttpStatusCode.PreconditionFailed, "Invalid Measure Found", description)
        {
        }
    }
}
