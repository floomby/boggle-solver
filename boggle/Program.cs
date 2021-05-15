using DotNet.RestApi.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace boggle
{

    enum matchKind
    {
        START,
        FULL,
        INVALID
    }

    class Grid
    {
        static private string[] dict;

        private matchKind binSearch(int a, int b, string word)
        {
            if (a == b)
            {
                int tmp = string.Compare(word, dict[a]);
                if (tmp == 0) return matchKind.FULL;
                if (tmp < 0 && dict[a].Contains(word)) return matchKind.START;
                return matchKind.INVALID;
            }
            int idx = a + (b - a) / 2;
            //Console.WriteLine($"{a} {b} {idx} {dict[idx]} {word}");
            switch (string.Compare(word, dict[idx]))
            {
                case -1:
                    if (b - a == 1) return binSearch(a, a, word);
                    //if (dict[idx].Contains(word)) return matchKind.START;
                    return binSearch(a, idx, word);
                case 0:
                    return matchKind.FULL;
                case 1:
                    if (b - a == 1) return binSearch(b, b, word);
                    return binSearch(idx, b, word);
                default:
                    throw new SystemException("badness");
                    return matchKind.INVALID;
            }
        }

        private bool IsWordStart(string wordStart)
        {
            return binSearch(0, dict.Length - 1, wordStart) != matchKind.INVALID;
        }

        private bool IsWord(string word)
        {
            return binSearch(0, dict.Length - 1, word) == matchKind.FULL;
        }

        private string[] letters;

        public Grid(string[] letters, int dimX, int dimY)
        {
            if (dict == null)
            {
                dict = File.ReadAllLines("../../../../words.txt");
            }
            this.letters = letters;
            this.dimX = dimX;
            this.dimY = dimY;
        }

        private List<(int, int)> getUnvisitedNeighbors(int x, int y, bool[] visited)
        {
            List<(int, int)> ret = new List<(int, int)>();

            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (x + i < 0 || x + i >= dimX) continue;
                    if (y + j < 0 || y + j >= dimY) continue;
                    if (visited[(y + j) * dimX + (x + i)]) continue;
                    ret.Add((x + i, y + j));
                }
            }

            return ret;
        }

        private int dimX, dimY;

        public List<string> findWordHelper(int x, int y, bool[] visited, string wordStart)
        {
            visited[y * dimX + x] = true;
            wordStart += letters[y * dimX + x];
            if (!IsWordStart(wordStart)) return new List<string>();

            List<string> ret = new List<string>();
            if (IsWord(wordStart)) ret.Add(wordStart);

            foreach ((int, int) coordinate in getUnvisitedNeighbors(x, y, visited))
            {
                //Console.WriteLine(coordinate);
                bool[] visitedCopy = new bool[dimX * dimY];
                Array.Copy(visited, visitedCopy, dimY * dimY);
                string wordStartCopy = new string(wordStart);

                ret.AddRange(findWordHelper(coordinate.Item1, coordinate.Item2, visitedCopy, wordStartCopy));
            }

            return ret;
        }

        public List<string> findWord(int x, int y)
        {
            bool[] visited = new bool[dimX * dimY];

            return findWordHelper(x, y, visited, string.Empty);
        }

        public List<string> findEverything()
        {
            List<string> ret = new List<string>();

            for(int i = 0; i < dimX; i++)
            {
                for(int j = 0; j < dimY; j++)
                {
                    ret.AddRange(findWord(i, j));
                }
            }

            return ret;
        }

        public override string ToString()
        {
            string ret = string.Empty;
            for(int i = 0; i < dimY; i++)
            {
                for(int j = 0; j < dimX; j++)
                {
                    ret += letters[j * dimX + i] + " ";
                }
                ret += "\n";
            }
            ret = ret.Trim();
            return ret;
        }
    }

    // Data model (auto generated)
    public class Phonetic
    {
        public string text { get; set; }
        public string audio { get; set; }
    }

    public class Definition
    {
        public string definition { get; set; }
        public string example { get; set; }
        public List<string> synonyms { get; set; }
    }

    public class Meaning
    {
        public string partOfSpeech { get; set; }
        public List<Definition> definitions { get; set; }
    }

    public class Root
    {
        public string word { get; set; }
        public List<Phonetic> phonetics { get; set; }
        public List<Meaning> meanings { get; set; }
    }

    class OnlineDict
    {
        // random free dictionary api endpoint
        private static Uri uri = new Uri("https://api.dictionaryapi.dev/api/v2/entries/en_US/");
        private RestApiClient restApiClient;

        public OnlineDict()
        {
            restApiClient = new RestApiClient(uri);
        }

        public List<Root> lookup(string word)
        {
            HttpResponseMessage response = restApiClient.SendJsonRequest(HttpMethod.Get, new Uri(word, UriKind.Relative), null).Result;
            try
            {
                return JsonConvert.DeserializeObject<List<Root>>(response.Content.ReadAsStringAsync().Result);
            }
            catch
            {
                return new List<Root>();
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            OnlineDict dict = new OnlineDict();
            List<string> lines = File.ReadAllLines("../../../../board").ToList();

            int dimX = int.Parse(lines[0]);
            int dimY = int.Parse(lines[1]);
            lines.RemoveRange(0, 2);

            Grid test = new Grid(lines.ToArray(), dimX, dimY);
            Console.WriteLine(test);
            List<string> results = (List<string>)test.findEverything().Where(x => x.Length > 3).Distinct().OrderBy(x => x).ToList();
            foreach (string str in results)
            {
                Console.Write($"{str} - ");
                List<Root> tmp = dict.lookup(str);
                if (tmp.Count > 0  && tmp[0].meanings.Count > 0) Console.Write((tmp[0].meanings[0].definitions[0].definition));
                Console.WriteLine("");
            }
            Console.WriteLine($"count: {results.Count}");
        }
    }
}
