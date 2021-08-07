<Query Kind="Program" />

void Main()
{
	var bible = GetBibleNodes();
	var verseStartNodeIndex = Array.FindIndex(bible, b => b.NodeType == XmlNodeType.Element && b.Name == "verse" && b.Attributes.Any(a => a.Name == "sID" && a.Value == "Acts.20.35"));
	var verseEndNodeIndex = Array.FindIndex(bible, b => b.NodeType == XmlNodeType.Element && b.Name == "verse" && b.Attributes.Any(a => a.Name == "eID" && a.Value == "Acts.20.35"));
	bible[verseStartNodeIndex..^verseEndNodeIndex]
	.Select(n => new { Node = n, IsInQuote = n.IsInParent("q") }).Dump();
}
XmlNode[] GetBibleNodes()
{
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

			if (current.NodeType == XmlNodeType.Element) currentopenelement = current;

			//Console.WriteLine(current.ToString());

		}

		reader.Close();
	}
	return xmldoc.ToArray();
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

	public bool IsInParent(string parentElementName)
	{
		var parent = this.Parent;
		while (parent != null)
		{
			if (parent.Name == parentElementName) return true;
		}
		return false;
	}

	public override string ToString()
	{
		return $"Depth: {Depth}; LineNumber: {LineNumber}; LinePosition: {LinePosistion}; Name: {Name}; NodeType: {NodeType}; Text: {Text}; Parent: {Parent?.Name}; Attributes:{{{string.Join("; ", Attributes.AsEnumerable())}}}";
	}

}

// Define other methods and classes here
