﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

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
        public decimal boost { get; set; }
        public DateTime dtCreated;
        public int ObjectIdCreation { get; set; }

        public static int ActualCreationId = 1;
        public Item(bool updateObjCreation = true)
        {
            this.dtCreated = DateTime.Now;
            this.boost = 0.0M;
            if (updateObjCreation)
                this.ObjectIdCreation = Item.ActualCreationId++;
        }
    }
    #endregion

    #region Class Program
    class Program
    {

        #region Attributes

        private QuickTree tree;
        private Hashtable items;
        List<string> output = new List<string>();
        private int ObjectIdCounter = 1;
        #endregion

        #region Start Program
        static void Main(string[] args)
        {
            //new Program().Run(args);

            ////////If debbuging, measure the execution time
            var watch = Stopwatch.StartNew();
            new Program().Run(args);
            watch.Stop();

            Console.WriteLine("Execution time: {0}", watch.ElapsedMilliseconds);
        }
        #endregion

        #region Run method
        public void Run(string[] args)
        {
            int numberOfInputs = 0;
            string tempLine = string.Empty;
            IList<string> inputs = new List<string>();
            StreamReader input = GetInputStream();
            //TextReader input = Console.In;

            //Start the items list to avoid nullpointers
            this.items = new Hashtable();
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
                inputs.Add(input.ReadLine());
            }

            for (int i = 0; i < numberOfInputs; i++)
            {
                if (i == 16904)
                {

                }

                this.Parse(inputs[i]);
            }
            string formattedOutput = string.Join("\n", output.ToArray());
            Console.Write(formattedOutput);

            using (StreamWriter sw = new StreamWriter("B:\\output.txt"))
            {
                sw.Write(formattedOutput);
            }
        }
        #endregion

        public void Parse(string strLine)
        {
            ParseAdd(strLine);
            ParseQuery(strLine);
            ParseWQuery(strLine);
            ParseDel(strLine);
        }

        public void ParseAdd(string strLine)
        {
            Regex addPattern = new Regex("(ADD)\\s+(user|topic|question|board)\\s+(.+)\\s+(\\d+\\.\\d+|\\.\\d+|\\d+\\.|\\d+)\\s+(.*)");
            if (addPattern.IsMatch(strLine))
            {
                MatchCollection results = addPattern.Matches(strLine);
                Match match = results[0];

                Item i = new Item();
                i.type = match.Groups[2].Value;
                i.id = match.Groups[3].Value;
                i.score = decimal.Parse(match.Groups[4].Value);
                i.dataString = Program.CleanString(match.Groups[5].Value.Trim());
                i.ObjectIdCreation = ObjectIdCounter++;

                this.Add(i);
            }
        }
        public void ParseQuery(string strLine)
        {
            Regex queryPattern = new Regex("\\bQUERY\\b\\s+(\\d+)\\s+(.*)");
            int limit = 0;
            string dataString = string.Empty;

            if (queryPattern.IsMatch(strLine))
            {
                MatchCollection results = queryPattern.Matches(strLine);
                Match m = results[0];

                if (Int32.TryParse(m.Groups[1].Value, out limit))
                {
                    dataString = m.Groups[2].Value;

                    this.output.Add(ShowResults(dataString, limit));
                }
            }
        }

        /// <summary>
        /// Resolves the issue #1 //
        /// </summary>
        /// <param name="strLine"></param>
        public void ParseWQuery(string strLine)
        {
            Regex wQueryPatter = new Regex("^WQUERY\\s+(\\d+)\\s+(\\d+)\\s+((?:[^\\s.]+\\:\\d*\\.?\\d*\\s+)*)(.*)");
            List<Tuple<string, decimal>> boosts = null;
            decimal tmpScoreBoost = 0.0M;
            int boosters = 0;
            int limit = 0;
            string dataString = string.Empty;
            MatchCollection results = null;

            if (wQueryPatter.IsMatch(strLine))
            {
                //Get boosters
                results = wQueryPatter.Matches(strLine);

                if (Int32.TryParse(results[0].Groups[2].Value, out boosters))
                {
                    if (boosters > 0)
                    {
                        boosts = new List<Tuple<string, decimal>>();
                        string[] _boosts = results[0].Groups[3].Value.Trim().Split(' ');
                        for (int i = 0; i < boosters; i++)
                        {
                            string[] tmp = _boosts[i].Split(':');
                            if (tmp.Length == 2)
                            {
                                if (decimal.TryParse(tmp[1], out tmpScoreBoost))
                                {
                                    boosts.Add(new Tuple<string, decimal>(tmp[0], tmpScoreBoost));
                                }
                            }
                        }
                    }
                }

                Match m = results[0];
                if (Int32.TryParse(m.Groups[1].Value, out limit))
                {
                    dataString = m.Groups[4].Value;

                    //Show results
                    this.output.Add(ShowResults(dataString, limit, boosts));
                }
            }
        }

        public void ParseDel(string strLine)
        {
            Regex regDel = new Regex("^DEL\\s+(.+)");
            if (regDel.IsMatch(strLine))
            {
                MatchCollection results = regDel.Matches(strLine);
                Match m = results[0];

                string id = m.Groups[1].Value.Trim();

                //Verify if the item exists
                if (items[id] != null)
                {
                    Item i = (Item)items[id];

                    //Remove references from three
                    this.tree.Del(i);

                    //Remove references from hashtable
                    items.Remove(id);
                }
            }
        }

        //public void ShowResults(MatchCollection results, IList<Tuple<string, decimal>> boosts = null)
        public string ShowResults(string dataString, int limit = 0, IList<Tuple<string, decimal>> boosts = null)
        {
            IList<Item> itemResults = this.tree.Find(dataString).ToList();

            //Clear items boosts if is WQUERY

            foreach (Item i in itemResults)
            {
                i.boost = 1.0M;
            }

            //.Take(limit)
            //.ToList();

            //Apply boosts
            if (boosts != null && boosts.Count > 0)
            {
                if (itemResults.Count > 0)
                {
                    foreach (Tuple<string, decimal> boost in boosts)
                    {
                        switch (boost.Item1)
                        {
                            case "user":
                            case "topic":
                            case "question":
                            case "board":
                                foreach (Item i in itemResults.Where(item => item.type == boost.Item1))
                                {
                                    i.boost *= boost.Item2;
                                }
                                break;
                            default:
                                Item tmpItem = itemResults.Where(item => item.id.Equals(boost.Item1)).FirstOrDefault();
                                if (tmpItem != null)
                                {
                                    tmpItem.score *= boost.Item2;
                                }
                                break;
                        }
                    }
                }
            }

            //Order and limit
            itemResults = itemResults
                .OrderByDescending(item => item.score * item.boost)
                .ThenByDescending(item => item.ObjectIdCreation)
                .Take(limit)
                .ToList();

            StringBuilder sb = new StringBuilder();
            foreach (string strId in itemResults.Select(item => item.id).ToList())
            {
                sb.Append(string.Format("{0} ", strId));
            }

            //Console.WriteLine(sb.ToString().Trim());

            return sb.ToString().Trim();
        }

        public static string CleanString(string inputString)
        {
            string tmp = inputString.ToLower();

            //Clean special characters
            //tmp = Regex.Replace(tmp, "[^\\w\\s]", "");

            tmp = tmp.Replace("\t", " ");

            //Remove double spaces
            tmp = Regex.Replace(tmp, "\\s{2,}", "");

            return tmp;
        }

        public bool Add(Item item)
        {
            //Method to add to the data our great data structure
            if (string.IsNullOrWhiteSpace(item.dataString))
            {
                return false;
            }

            string dataString = Program.CleanString(item.dataString);

            items.Add(item.id, item);
            try
            {
                //items.Add(item);
                string[] tokens = dataString.Split(' ');

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
            //StreamReader sr = new StreamReader(Console.);
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
        public Hashtable Children { get; set; }
        public List<Item> Documents { get; set; }

        public Node()
        {
            this.Documents = new List<Item>();
            this.Children = new Hashtable();
        }

    }

    public class QuickTree
    {
        private Hashtable root;
        public QuickTree()
        {
            this.root = new Hashtable();
        }

        public bool Add(string token, Item document)
        {
            token = Program.CleanString(token);
            char[] chars = token.ToArray<char>();
            Node iterator = null;
            Node prevNode = null;
            Hashtable currentNodeList = this.root;

            if (chars.Length > 0)
            {
                //Iterate through the tree and get the last node
                foreach (char c in chars)
                {
                    iterator = (Node)currentNodeList[c];
                    if (iterator == null)
                    {
                        iterator = new Node();
                        iterator.Letter = Char.ToUpper(c);
                        iterator.Parent = prevNode;

                        currentNodeList[c] = iterator;
                    }
                    prevNode = iterator;
                    currentNodeList = iterator.Children;

                    //Insert the document in the document list
                    iterator.Documents.Add(document);
                }
                return true;
            }

            return false;
        }

        public void Del(Item document)
        {
            //Get the item tokens to find them in the tree
            string[] tokens = document.dataString.Trim().Split(' ');
            foreach (string token in tokens)
            {
                Node iterator = null;
                Node prevNode = null;
                Hashtable currentNodeList = this.root;

                foreach (char c in token.ToArray<char>())
                {
                    iterator = (Node)currentNodeList[c];
                    if (iterator == null)
                        break;
                    prevNode = iterator;
                    currentNodeList = iterator.Children;

                    iterator.Documents.Remove(document);
                }
            }
        }

        public IList<Item> Find(string query, bool matchCase = false)
        {
            query = Program.CleanString(query);

            //Get the query tokens
            List<string> tokens = query.Trim().Split(' ').ToList<string>();

            //Temporary hold the documents found
            List<List<Item>> documentsList = new List<List<Item>>();

            //????
            char[] chars = null;
            Node iterator = null;
            Hashtable currentNodeList = null;

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
                            iterator = (Node)currentNodeList[Char.ToLower(c)];
                        }
                        else
                        {
                            iterator = (Node)currentNodeList[c];
                        }

                        //When iterator is null then the word wasn't found
                        if (iterator == null)
                        {
                            return new List<Item>();
                        }

                        currentNodeList = iterator.Children;
                    }

                    //The current node holds the node for the prefix
                    documentsList.Add(iterator.Documents);
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
                    .Distinct()
                    .ToList();
            }
            else if (tokens.Count == 2)
            {
                return this.Intersection(documentsList[0], documentsList[1])
                    .Distinct()
                    .ToList();
            }
            else
            {
                List<Item> tempList = documentsList[0];
                for (int i = 1; i < documentsList.Count; i++)
                {
                    tempList = tempList.Intersect(documentsList[i]).ToList();
                }
                return tempList.Distinct().ToList();
            }
        }

        private Node Navigate(string word, bool matchCase = false)
        {
            char[] chars = null;
            Node iterator = null;
            Hashtable currentNodeList = null;

            chars = word.ToArray<char>();
            iterator = null;
            currentNodeList = this.root;

            if (chars.Length > 0)
            {
                //Navigate through the tree and get the last node
                foreach (char c in chars)
                {
                    if (!matchCase)
                    {
                        iterator = (Node)currentNodeList[Char.ToUpper(c)];
                    }
                    else
                    {
                        iterator = (Node)currentNodeList[c];
                    }

                    if (iterator == null)
                    {
                        return null;
                    }

                    currentNodeList = iterator.Children;
                }

                return iterator;
            }

            return null;
        }

        private List<Item> Intersection(List<Item> listA, List<Item> listB)
        {
            return listA.Intersect(listB).ToList();
        }

    }

    #endregion
}
