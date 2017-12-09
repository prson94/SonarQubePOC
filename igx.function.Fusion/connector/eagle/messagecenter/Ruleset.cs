using d360.core;
using d360.extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace igx.function.fusion.connector.eagle
{
    public enum ChangeType
    {
        Notset,
        Add,
        Delete,
        None
    }

    public class GenericRelationship : IComparable<GenericRelationship>
    {
        public string StarTag { get; set; }
        public string Target { get; set; }
        public ChangeType Change { get; set; }
        public string Raw { get; set; }

        public int CompareTo(GenericRelationship other)
        {
            var res = string.Compare(StarTag, other.StarTag, true);

            if (res != 0) return res;

            return string.Compare(Target, other.Target, true);
        }
    }

    public class Ruleset
    {
        public Ruleset()
        {
            XmlMappings = new List<MapList>();
            FileList = new List<RulesetFile>();
        }

        public List<MapList> XmlMappings { get; set; }

        public MapFormat Format { get; private set; }
        public MapDirection Direction { get; private set; }
        public string CreatedBy { get; private set; }
        public DateTime CreatedOn { get; private set; }

        public List<RulesetFile> FileList { get; set; }

        public IEnumerable<IRelationshipMapping> Relations {
            get
            {
                return XmlMappings.SelectMany(x => x).OrderBy(x => x.Tag).Distinct(new RelationshipEqualityComparer()).ToList();                
            }                
        }

        public IEnumerable<GenericRelationship> FlattendMappings
        {
            get
            {
                //flatten mappings to be tag, nmeonic, change type
                List<IRelationshipMapping> rels = XmlMappings.SelectMany(x => x).OrderBy(x => x.Tag).Distinct(new RelationshipEqualityComparer()).ToList();
                
                foreach (var item in rels)
                {
                    if (item.ExpressionValueType != RelationshipExpressionType.DirectMapping && item.ExpressionValueType != RelationshipExpressionType.ConditionalMapping || item.ColumnTagType != RelationshipColumnType.Mapping) continue;
                    
                    if (item is BloombergRelationshipMapping)
                    {
                        BloombergRelationshipMapping bbMapping = item as BloombergRelationshipMapping;

                        foreach (var mnemonic in bbMapping.BloombergMnemonics)
                        {
                            yield return new GenericRelationship { StarTag = item.Tag, Target = mnemonic, Change = ChangeType.Notset, Raw = item.Expression };
                        }
                    }
                }                
            }
        }

        public static Ruleset Load(IStorageProvider storageProvider, string directory, string fileName)
        {            
            Ruleset ruleset = new Ruleset();

            if (directory[directory.Length - 1] != '/') directory += '/';

            var sXML = storageProvider.GetFileContentsAsString(constants.AZURE_CLOUD_FUSION_CONTAINER, directory + fileName);
            XElement doc = XElement.Parse(sXML);

                
            //determine ruleset properties            
            var format = doc.Attribute("FORMAT") == null ? string.Empty : doc.Attribute("FORMAT").Value;
            var direction = doc.Attribute("DIRECTION") == null ? string.Empty : doc.Attribute("DIRECTION").Value;
            var created = doc.Attribute("CREATED") == null ? string.Empty : doc.Attribute("CREATED").Value;
            var createDate = doc.Attribute("DATE") == null ? string.Empty : doc.Attribute("DATE").Value;

            //In or out throw exception if something else
            if (direction.ToUpper() == "I")
                ruleset.Direction = MapDirection.Input;
            else
                ruleset.Direction = MapDirection.Output;

            MapFormat tempFormat = MapFormat.Unknown;

            Enum.TryParse<MapFormat>(format, true, out tempFormat);

            ruleset.Format = tempFormat;

            ruleset.CreatedBy = created;

            ruleset.CreatedOn = DateTime.ParseExact(createDate, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                        
            loadRulesetFiles(doc, ruleset);

            loadXmlMappings(storageProvider, directory, ruleset);

            return ruleset;
        }

        /// <summary>
        /// Loads the xml mapping files from a given ruleset file
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="rs"></param>
        private static void loadRulesetFiles(XElement doc, Ruleset rs)
        {
            //look for default file
            var defaultFile = doc.Descendants("DEFAULT").FirstOrDefault();
            
            var defaultFileAttribute = defaultFile != null ? defaultFile.Attribute("FILE") : null;

            if(defaultFileAttribute != null)
                rs.FileList.Add(new RulesetFile { FileType = RulesetFileType.Default, FileName = defaultFileAttribute.Value });

            var conditionalFiles = doc.Descendants("IF").Select(x => x);

            foreach (var  conditionalFile in conditionalFiles)
            {
                var condition = conditionalFile.Attribute("EXPR") == null ? string.Empty : conditionalFile.Attribute("EXPR").Value;
                var fileName = conditionalFile.Attribute("FILE") == null ? string.Empty : conditionalFile.Attribute("FILE").Value.ToLower();

                if(!rs.FileList.Where(x => x.FileName == fileName).Any())
                    rs.FileList.Add(new RulesetFile { FileType = RulesetFileType.Conditional, Condition = condition, FileName = fileName.ToLower() });
            }
        }

        /// <summary>
        /// Loads the mappings from a given xml mapping file
        /// </summary>
        /// <param name="ruleset"></param>
        private static void loadXmlMappings(IStorageProvider storageProvider, string path, Ruleset ruleset)
        {
            //based on the format of the ruleset file we need to load the appropriate xml mapping
            foreach (var file in ruleset.FileList)
            {
                string sXML = string.Empty;
                try
                {
                    sXML = storageProvider.GetFileContentsAsString(constants.AZURE_CLOUD_FUSION_CONTAINER, path + file.FileName);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error while loading xml mapping file: [{0}] path: [{1}]", file.FileName, path);
                    throw ex;
                }

                XElement xmlMapping = null;
                //assume the file is in same directory as ruleset
                try
                {
                    xmlMapping = XElement.Parse(sXML);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while parsing xml mapping file: [{0}] path: [{1}] content: [{2}]", file.FileName, path,sXML);
                    throw ex;
                }

                if (xmlMapping == null) continue;

                ruleset.XmlMappings.Add(MapListCreator.Create(ruleset.Format, xmlMapping));
            }            
        }
    }
}
