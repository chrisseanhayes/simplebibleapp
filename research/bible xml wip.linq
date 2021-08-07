<Query Kind="Program" />

void Main()
{
	
	var doc = XDocument.Load(@"D:\code\simplebibleapp\src\simplebibleapp\simplebibleapp\Data\Bible\kjvfull.xml");
	
	var versewords = doc.Descendants("w").Where(d => d?.Attribute("lemma")?.Value.Contains("G2071") ?? false).ToArray();

	var precedingVerseNode = versewords[100]
		.XPathSelectElement("preceding::verse[1]");
	var followingVerseNode = versewords[100]
		.XPathSelectElement("following::verse[1]");

	if(followingVerseNode.Parent == precedingVerseNode.Parent){
		var verseId = precedingVerseNode.Attribute("sID").Value;
	
		var nodesafterverse = precedingVerseNode.NodesAfterSelf()
			.TakeWhile(n => n != followingVerseNode);
		
	}

}

// Define other methods and classes here

string NodePath(XElement node) => 
	string.Join ("/", node.AncestorsAndSelf().Reverse().Select(a => a.Name.LocalName).ToArray());