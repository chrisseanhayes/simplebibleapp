<Query Kind="Program" />

void Main()
{
	var bible = GetBibleNodes();
	var indexes = GetVerseIndexes(bible);
	var vs1 = GetVerse("Acts.20.35", bible, indexes).Where(v => v.Node.NodeType == XmlNodeType.Text || v.Node.NodeType == XmlNodeType.Whitespace).Dump();
	var vs2 = GetVerse("Rev.1.8", bible, indexes).Where(v => v.Node.NodeType == XmlNodeType.Text || v.Node.NodeType == XmlNodeType.Whitespace).Dump();
	var vs3 = GetVerse("Rev.17.5", bible, indexes).Where(v => v.Node.NodeType == XmlNodeType.Text || v.Node.NodeType == XmlNodeType.Whitespace).Dump();

	string.Join("", vs1.Select(v => v.Node.Text)).Dump();
	string.Join("", vs2.Select(v => v.Node.Text)).Dump();
	string.Join("",vs3.Select(v => v.Node.Text)).Dump();
	
}
Dictionary<string,(int StartIndex,int EndIndex)> GetVerseIndexes(List<XmlNode> bible){
	var output = new Dictionary<string, (int StartIndex,int EndIndex)>();
	for(int i = 0; i < bible.Count; i++){
		if(bible[i].NodeType == XmlNodeType.Element && bible[i].Name == "verse"){
			if(bible[i].Attributes.Any(a => a.Name == "sID")){
				output[bible[i].Attributes.First(a => a.Name == "sID").Value] = (i,0);
			}
			if (bible[i].Attributes.Any(a => a.Name == "eID"))
			{
				var current = output[bible[i].Attributes.First(a => a.Name == "eID").Value];
				var newval = (current.StartIndex,i);
				output[bible[i].Attributes.First(a => a.Name == "eID").Value] = newval;
			}
		}
	}
	return output;
}
BibleVerseNode[] GetVerse(string verseKey, List<XmlNode> bible, Dictionary<string, (int StartIndex,int EndIndex)> verseIndexes)
{
	var verseStartNodeIndex = bible.FindIndex(b => b.NodeType == XmlNodeType.Element && b.Name == "verse" && b.Attributes.Any(a => a.Name == "sID" && a.Value == verseKey));
	var verseEndNodeIndex = bible.FindIndex(b => b.NodeType == XmlNodeType.Element && b.Name == "verse" && b.Attributes.Any(a => a.Name == "eID" && a.Value == verseKey));
	return bible.Skip(verseStartNodeIndex).Take(verseEndNodeIndex - verseStartNodeIndex)
	.Select(n => new BibleVerseNode(
	n,
	n.IsInParent("q", out var parent) && parent.Attributes.Any(a => a.Name == "who" && a.Value == "Jesus"),
	n.IsInParent("inscription", out var insp)
	)).ToArray();
}
class BibleVerseNode{
	public XmlNode Node { get; private set; }
	public bool IsInJesusQuote { get; private set; }
	public bool IsInscription { get; private set; }
	public BibleVerseNode(XmlNode node, bool isInJesusQuote, bool isInscription)
	{
		Node = node;
		IsInJesusQuote = isInJesusQuote;
		IsInscription = isInscription;
	}
}
List<XmlNode> GetBibleNodes(){
	var xmldoc = new List<XmlNode>();

	using (var str = File.OpenRead(@"D:\code\simplebibleapp\src\simplebibleapp\simplebibleapp\Data\Bible\kjvfull.xml"))
	{
		NameTable nt = new NameTable();
		XmlNamespaceManager nsmgr = new XmlNamespaceManager(nt);
		XmlParserContext context = new XmlParserContext(null, nsmgr, null, XmlSpace.None);
		XmlTextReader reader = new XmlTextReader(str);

		var parentstack = new Stack<XmlNode>();
		int currentdepth = 0;
		XmlNode currentopenelement = null;

		while (reader.Read())
		{

			if (reader.Depth < currentdepth)
			{
				parentstack.Pop();
				currentdepth = reader.Depth;
			}

			if (reader.Depth > currentdepth)
			{
				parentstack.Push(currentopenelement);
				currentdepth = reader.Depth;
			}


			var current = GetNodeFromReader(reader, parentstack.Any() ? parentstack.Peek() : null);

			xmldoc.Add(current);
			
			if(current.NodeType == XmlNodeType.Element) currentopenelement = current;

			//Console.WriteLine(current.ToString());

		}

		reader.Close();
	}
	return xmldoc;
}
private XmlNode GetNodeFromReader(XmlTextReader reader, XmlNode currentParent)
{
	return new XmlNode(
				reader.Depth,
				reader.LineNumber,
				reader.LinePosition,
				reader.Name,
				reader.NodeType,
				reader.Value,
				reader.Depth == 0 ? null : currentParent,
				reader.HasAttributes ? GetAttributes(reader) : new XmlAttributeKVP[0]);
}
private XmlAttributeKVP[] GetAttributes(XmlTextReader reader)
{
	var attributes = new List<XmlAttributeKVP>();
	reader.MoveToFirstAttribute();
	do
	{
		attributes.Add(new XmlAttributeKVP(reader.Name, reader.Value));
	} while (reader.MoveToNextAttribute());

	return attributes.ToArray();
}
class XmlAttributeKVP
{
	public string Name { get; private set; }
	public string Value { get; private set; }
	public XmlAttributeKVP(string name, string @value)
	{
		Name = name;
		Value = @value;
	}

	public override string ToString()
	{
		return $"{Name}: \"{Value}\"";
	}
}
class XmlNode
{
	public int Depth { get; private set; }
	public int LineNumber { get; private set; }
	public int LinePosistion { get; private set; }
	public string Name { get; private set; }
	public XmlNodeType NodeType { get; private set; }
	public string Text { get; private set; }
	public XmlNode Parent { get; private set; }
	public XmlAttributeKVP[] Attributes { get; private set; }
	public XmlNode(int depth, int linenumber, int lineposition, string name, XmlNodeType type, string text, XmlNode parent, XmlAttributeKVP[] attributes)
	{
		Depth = depth;
		LineNumber = linenumber;
		LinePosistion = lineposition;
		Name = name;
		NodeType = type;
		Text = text;
		Parent = parent;
		Attributes = attributes;
	}
	
	public bool IsInParent(string parentElementName, out XmlNode parent){
		parent = this.Parent;
		while(parent != null){
			if(parent.Name == parentElementName) return true;
			parent = parent.Parent;
		}
		return false;
	}

	public override string ToString()
	{
		return $"Depth: {Depth}; LineNumber: {LineNumber}; LinePosition: {LinePosistion}; Name: {Name}; NodeType: {NodeType}; Text: {Text}; Parent: {Parent?.Name}; Attributes:{{{string.Join("; ", Attributes.AsEnumerable())}}}";
	}

}

// Define other methods and classes here
