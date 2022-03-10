using System.Net;

using d360.core.resources;

namespace d360.core.exceptions
{
    public class InvalidScoreMeasure : BaseException
    {
        public InvalidScoreMeasure(string description)
            : base(HttpStatusCode.PreconditionFailed, OthersError.InvalidMeasureFound, description)
        {
        }
    }
}
