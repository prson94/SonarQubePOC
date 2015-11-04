using System.Collections.Generic;

namespace d360.core.queue
{
    public class IndexObjectModel : QueueObject
    {
        public int ID { get; set; }

        public string RelativeUrl { get; set; }

        /// <summary>
        /// This is the name of the underlying type, such as:
        /// Business Term, Application, etc.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// This value contains the high-level type of object this is: 
        /// Artifact, Information Model, Domain, Fusion
        /// </summary>
        public string Group { get; set; }

        public Dictionary<string, string> Fields { get; set; }
    }
}
