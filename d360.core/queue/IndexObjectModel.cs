using System.Collections.Generic;

namespace d360.core.queue
{
    public class IndexObjectModel : QueueObject
    {
        public int ID { get; set; }

        public string ItemUniqueID { get; set; }

        public string RelativeUrl { get; set; }

        /// <summary>
        /// This is the name of the underlying type, such as:
        /// Business Term, Application, etc.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// This value contains the high-level type of object this is: 
        /// Artifact, Information Model, Reference, Fusion
        /// </summary>
        public string Group { get; set; }

        public Dictionary<string, string> Fields { get; set; }

        /// <summary>
        /// Returns the unique id for this search item
        /// </summary>
        /// <returns></returns>
        public string getObjectID()
        {
            if (!string.IsNullOrEmpty(ItemUniqueID))
                return $"{Group}|{ItemUniqueID}";
            return $"{Group}|{ID}";
        }
    }

    public class ReindexModel : QueueObject
    {
        //No additional properties
    }
}
