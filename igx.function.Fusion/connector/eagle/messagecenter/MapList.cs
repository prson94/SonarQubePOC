using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace igx.function.fusion.connector.eagle
{
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
        public Dictionary<string,string> Constants { get { return _constants; } set { _constants = value; } }

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
}