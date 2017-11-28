using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace igx.functions.fusion.Connector.Eagle
{
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
}
