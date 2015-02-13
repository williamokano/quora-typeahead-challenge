using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace TypeaheadSearch
{
    #region Class Item
    public class Item
    {
        public string type { get; set; }
        public string id { get; set; }
        public decimal score { get; set; }
        public string dataString { get; set; }
        public bool deleted { get; set; } //This is for lazy deletion
    }
    #endregion

    #region Class Program
    class Program
    {

        #region Attributes
        private IList<Item> items;
        private QuickTree tree;
        #endregion

        #region Start Program
        static void Main(string[] args)
        {
            new Program().Run(args);
        }
        #endregion

        #region Run method
        public void Run(string[] args)
        {
            int numberOfInputs = 0;
            string tempLine = string.Empty;
            StreamReader input = GetInputStream();

            //Start the items list to avoid nullpointers
            this.items = new List<Item>();
            this.tree = new QuickTree();

            //The first line from the reader is how many inputs will have
            tempLine = input.ReadLine();
            if (!Int32.TryParse(tempLine, out numberOfInputs))
            {
                Console.WriteLine("Wrong input format!");
                Environment.Exit(-1);
            }

            for (int i = 0; i < numberOfInputs; i++)
            {
                this.Parse(input.ReadLine());
            }

        }
        #endregion

        public void Parse(string strLine)
        {
            ParseAdd(strLine);
            ParseQuery(strLine);
        }

        public void ParseAdd(string strLine)
        {
            Regex addPattern = new Regex("(ADD)\\s+(user|topic|question|board)\\s+(\\w+)\\s+(\\d?\\.\\d+)\\s+(.*)");
            if (addPattern.IsMatch(strLine))
            {
                MatchCollection results = addPattern.Matches(strLine);
                Match match = results[0];

                Item i = new Item();
                i.type = match.Groups[2].Value;
                i.id = match.Groups[3].Value;
                i.score = decimal.Parse(match.Groups[4].Value);
                i.dataString = match.Groups[5].Value;

                this.Add(i);
            }
        }
        public void ParseQuery(string strLine)
        {
            Regex queryPattern = new Regex("\\bQUERY\\b\\s+(\\d+)\\s+(.*)");
            int limit = 0;
            List<string> docIds = new List<string>();

            if (queryPattern.IsMatch(strLine))
            {
                MatchCollection results = queryPattern.Matches(strLine);
                Match match = results[0];

                if (Int32.TryParse(match.Groups[1].Value, out limit))
                {
                    IList<Item> items = this.tree.Find(match.Groups[2].Value)
                        .Take(limit)
                        .ToList();

                    StringBuilder sb = new StringBuilder();
                    foreach (string strId in items.Select(item => item.id).ToList())
                    {
                        sb.Append(string.Format("{0} ", strId));
                    }
                    Console.WriteLine(sb.ToString().Trim());
                }
            }
        }

        public bool Add(Item item)
        {
            //Method to add to the data our great data structure
            try
            {
                //items.Add(item);
                string[] tokens = item.dataString.Split(' ');

                foreach (string token in tokens)
                {
                    this.tree.Add(token, item);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Get Input Method
        public StreamReader GetInputStream()
        {
            //Read from the file, on production, change to stdin
            StreamReader sr = new StreamReader("B:\\quora.txt");
            return sr;
        }
        #endregion

    }
    #endregion

    #region Data structure for quick finding

    public class Node
    {
        public char Letter { get; set; }
        public Node Parent { get; set; }
        public List<Node> Children { get; set; }
        public List<Item> Documents { get; set; }

        public Node()
        {
            this.Children = new List<Node>();
            this.Documents = new List<Item>();
        }

    }

    public class QuickTree
    {
        private List<Node> root;
        public QuickTree()
        {
            this.root = new List<Node>();
        }

        public bool Add(string token, Item document)
        {
            char[] chars = token.ToArray<char>();
            Node iterator = null;
            Node prevNode = null;
            IList<Node> currentNodeList = this.root;

            if (chars.Length > 0)
            {
                //Iterate through the tree and get the last node
                foreach (char c in chars)
                {
                    iterator = currentNodeList.Where(p => p.Letter == c).FirstOrDefault();
                    if (iterator == null)
                    {
                        iterator = new Node();
                        iterator.Letter = c;
                        iterator.Parent = prevNode;

                        currentNodeList.Add(iterator);
                    }
                    prevNode = iterator;
                    currentNodeList = iterator.Children;
                }

                //Insert the document in the document list
                iterator.Documents.Add(document);
                return true;
            }

            return false;
        }

        public IList<Item> Find(string query, bool matchCase = false)
        {
            //Get the query tokens
            List<string> tokens = query.Trim().Split(' ').ToList<string>();

            //Temporary hold the documents found
            List<List<Item>> documentsList = new List<List<Item>>();

            //????
            char[] chars = null;
            Node iterator = null;
            IList<Node> currentNodeList = null;

            //Find the documents
            foreach (string token in tokens)
            {
                chars = token.ToArray<char>();
                iterator = null;
                currentNodeList = this.root;

                if (chars.Length > 0)
                {
                    //Navigate through the tree and get the last node
                    foreach (char c in chars)
                    {
                        if (!matchCase)
                        {
                            iterator = currentNodeList.Where(p => Char.ToUpper(p.Letter) == Char.ToUpper(c)).FirstOrDefault();
                        }
                        else
                        {
                            iterator = currentNodeList.Where(p => p.Letter == c).FirstOrDefault();
                        }

                        //When iterator is null then the word wasn't found
                        if(iterator == null)
                        {
                            return new List<Item>();
                        }
                        
                        currentNodeList = iterator.Children;
                    }

                    //The current node holds the node for the prefix
                    documentsList.Add(this.GetItems(iterator));
                }
            }

            //Three possible cases
            //1 - Only one token
            //--- Then just return in order
            //2 - Two tokens
            //--- Then return the intersection of the two lists
            //3 - Three or more
            //--- Return the intersection of A - B, Then (A - B) - C ... (A - B) - C ... - N
            if (tokens.Count == 1)
            {
                return documentsList[0]
                    .OrderByDescending(item => item.score)
                    .ThenByDescending(item => item.id)
                    .ToList();
            }
            else if (tokens.Count == 2)
            {
                return this.Intersection(documentsList[0], documentsList[1])
                    .OrderByDescending(item => item.score)
                    .ThenByDescending(item => item.id)
                    .ToList();
            }
            else
            {
                return null;
            }
        }

        private List<Item> GetItems(Node _root)
        {
            List<Item> documents = _root.Documents;

            //If I have childrens, keep going
            if (_root.Children.Count > 0)
            {
                foreach (Node n in _root.Children)
                {
                    documents.AddRange(GetItems(n));
                }
            }

            return documents;
        }

        private List<Item> Intersection(List<Item> listA, List<Item> listB)
        {
            return listA.Intersect(listB).ToList();
        }

    }

    #endregion
}
