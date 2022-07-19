using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq;
using System.Web;
using System.IO;
using System.Data.SqlClient;
using WebApplication2.Models;
using System.Web.Mvc;

namespace WebApplication2.Controllers
{
    public class SearchingController : Controller
    {
        // GET: Searching
        public static SqlConnection connection = new SqlConnection(@"Data Source=DESKTOP-CDGBUN2;Initial Catalog=ir;User ID=sa;Password=123456");

        static Dictionary<string, Inverted_Index> Index_Row = new Dictionary<string, Inverted_Index>();
        Dictionary<string, string> Unstemmed_Words = new Dictionary<string, string>();
        static List<string[]> Index_pos_Doc = new List<string[]>();
        static List<int> Command_Link = new List<int>();
        static List<string> links = new List<string>();
        public ActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public ActionResult Search()
        {
            @ViewBag.term = "";
            return View();
        }
        [HttpPost]
        public ActionResult Search(Inverted_Index model)
        {
            if (ModelState.IsValid)
            {
                //Index_Row.Clear();
                List<string> url = new List<string>();
                url = show_link(model.Term);
                @ViewBag.term = url;
                url.Clear();
            }
            return View();
        }
        public static List<string> show_link(string Word_input)
        {
            Inverted_Index row = new Inverted_Index();
            string[] arrToCheck = System.IO.File.ReadAllLines(@"C:\Users\dell\source\repos\Indexing\Indexing\stop_words_english.txt");
            // Start Tokenization  
            //split the page text
            bool exact_flag = false;
            if (Word_input.Contains("\""))
            {
                //Exact Search
                exact_flag = true;
            }
            else
            {
                //Multikey Word
                exact_flag = false;
            }

            string[] splited_words_Array = Word_input.Split(' ', ',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            List<string> splited_words = splited_words_Array.ToList();
            List<int> StopWordsIndex = new List<int>();

            var stemmer = new PorterStemmer();

            for (int i = 0; i < splited_words.Count; i++)
            {
                //Console.WriteLine(splited_words_Array[i]);
                //Remove any punctuation character 
                //'-','(',')',';',':','!','_','^','*','&','$',"/",'?','.',',','"','{','}','[',']'
                //splited_words[i] = splited_words[i].Trim('-', '(', ')', ';','|','<','>', ':', '!', '_', '^', '*', '&', '$', '/', '?', '.', ',', '\'', '"', '{', '}', '[', ']');

                var character = from ch in splited_words[i]
                                where !Char.IsPunctuation(ch)
                                select ch;
                var bytes = UnicodeEncoding.ASCII.GetBytes(character.ToArray());
                splited_words[i] = UnicodeEncoding.ASCII.GetString(bytes);

                // CaseFolding => Make all chars small
                string s = (splited_words[i]).ToLower();
                splited_words[i] = s;

                //Calculating the postion 
                // Removing stop words 
                
                if (!splited_words[i].Equals("") || !string.IsNullOrWhiteSpace(splited_words[i]) || !string.IsNullOrEmpty(splited_words[i]))
                {
                    foreach (string word in arrToCheck)
                    {
                        if (splited_words[i].Equals(word))
                        {
                            splited_words[i] = null;
                            break;
                        }
                    }
                }
                // Do Stemming using Porter Stemmer
                splited_words[i] = stemmer.StemWord(splited_words[i]);
                //splited_words[i] = splited_words[i].Trim('-', '(', ')', ';', '|', '<', '>', ':', '!', '_', '^', '*', '&', '$', '/', '?', '.', ',', '\'', '"', '{', '}', '[', ']');
                row.Term = splited_words[i];
            }
            splited_words.RemoveAll(item => item == null);
            splited_words.RemoveAll(item => item == "");

            //Retrive DOCID from Database   
            Retrive_DocID(splited_words);

            int counter = 0;
            string[] Doc = new string[10000];
            //Index_Row = 1 ==> input search one word  
            if (Index_Row.Count == 1)
            {
                for (int k = 0; k < Index_Row.Count; k++)
                {
                    Doc = Index_Row[splited_words[k]].DocID.Split(',');
                }
                for (int j = 0; j < Doc.Length; j++)
                {
                    //string[] word1 = (Index_pos_Doc[0].ElementAt(j)).Split(',');
                    string word1 = Doc[j];
                    if (!Command_Link.Contains(Int32.Parse(word1)))
                    {
                        Command_Link.Add(Int32.Parse(word1));
                    }
                }
            }
            else
            {
                string[] word2, word1, position1, position2;
                //return min postion from multikey word
                Dictionary<string, int> min_pos_word = new Dictionary<string, int>();
                //return max frequance from exact search
                Dictionary<int, string> Sort_Freq = new Dictionary<int, string>();
                Dictionary<string, int> Sort_Max_Freq = new Dictionary<string, int>();
                if (exact_flag == false)
                {
                    // Multikey word
                    for (int k = 0; k < Index_Row.Count; k++)
                    {
                        //int count = 1000000000;
                        if (k == Index_Row.Count - 1)
                            break;
                        word1 = Index_Row.ElementAt(k).Value.DocID.Split(',');//computer
                        word2 = Index_Row.ElementAt(k + 1).Value.DocID.Split(',');//science
                        position1 = Index_Row.ElementAt(k).Value.Position.Split(',');
                        position2 = Index_Row.ElementAt(k + 1).Value.Position.Split(',');
                        for (int j = 0; j < word1.Length; j++)
                        {
                            for (int i = 0; i < word2.Length; i++)
                            {
                                if (j <= word2.Length - 1 && word1[j] == word2[i])
                                {
                                    if (Int32.Parse(position1[j]) < Int32.Parse(position2[i]))
                                    {
                                        int pos = Int32.Parse(position2[i]) - Int32.Parse(position1[j]);
                                        //Result.Add(pos);
                                        if (!min_pos_word.ContainsKey(word1[j]))
                                        {
                                            min_pos_word.Add(word1[j], pos);
                                        }
                                    }
                                    else
                                        continue;
                                }
                                else
                                {
                                    if (Int32.Parse(word1[j]) > Int32.Parse(word2[i]))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    //(Sorting by min position)Order By min distance between two position 
                    foreach (var item in min_pos_word.OrderBy(x => x.Value))
                    {
                        //save ID into Command list to Retrive Links 
                        Command_Link.Add(Int32.Parse(item.Key));
                    }
                }
                else if (exact_flag == true)
                {
                    int count_frequancy = 1;
                    for (int k = 0; k < Index_Row.Count; k++)
                    {
                        if (k == Index_Row.Count - 1)
                            break;
                        word1 = Index_Row.ElementAt(k).Value.DocID.Split(',');
                        word2 = Index_Row.ElementAt(k + 1).Value.DocID.Split(',');
                        position1 = Index_Row.ElementAt(k).Value.Position.Split(',');
                        position2 = Index_Row.ElementAt(k + 1).Value.Position.Split(',');
                        for (int j = 0; j < word1.Length; j++)
                        {
                            for (int i = 0; i < word2.Length; i++)
                            {
                                if (j <= word2.Length - 1 && word1[j] == word2[i])
                                {
                                    if (Int32.Parse(position1[j]) < Int32.Parse(position2[i]) && (Int32.Parse(position2[i]) - Int32.Parse(position1[j]) == 1))
                                    {
                                        int pos = Int32.Parse(position2[i]) - Int32.Parse(position1[j]);
                                        Sort_Freq.Add(j, word1[j]);
                                    }
                                    else
                                        continue;
                                }
                                else
                                {
                                    if (Int32.Parse(word1[j]) > Int32.Parse(word2[i]))
                                        continue;
                                    else
                                    {
                                        break;
                                    }
                                }
                            }
                            //count_frequancy = 1;
                        }
                        for (int i = 0; i < Sort_Freq.Count; i++)
                        {
                            if (!Sort_Max_Freq.ContainsKey(word1[i]))
                            {
                                Sort_Max_Freq.Add(word1[i], count_frequancy);
                                count_frequancy = 1;
                            }
                            else
                            {
                                Sort_Max_Freq.Remove(word1[i]);
                                count_frequancy++;
                                Sort_Max_Freq.Add(word1[i], count_frequancy);
                            }
                        }
                        //sorting Dictionary by frequancy
                        var Sorting_by_freq = Sort_Max_Freq.ToList();
                        Sorting_by_freq.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

                        for (int x = 0; x < Sorting_by_freq.Count; x++)
                        {
                            Command_Link.Add(Int32.Parse(Sorting_by_freq[x].Key));
                        }
                    }
                }
            }
            //Retrive Link from Database   
            links = Retrive_Links(Command_Link);
            return links;
        }

        //Porter Stemmer class
        public class PorterStemmer
        {
            // The passed in word turned into a char array. 
            // Quicker to use to rebuilding strings each time a change is made.
            private char[] wordArray;

            // Current index to the end of the word in the character array. This will
            // change as the end of the string gets modified.
            private int endIndex;

            // Index of the (potential) end of the stem word in the char array.
            private int stemIndex;

            /// <summary>
            /// Stem the passed in word.
            /// </summary>
            /// <param name="word">Word to evaluate</param>
            /// <returns></returns>
            public string StemWord(string word)
            {

                // Do nothing for empty strings or short words.
                if (string.IsNullOrWhiteSpace(word) || word.Length <= 2) return word;

                wordArray = word.ToCharArray();

                stemIndex = 0;
                endIndex = word.Length - 1;
                Step1();
                Step2();
                Step3();
                Step4();
                Step5();
                Step6();

                var length = endIndex + 1;
                return new String(wordArray, 0, length);
            }


            // Step1() gets rid of plurals and -ed or -ing.
            /* Examples:
                   caresses  ->  caress
                   ponies    ->  poni
                   ties      ->  ti
                   caress    ->  caress
                   cats      ->  cat

                   feed      ->  feed
                   agreed    ->  agree
                   disabled  ->  disable

                   matting   ->  mat
                   mating    ->  mate
                   meeting   ->  meet
                   milling   ->  mill
                   messing   ->  mess

                   meetings  ->  meet  		*/
            private void Step1()
            {
                // If the word ends with s take that off
                if (wordArray[endIndex] == 's')
                {
                    if (EndsWith("sses"))
                    {
                        endIndex -= 2;
                    }
                    else if (EndsWith("ies"))
                    {
                        SetEnd("i");
                    }
                    else if (wordArray[endIndex - 1] != 's')
                    {
                        endIndex--;
                    }
                }
                if (EndsWith("eed"))
                {
                    if (MeasureConsontantSequence() > 0)
                        endIndex--;
                }
                else if ((EndsWith("ed") || EndsWith("ing")) && VowelInStem())
                {
                    endIndex = stemIndex;
                    if (EndsWith("at"))
                        SetEnd("ate");
                    else if (EndsWith("bl"))
                        SetEnd("ble");
                    else if (EndsWith("iz"))
                        SetEnd("ize");
                    else if (IsDoubleConsontant(endIndex))
                    {
                        endIndex--;
                        int ch = wordArray[endIndex];
                        if (ch == 'l' || ch == 's' || ch == 'z')
                            endIndex++;
                    }
                    else if (MeasureConsontantSequence() == 1 && IsCVC(endIndex)) SetEnd("e");
                }
            }

            // Step2() turns terminal y to i when there is another vowel in the stem.
            private void Step2()
            {
                if (EndsWith("y") && VowelInStem())
                    wordArray[endIndex] = 'i';
            }

            // Step3() maps double suffices to single ones. so -ization ( = -ize plus
            // -ation) maps to -ize etc. note that the string before the suffix must give m() > 0. 
            private void Step3()
            {
                if (endIndex == 0) return;

                /* For Bug 1 */
                switch (wordArray[endIndex - 1])
                {
                    case 'a':
                        if (EndsWith("ational")) { ReplaceEnd("ate"); break; }
                        if (EndsWith("tional")) { ReplaceEnd("tion"); }
                        break;
                    case 'c':
                        if (EndsWith("enci")) { ReplaceEnd("ence"); break; }
                        if (EndsWith("anci")) { ReplaceEnd("ance"); }
                        break;
                    case 'e':
                        if (EndsWith("izer")) { ReplaceEnd("ize"); }
                        break;
                    case 'l':
                        if (EndsWith("bli")) { ReplaceEnd("ble"); break; }
                        if (EndsWith("alli")) { ReplaceEnd("al"); break; }
                        if (EndsWith("entli")) { ReplaceEnd("ent"); break; }
                        if (EndsWith("eli")) { ReplaceEnd("e"); break; }
                        if (EndsWith("ousli")) { ReplaceEnd("ous"); }
                        break;
                    case 'o':
                        if (EndsWith("ization")) { ReplaceEnd("ize"); break; }
                        if (EndsWith("ation")) { ReplaceEnd("ate"); break; }
                        if (EndsWith("ator")) { ReplaceEnd("ate"); }
                        break;
                    case 's':
                        if (EndsWith("alism")) { ReplaceEnd("al"); break; }
                        if (EndsWith("iveness")) { ReplaceEnd("ive"); break; }
                        if (EndsWith("fulness")) { ReplaceEnd("ful"); break; }
                        if (EndsWith("ousness")) { ReplaceEnd("ous"); }
                        break;
                    case 't':
                        if (EndsWith("aliti")) { ReplaceEnd("al"); break; }
                        if (EndsWith("iviti")) { ReplaceEnd("ive"); break; }
                        if (EndsWith("biliti")) { ReplaceEnd("ble"); }
                        break;
                    case 'g':
                        if (EndsWith("logi"))
                        {
                            ReplaceEnd("log");
                        }
                        break;
                }
            }

            /* step4() deals with -ic-, -full, -ness etc. similar strategy to step3. */
            private void Step4()
            {
                switch (wordArray[endIndex])
                {
                    case 'e':
                        if (EndsWith("icate")) { ReplaceEnd("ic"); break; }
                        if (EndsWith("ative")) { ReplaceEnd(""); break; }
                        if (EndsWith("alize")) { ReplaceEnd("al"); }
                        break;
                    case 'i':
                        if (EndsWith("iciti")) { ReplaceEnd("ic"); }
                        break;
                    case 'l':
                        if (EndsWith("ical")) { ReplaceEnd("ic"); break; }
                        if (EndsWith("ful")) { ReplaceEnd(""); }
                        break;
                    case 's':
                        if (EndsWith("ness")) { ReplaceEnd(""); }
                        break;
                }
            }

            /* step5() takes off -ant, -ence etc., in context <c>vcvc<v>. */
            private void Step5()
            {
                if (endIndex == 0) return;

                switch (wordArray[endIndex - 1])
                {
                    case 'a':
                        if (EndsWith("al")) break; return;
                    case 'c':
                        if (EndsWith("ance")) break;
                        if (EndsWith("ence")) break; return;
                    case 'e':
                        if (EndsWith("er")) break; return;
                    case 'i':
                        if (EndsWith("ic")) break; return;
                    case 'l':
                        if (EndsWith("able")) break;
                        if (EndsWith("ible")) break; return;
                    case 'n':
                        if (EndsWith("ant")) break;
                        if (EndsWith("ement")) break;
                        if (EndsWith("ment")) break;
                        /* element etc. not stripped before the m */
                        if (EndsWith("ent")) break; return;
                    case 'o':
                        if (EndsWith("ion") && stemIndex >= 0 && (wordArray[stemIndex] == 's' || wordArray[stemIndex] == 't')) break;
                        /* j >= 0 fixes Bug 2 */
                        if (EndsWith("ou")) break; return;
                    /* takes care of -ous */
                    case 's':
                        if (EndsWith("ism")) break; return;
                    case 't':
                        if (EndsWith("ate")) break;
                        if (EndsWith("iti")) break; return;
                    case 'u':
                        if (EndsWith("ous")) break; return;
                    case 'v':
                        if (EndsWith("ive")) break; return;
                    case 'z':
                        if (EndsWith("ize")) break; return;
                    default:
                        return;
                }
                if (MeasureConsontantSequence() > 1)
                    endIndex = stemIndex;
            }

            /* step6() removes a final -e if m() > 1. */
            private void Step6()
            {
                stemIndex = endIndex;

                if (wordArray[endIndex] == 'e')
                {
                    var a = MeasureConsontantSequence();
                    if (a > 1 || a == 1 && !IsCVC(endIndex - 1))
                        endIndex--;
                }
                if (wordArray[endIndex] == 'l' && IsDoubleConsontant(endIndex) && MeasureConsontantSequence() > 1)
                    endIndex--;
            }

            // Returns true if the character at the specified index is a consonant.
            // With special handling for 'y'.
            private bool IsConsonant(int index)
            {
                var c = wordArray[index];
                if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u') return false;
                return c != 'y' || (index == 0 || !IsConsonant(index - 1));
            }

            /* m() measures the number of consonant sequences between 0 and j. if c is
               a consonant sequence and v a vowel sequence, and <..> indicates arbitrary
               presence,

                  <c><v>       gives 0
                  <c>vc<v>     gives 1
                  <c>vcvc<v>   gives 2
                  <c>vcvcvc<v> gives 3
                  ....		*/
            private int MeasureConsontantSequence()
            {
                var n = 0;
                var index = 0;
                while (true)
                {
                    if (index > stemIndex) return n;
                    if (!IsConsonant(index)) break; index++;
                }
                index++;
                while (true)
                {
                    while (true)
                    {
                        if (index > stemIndex) return n;
                        if (IsConsonant(index)) break;
                        index++;
                    }
                    index++;
                    n++;
                    while (true)
                    {
                        if (index > stemIndex) return n;
                        if (!IsConsonant(index)) break;
                        index++;
                    }
                    index++;
                }
            }

            // Return true if there is a vowel in the current stem (0 ... stemIndex)
            private bool VowelInStem()
            {
                int i;
                for (i = 0; i <= stemIndex; i++)
                {
                    if (!IsConsonant(i)) return true;
                }
                return false;
            }

            // Returns true if the char at the specified index and the one preceeding it are the same consonants.
            private bool IsDoubleConsontant(int index)
            {
                if (index < 1) return false;
                return wordArray[index] == wordArray[index - 1] && IsConsonant(index);
            }

            /* cvc(i) is true <=> i-2,i-1,i has the form consonant - vowel - consonant
               and also if the second c is not w,x or y. this is used when trying to
               restore an e at the end of a short word. e.g.

                  cav(e), lov(e), hop(e), crim(e), but
                  snow, box, tray.		*/
            private bool IsCVC(int index)
            {
                if (index < 2 || !IsConsonant(index) || IsConsonant(index - 1) || !IsConsonant(index - 2)) return false;
                var c = wordArray[index];
                return c != 'w' && c != 'x' && c != 'y';
            }

            // Does the current word array end with the specified string.
            private bool EndsWith(string s)
            {
                var length = s.Length;
                var index = endIndex - length + 1;
                if (index < 0) return false;

                for (var i = 0; i < length; i++)
                {
                    if (wordArray[index + i] != s[i]) return false;
                }
                stemIndex = endIndex - length;
                return true;
            }

            // Set the end of the word to s.
            // Starting at the current stem pointer and readjusting the end pointer.
            private void SetEnd(string s)
            {
                var length = s.Length;
                var index = stemIndex + 1;
                for (var i = 0; i < length; i++)
                {
                    wordArray[index + i] = s[i];
                }
                // Set the end pointer to the new end of the word.
                endIndex = stemIndex + length;
            }

            // Conditionally replace the end of the word
            private void ReplaceEnd(string s)
            {
                if (MeasureConsontantSequence() > 0) SetEnd(s);
            }
        }

        public static void Retrive_DocID(List<string> Words)
        {
            connection.Open();
            try
            {
                for (int i = 0; i < Words.Count; i++)
                {
                    string sql = "SELECT * from Stemming_Word where Term ='" + Words[i] + "'";
                    SqlCommand cmd = new SqlCommand(sql, connection);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            //Retrive all data from sql base on input word and save in Dictionary
                            Inverted_Index row = new Inverted_Index();
                            row.Term = reader.GetString(0);
                            row.DocID = reader.GetString(1);
                            row.Frequency = reader.GetString(2);
                            row.Position = reader.GetString(3);
                            row.DocID_Position = reader.GetString(4);
                            Index_Row.Add(row.Term, row);
                        }
                        reader.Close();
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                        connection.Close();
                    }
                }
               // Console.WriteLine(Words.Count);
            }
            catch (Exception ex)
            { Console.WriteLine(ex.Message); }
            connection.Close();
        }
        public static List<string> Retrive_Links(List<int> ID)
        {
            List<string> link = new List<string>();
            try
            {
                connection.Open();
                for (int i = 0; i < ID.Count; i++)
                {
                    string sql = "SELECT [URL_String] from Web_carwling where ID ='" + ID[i] + "'";
                    Console.WriteLine(sql);
                    SqlCommand cmd = new SqlCommand(sql, connection);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            //Retrive links from Crawler  base on DOCID 
                            link.Add(reader.GetString(0));
                        }
                    }
                    else
                    {
                        Console.WriteLine("No rows found.");
                    }
                    reader.Close();
                }
                connection.Close();
                return link;

            }
            catch (Exception ex)
            { Console.WriteLine(ex.Message); }
            connection.Close();
            return null;
        }  
    }
}