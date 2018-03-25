using d360.core;
using d360.extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace igx.jobs.eaglemessagestreamprocessor
{
    #region ENUMs

    public enum ChangeType
    {
        Notset,
        Add,
        Delete,
        None
    }

    public enum MapDirection
    {
        Input,
        Output
    }

    public enum MapFormat
    {
        CSV,
        Bloomberg,
        Fixed,
        SIRS,
        Star,
        Swift,
        TagValue,
        XML,
        Unknown
    }

    public enum RelationshipColumnType
    {
        Mapping,
        Constant
    }

    public enum RelationshipExpressionType
    {
        ConstantValue, // some string in a value
        DirectMapping, // star tag - bloomberg mnemonic
        ConditionalMapping, // some expression if this then that yada yada
        Unknown
    }

    public enum RulesetFileType
    {
        Default,
        Conditional
    }
    
    #endregion

    public class BloombergMapList : MapList
    {

        protected override IRelationshipMapping CreateRelationship(string tag, string expression, string description)
        {
            //we need to handle interpretation of the expression

            var relationship = new BloombergRelationshipMapping { Tag = tag, Expression = expression, UserDescription = description };

            //need to evaluate the relationship

            interpretRelationshipTagValue(relationship);

            parseExpression(relationship);

            return relationship;
        }


        private void parseExpression(BloombergRelationshipMapping rel)
        {
            //determine what we are dealing with
            DetermineExpressionValueType(rel);

            switch (rel.ExpressionValueType)
            {
                case RelationshipExpressionType.ConstantValue:
                    rel.ConstantValue = rel.Expression;
                    break;
                case RelationshipExpressionType.DirectMapping:
                    //some direct mappings are referneces to prior mappings tags.  We must check if the expression is the tag on a prior column
                    //if (Regex.IsMatch(rel.Expression, ":tag(.*?):"))
                    if (!isExressionValueAPriorTag(rel))
                        rel.BloombergMnemonics.Add(rel.Expression.Trim(':')); //single mnemonic value
                    break;
                case RelationshipExpressionType.ConditionalMapping:
                    extractMnemonics(rel);
                    break;
                case RelationshipExpressionType.Unknown:
                    break;
                default:
                    break;
            }
        }


        private void DetermineExpressionValueType(BloombergRelationshipMapping rel)
        {
            // if first and last character is : then its a direct bloomberg mnemonic
            // if first and last character is ' then its a constant value
            if (string.IsNullOrEmpty(rel.Expression))
                rel.ExpressionValueType = RelationshipExpressionType.Unknown;
            //else if (rel.Expression[0] == '\'' && rel.Expression[rel.Expression.Length - 1] == '\'')
            else if (Regex.IsMatch(rel.Expression, "^\'(.*?)\'$")) // a expression starting and ending in " with leters or numbers or spaces inbetween
                rel.ExpressionValueType = RelationshipExpressionType.ConstantValue;
            else if (Regex.IsMatch(rel.Expression, "^:([a-zA-Z0-9.,$;-_]+):$")) // a expression starting and ending in : with either a constant or bb mnemonic
                rel.ExpressionValueType = RelationshipExpressionType.DirectMapping;
            else
                rel.ExpressionValueType = RelationshipExpressionType.ConditionalMapping;
        }

        private bool isExressionValueAPriorTag(BloombergRelationshipMapping rel)
        {
            bool isTagReference = false;

            var val = rel.Expression.Trim(':');
            val = val.ToUpper();

            if (val.StartsWith("TAG", StringComparison.InvariantCulture))
            {
                isTagReference = true;

                val = val.Replace("TAG", ""); //sometime they start the value with TAG...
            }

            uint tempUInt = 0;
            if (uint.TryParse(val, out tempUInt))
                isTagReference = true;


            var ex = this.Find(x => x.Tag == val);

            if (ex == null && isTagReference) return true; // just ingore this as its a tag reference and we cant find it            
            if (ex == null) return false;

            rel.Expression = ex.Expression;

            //need to recheck call parse expression to update any properties that may change as a result of this reference to another expression 
            parseExpression(rel);

            return true; //caution recursive
        }

        private void extractMnemonics(BloombergRelationshipMapping rel)
        {
            //  return; // dont want to do this yet as it will take things in if(my value) then my value 2 

            //run regexpression to find all 
            var matches = Regex.Matches(rel.Expression, ":([A-Z0-9-_]+):");

            foreach (var match in matches)
            {
                var item = match.ToString().Trim(':');

                if (!rel.BloombergMnemonics.Contains(item))
                    rel.BloombergMnemonics.Add(item);
            }

        }
    }

    public class BloombergRelationshipMapping : IRelationshipMapping
    {

        /// <summary>
        /// The contents of the tag attribute 
        /// possible values are a constant or a star tag (unsigned int)
        /// </summary>
        public string Tag { get; set; }

        public string Expression { get; set; }

        public string UserDescription { get; set; }
        public string ConstantName { get; set; }
        public string ConstantValue { get; set; }
        public uint StarTag { get; set; }
        public RelationshipColumnType ColumnTagType { get; set; }
        public RelationshipExpressionType ExpressionValueType { get; set; }

        List<string> _bbMnemonics = new List<string>();
        public List<string> BloombergMnemonics { get { return _bbMnemonics; } }

    }

    public class CSVMapList : MapList
    {
        protected override IRelationshipMapping CreateRelationship(string tag, string expression, string description)
        {
            //we need to handle interpretation of the expression

            var relationship = new BloombergRelationshipMapping { Tag = tag, Expression = expression, UserDescription = description };

            //need to evaluate the relationship

            interpretRelationshipTagValue(relationship);

            parseExpression(relationship);

            return relationship;
        }


        private void parseExpression(BloombergRelationshipMapping rel)
        {
            //determine what we are dealing with
            DetermineExpressionValueType(rel);

            switch (rel.ExpressionValueType)
            {
                case RelationshipExpressionType.ConstantValue:
                    rel.ConstantValue = rel.Expression;
                    break;
                case RelationshipExpressionType.DirectMapping:
                    if (!isExressionValueAPriorTag(rel))
                        rel.BloombergMnemonics.Add(rel.Expression.Trim('|')); //single mnemonic value
                    break;
                case RelationshipExpressionType.ConditionalMapping:
                    extractMnemonics(rel);
                    break;
                case RelationshipExpressionType.Unknown:
                    break;
                default:
                    break;
            }
        }


        private void DetermineExpressionValueType(BloombergRelationshipMapping rel)
        {
            // if first and last character is : then its a direct bloomberg mnemonic
            // if first and last character is ' then its a constant value
            if (string.IsNullOrEmpty(rel.Expression))
                rel.ExpressionValueType = RelationshipExpressionType.Unknown;
            //else if (rel.Expression[0] == '\'' && rel.Expression[rel.Expression.Length - 1] == '\'')
            else if (Regex.IsMatch(rel.Expression, "^\'(.*?)\'$")) // a expression starting and ending in " with leters or numbers or spaces inbetween
                rel.ExpressionValueType = RelationshipExpressionType.ConstantValue;
            else if (Regex.IsMatch(rel.Expression, "^\\|([a-zA-Z0-9.,$;-_]+)\\|$")) // a expression starting and ending in : with either a constant or bb mnemonic
                rel.ExpressionValueType = RelationshipExpressionType.DirectMapping;
            else
                rel.ExpressionValueType = RelationshipExpressionType.ConditionalMapping;
        }

        private bool isExressionValueAPriorTag(BloombergRelationshipMapping rel)
        {
            bool isTagReference = false;

            var val = rel.Expression.Trim(':');
            val = val.ToUpper();

            if (val.StartsWith("TAG", StringComparison.InvariantCulture))
            {
                isTagReference = true;

                val = val.Replace("TAG", ""); //sometime they start the value with TAG...
            }

            uint tempUInt = 0;
            if (uint.TryParse(val, out tempUInt))
                isTagReference = true;


            var ex = this.Find(x => x.Tag == val);

            if (ex == null && isTagReference) return true; // just ingore this as its a tag reference and we cant find it            
            if (ex == null) return false;

            rel.Expression = ex.Expression;

            //need to recheck call parse expression to update any properties that may change as a result of this reference to another expression 
            parseExpression(rel);

            return true; //caution recursive
        }

        private void extractMnemonics(BloombergRelationshipMapping rel)
        {
            //  return; // dont want to do this yet as it will take things in if(my value) then my value 2 

            //run regexpression to find all 
            var matches = Regex.Matches(rel.Expression, ":([A-Z0-9-_]+):");

            foreach (var match in matches)
            {
                var item = match.ToString().Trim(':');

                if (!rel.BloombergMnemonics.Contains(item))
                    rel.BloombergMnemonics.Add(item);
            }

            //run regexpression to find all 
            var matchesPipe = Regex.Matches(rel.Expression, "\\|([A-Z0-9-_]+)\\|");

            foreach (var match in matchesPipe)
            {
                var item = match.ToString().Trim('|');

                if (!rel.BloombergMnemonics.Contains(item))
                    rel.BloombergMnemonics.Add(item);
            }

        }
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

    public interface IRelationshipMapping
    {
        string Expression { get; set; }
        string Tag { get; set; }
        string UserDescription { get; set; }
        RelationshipColumnType ColumnTagType { get; set; }
        RelationshipExpressionType ExpressionValueType { get; set; }
        uint StarTag { get; set; }
        string ConstantName { get; set; }
    }

    public abstract class MapList : List<IRelationshipMapping>
    {

        public void Load(XElement doc)
        {
            try
            {
                var format = doc.Attribute("FORMAT") == null ? string.Empty : doc.Attribute("FORMAT").Value;
                var direction = doc.Attribute("DIRECTION") == null ? string.Empty : doc.Attribute("DIRECTION").Value;
                var created = doc.Attribute("CREATED") == null ? string.Empty : doc.Attribute("CREATED").Value;
                var createDate = doc.Attribute("DATE") == null ? string.Empty : doc.Attribute("DATE").Value;


                //In or out throw exception if something else
                if (direction.ToUpper() == "I")
                    Direction = MapDirection.Input;
                else
                    Direction = MapDirection.Output;

                MapFormat tempFormat = MapFormat.Unknown;

                Enum.TryParse<MapFormat>(format, true, out tempFormat);

                Format = tempFormat;

                CreatedBy = created;

                CreatedOn = DateTime.ParseExact(createDate, "yyyy-MM-dd hh:mm", CultureInfo.InvariantCulture);


                var cols = doc.Element("EAGLESTARFILEFORMAT").Elements("BATCHIMPORT").Elements("COLUMNS").SelectMany(el => el.Elements("COL"));

                foreach (var col in cols)
                {
                    AddColumn(col);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        public MapFormat Format { get; set; }

        public MapDirection Direction { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        Dictionary<string, string> _constants = new Dictionary<string, string>();
        public Dictionary<string, string> Constants { get { return _constants; } set { _constants = value; } }

        private void AddColumn(XElement col)
        {
            var userdescription = col.Attribute("USERDESCRIPTION") != null ? col.Attribute("USERDESCRIPTION").Value : string.Empty;
            var expression = col.Attribute("EXPRESSION") != null ? col.Attribute("EXPRESSION").Value : string.Empty;
            var tag = col.Attribute("TAG") != null ? col.Attribute("TAG").Value : string.Empty;

            this.Add(CreateRelationship(tag.ToUpper(), expression, userdescription));
        }

        protected abstract IRelationshipMapping CreateRelationship(string tag, string expression, string description);



        /// <summary>
        /// Used to determine the use of the tag attribute in an xml mapping file
        /// some tags are used as constants 
        /// others directly reference a star tag
        /// </summary>
        /// <param name="relationship"></param>
        protected void interpretRelationshipTagValue(IRelationshipMapping relationship)
        {
            uint tempUInt = uint.MinValue;
            if (uint.TryParse(relationship.Tag, out tempUInt))
            {
                relationship.ColumnTagType = RelationshipColumnType.Mapping;
                relationship.StarTag = tempUInt;
            }
            else
            {
                relationship.ConstantName = relationship.Tag;
                relationship.ColumnTagType = RelationshipColumnType.Constant;
            }
        }

    }

    public static class MapListCreator
    {
        public static MapList Create(MapFormat format, XElement doc)
        {
            MapList list = null;

            switch (format)
            {
                case MapFormat.CSV:
                    list = new CSVMapList();
                    break;
                case MapFormat.Bloomberg:
                    list = new BloombergMapList();
                    break;
                case MapFormat.Fixed:
                    break;
                case MapFormat.SIRS:
                    break;
                case MapFormat.Star:
                    break;
                case MapFormat.Swift:
                    break;
                case MapFormat.TagValue:
                    break;
                case MapFormat.XML:
                    break;
                case MapFormat.Unknown:
                    break;
                default:
                    break;
            }

            if (list == null)
                throw new Exception("UNSUPPORTED MESSAGE CENTER FORMAT");

            list.Load(doc);

            return list;
        }
    }

    public class RelationshipEqualityComparer : IEqualityComparer<IRelationshipMapping>
    {
        public bool Equals(IRelationshipMapping x, IRelationshipMapping y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;

            return string.Compare(x.Tag, y.Tag, true) == 0 && string.Compare(x.Expression, y.Expression, true) == 0;
        }

        public int GetHashCode(IRelationshipMapping obj)
        {
            int hash = 17;
            hash = hash * 23 + (obj.Expression ?? "").GetHashCode();
            hash = hash * 23 + (obj.Tag ?? "").GetHashCode();
            return hash;
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

        public IEnumerable<IRelationshipMapping> Relations
        {
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

            if (defaultFileAttribute != null)
                rs.FileList.Add(new RulesetFile { FileType = RulesetFileType.Default, FileName = defaultFileAttribute.Value });

            var conditionalFiles = doc.Descendants("IF").Select(x => x);

            foreach (var conditionalFile in conditionalFiles)
            {
                var condition = conditionalFile.Attribute("EXPR") == null ? string.Empty : conditionalFile.Attribute("EXPR").Value;
                var fileName = conditionalFile.Attribute("FILE") == null ? string.Empty : conditionalFile.Attribute("FILE").Value.ToLower();

                if (!rs.FileList.Where(x => x.FileName == fileName).Any())
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
                catch (Exception ex)
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
                    Console.WriteLine("Error while parsing xml mapping file: [{0}] path: [{1}] content: [{2}]", file.FileName, path, sXML);
                    throw ex;
                }

                if (xmlMapping == null) continue;

                ruleset.XmlMappings.Add(MapListCreator.Create(ruleset.Format, xmlMapping));
            }
        }
    }

    public class RulesetFile
    {
        public string FileName { get; set; }
        public string Condition { get; set; }
        public RulesetFileType FileType { get; set; }

    }

    internal class StagingFile
    {
        public int ID { get; set; }
        public int FusionID { get; set; }
        public int FussionAttributeID { get; set; }
        public string File { get; set; }
        public DateTime UpdatedOn { get; set; }
    }

    internal class StagingFileItem
    {
        public int StagingFileID { get; set; }
        public string Tag { get; set; }
        public string Value { get; set; }
        public ChangeType ChangeType { get; set; }
        public string Description { get; set; }
    }
}
