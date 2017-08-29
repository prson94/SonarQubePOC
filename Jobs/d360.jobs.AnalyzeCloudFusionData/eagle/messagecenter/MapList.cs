using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace d360.jobs.AnalyzeCloudFusionData.eagle.messageCenter
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

                //add relations for any postprocessing

                if (doc.Element("POSTPROCESSING") != null)
                {
                    var postProcessing = doc.Element("POSTPROCESSING").Elements("PROCESSINSTREAM");

                    foreach (var item in postProcessing)
                    {
                        AddPostProcess(item);
                    }
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

        private void AddPostProcess(XElement item)
        {
            var messageData = item.Attribute("MESSAGEDATA") != null ? item.Attribute("MESSAGEDATA").Value : string.Empty;

            if (string.IsNullOrEmpty(messageData)) return;

            //parse the message data 
            var messageItems = messageData.Split('+');

            // loop through and peak at the next item if it is a mnemonic add a relationship
            var i = 0;
            var j = 1;

            while(j < messageItems.Length)
            {
                var firstItem = messageItems[i];
                var secondItem = messageItems[j];

                if(string.IsNullOrEmpty(firstItem) || string.IsNullOrEmpty(secondItem))
                {
                    i++;j++;
                    continue;
                }

                firstItem = firstItem.Trim('\'');
                uint tag = 0;
                //if item is not star tag move up one
                if ((firstItem[0] != ':') || (firstItem[firstItem.Length - 1] != ':'))
                {
                    i++;j++;
                    continue;
                }

                //stripg the : from the firstitem
                firstItem = firstItem.Trim(':');

                if(!uint.TryParse(firstItem, out tag))
                {
                    i++; j++;
                    continue;
                }

                // if item is not mnemonic move up one.
                if ((secondItem[0] !='|') || (secondItem[secondItem.Length-1] !='|'))
                {
                    i++; j++;
                    continue;
                }


                // if is match add relationship and move up 2.
                var rel = new BloombergRelationshipMapping { StarTag = tag, Tag = firstItem, ExpressionValueType = RelationshipExpressionType.DirectMapping};

                rel.BloombergMnemonics.Add(secondItem.Trim('|'));

                this.Add(rel);

                i = i + 2;
                j = j + 2;
            }
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