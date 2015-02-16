using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleGenerator
{
    class Program
    {
        StreamReader sr = new StreamReader("B:\\quorasample.txt");
        StringBuilder sb = new StringBuilder();
        Random r = new Random();

        int idUser = 1;
        int idTopic = 1;
        int idQuestion = 1;
        int idBoard = 1;

        int limit = 0;
        int boosts = 0;
        int numOfTokens = 0;
        decimal score = 0.0M;

        string[] types = new string[] { "user", "topic", "question", "board" };

        string type = string.Empty;
        string id = string.Empty;
        string dataString = string.Empty;

        List<string> tokens = null;

        //int numberOfInputs = r.Next(10000, 30000);
        int numberOfInputs = 71000;

        private string CreateAdd()
        {
            string commandInput = string.Empty;
            dataString = string.Empty;

            type = types[r.Next(0, 40) % 4];
            switch (type)
            {
                case "user":
                    id = "user#" + idUser++;
                    break;
                case "topic":
                    id = "topic#" + idTopic++;
                    break;
                case "question":
                    id = "question#" + idQuestion++;
                    break;
                case "board":
                    id = "board#" + idBoard++;
                    break;
            }

            //Define the length of the dataString
            int dsLength = r.Next(2, 10);

            //create the text
            for (int j = 0; j < dsLength; j++)
            {
                dataString += tokens[r.Next(0, tokens.Count - 1)] + " ";
            }

            score = r.Next(1, 10000) / 100.0M;

            commandInput = string.Format("ADD {0} {1} {2} {3}\n", type, id, score, dataString.Trim());

            return commandInput;
        }

        private string CreateDel()
        {
            string commandInput = string.Empty;

            type = types[r.Next(0, 3)];
            id = type + "#";
            if (type == "user")
                id = id + r.Next(1, idUser);
            if (type == "question")
                id = id + r.Next(1, idQuestion);
            if (type == "topic")
                id = id + r.Next(1, idTopic);
            if (type == "board")
                id = id + r.Next(1, idBoard);

            commandInput = string.Format("DEL {0}\n", id);
            return commandInput;
        }

        private string CreateQuery()
        {
            limit = r.Next(1, 25);
            numOfTokens = r.Next(1, 7);
            string[] ds = new string[numOfTokens];
            for (int j = 0; j < numOfTokens; j++)
            {
                int iTmp = r.Next(0, tokens.Count - 1);
                ds[j] = tokens[iTmp];
            }

            string commandInput = string.Format("QUERY {0} {1}\n", limit, string.Join(" ", ds));

            return commandInput;
        }

        private string CreateWQuery()
        {
            limit = r.Next(1, 25);
            boosts = r.Next(1, 25);
            decimal boostValue = 0M;
            List<string> boostsList = new List<string>();

            for (int j = 0; j < boosts; j++)
            {
                boostValue = r.Next(1, 10001) / 100M;

                //Check if boost is for type or ID
                if (r.Next(1, 20) % 2 == 0)
                {
                    //goes for type
                    type = types[r.Next(0, 40) % 4];
                    boostsList.Add(string.Format("{0}:{1:0.00}", type, boostValue));
                }
                else
                {
                    //Goes for ID
                    int iTmp = r.Next(1, 26) % 4;
                    id = types[iTmp] + "#";
                    switch (iTmp)
                    {
                        case 1:
                            id = id + r.Next(1, idUser).ToString();
                            break;
                        case 2:
                            id = id + r.Next(1, idTopic).ToString();
                            break;
                        case 3:
                            id = id + r.Next(1, idQuestion).ToString();
                            break;
                        case 4:
                            id = id + r.Next(1, idBoard).ToString();
                            break;
                    }

                    boostsList.Add(string.Format("{0}:{1:0.00}", id, boostValue));
                }
            }

            int iiTmp = r.Next(1, 7);
            dataString = string.Empty;
            for (int j = 0; j < iiTmp; j++)
            {
                dataString += string.Format("{0} ", tokens[r.Next(0, tokens.Count - 1)]);
            }

            string commandInput = string.Format("WQUERY {0} {1} {2} {3}\n", limit, boosts, string.Join(" ", boostsList.ToArray()).Trim(), dataString.Trim());

            return commandInput;
        }

        public void Run()
        {
            string sampleText = sr.ReadToEnd();
            sr.Close();
            tokens = sampleText.Split(' ').ToList<string>();

            sb.Append(numberOfInputs.ToString() + "\n");

            int i = 0;

            //Generate add's
            for (i = 0; i < 40000; i++)
            {
                sb.Append(CreateAdd());
            }

            //Generate del's
            for (i = 0; i < 10000; i++)
            {
                sb.Append(CreateDel());
            }

            //Generate query's
            for (i = 0; i < 20000; i++)
            {
                sb.Append(CreateQuery());
            }

            //Generate wquery's
            for (i = 0; i < 1000; i++)
            {
                sb.Append(CreateWQuery());
            }

            using (StreamWriter sw = new StreamWriter("B:\\quora.txt"))
            {
                sw.Write(sb.ToString().Trim());
            }

            Console.WriteLine("File generated...");
        }

        static void Main(string[] args)
        {
            new Program().Run();
        }
    }
}
