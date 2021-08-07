using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace simplebibleapp.xmlbiblerepository
{
    public static class ScriptureExtensions
    {
        public static IEnumerable<XElement> StreamVerses(string uri, string bookAbbr)
        {
            using (XmlReader reader = XmlReader.Create(uri))
            {
                reader.MoveToContent();
                //Parse the file and display each of the nodes.
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (reader.Name == "verse" && (reader.GetAttribute("sID")?.StartsWith($"{bookAbbr}.") ?? false))
                            {
                                XElement el = XElement.ReadFrom(reader) as XElement;
                                if (el != null)
                                    yield return el;
                            }
                            break;
                    }
                }
            }
        }
    }
}
