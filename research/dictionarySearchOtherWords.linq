<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Xml.Linq.dll</Reference>
  <Namespace>System.Xml.Linq</Namespace>
</Query>

void Main()
{
	var searcher = new VerseSearch(new Lazy<XmlDocument>(()=>XmlHelps.GetDocument(@"..\src\simplebibleapp\simplebibleapp\Data\Bible\kjvfull.xml")));
	Console.WriteLine(searcher.GetVersesByWordRef(1344));
	Console.WriteLine(searcher.GetVersesByWordRef(1345));
	Console.WriteLine(searcher.GetVersesByWordRef(1346));
}

class VerseInfo
{
	public string ChapterAbbr { get; }
	public int ChapterNumber { get; }
	public int VerseNumber { get; }
	public string VerseXmlText { get; }
	public VerseInfo(string chapterAbbr, int chapterNumber, int verseNumber, string verseXmlText)
	{
		ChapterAbbr = chapterAbbr;
		ChapterNumber = chapterNumber;
		VerseNumber = verseNumber;
		VerseXmlText = verseXmlText;
	}
}
class XmlHelps
{
	public static XmlDocument GetDocument(string xmlPath)
	{
		var doc = new XmlDocument();
		doc.Load(xmlPath);
		return doc;
	}

}
class VerseSearch
{
	private readonly Lazy<XmlDocument> _bible;
	public VerseSearch(Lazy<XmlDocument> bible)
	{
		_bible = bible;
	}
	public IEnumerable<VerseInfo> GetVersesByWordRef(int highlitedWordId)
	{
		var doc = _bible.Value;
		var words = SearchGreekDictionary(doc, highlitedWordId);
		return words.Select(w => GetVerseInfo(GetVerseNodeFromWord(w), highlitedWordId));
	}

	VerseInfo GetVerseInfo(XmlNode verseNode, int highlitedWordId)
	{
		var refText = verseNode.Attributes.Cast<XmlAttribute>().FirstOrDefault(x => x.Name == "osisID")?.InnerText;
		var refTextParts = refText.Split('.');
		var verseText = GetHighlightedWordTextXml(GetWordVerse(verseNode), highlitedWordId);
		return new VerseInfo(refTextParts[0], int.Parse(refTextParts[1]), int.Parse(refTextParts[2]), verseText);
	}

	string GetHighlightedWordTextXml(IEnumerable<XmlNode> verseNodes, int highlitedWordId)
	{
		var verseText = new StringBuilder();
		Func<XmlNode, string> wordSpace = n => n.NodeType != XmlNodeType.Text && n.PreviousSibling?.Name == "w" ? " " : string.Empty;
		Func<XmlNode, string> getEmphText = n =>
					 n.NodeType != XmlNodeType.Text &&
					 n.Name == "w" &&
					 n.Attributes.Cast<XmlAttribute>().Any(x => x.Name == "lemma" && x.InnerText.Contains(highlitedWordId.ToString())) ?
					 $"<em>{n.InnerText}</em>" :
					 n.InnerText;
		verseNodes.ToList().ForEach(n =>
		{
			verseText.Append(wordSpace(n) + getEmphText(n));
		});
		return $"<verse>{verseText.ToString()}</verse>";
	}


	IEnumerable<XmlNode> SearchGreekDictionary(XmlDocument doc, int greekStrongsRef) =>
		doc.SelectNodes($"//*[name()='w' and contains(@lemma, 'strong:G{greekStrongsRef}')]").Cast<XmlNode>();

	IEnumerable<XmlNode> GetWordVerse(XmlNode word)
	{
		var verseRef = GetVerseNodeFromWord(word);
		return GetVerseNodesFromTopVerseNode(verseRef);
	}

	XmlNode GetVerseNodeFromWord(XmlNode word)
	{
		var topVerse = word;
		while (topVerse.Name != "verse")
		{
			//words can be wrapped in other tags like quotes, verse can continue outside of parent tags
			//therefore the prev sibling could be null because it is the beginning of a wrapping node but NOT
			//the start of a verse
			if (topVerse.PreviousSibling == null)
			{
				topVerse = topVerse.ParentNode.PreviousSibling;
			}
			topVerse = topVerse.PreviousSibling;
		}
		return topVerse;
	}

	IEnumerable<XmlNode> GetVerseNodesFromTopVerseNode(XmlNode verseNode)
	{
		var output = new List<XmlNode>();
		output.Add(verseNode);
		var bottomVerse = verseNode.NextSibling;
		while (bottomVerse.Name != "verse")
		{
			if (bottomVerse.Name == "q")
			{
				bottomVerse = bottomVerse.ChildNodes[0];
			}
			//words can be wrapped in other tags like quotes, verse can continue outside of parent tags
			//therefore the next sibling could be null because it is the end of a wrapping node but NOT
			//the end of a verse
			if (bottomVerse.NextSibling == null)
			{
				bottomVerse = bottomVerse.ParentNode.NextSibling;
			}
			output.Add(bottomVerse);
			bottomVerse = bottomVerse?.NextSibling;
		}
		return output;
	}
}
// Define other methods and classes here