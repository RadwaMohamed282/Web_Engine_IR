using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using mshtml;
using System.Data.SqlClient;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Porter2Stemmer;
using System.Xml;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Indexing
{
    public partial class Form1 : Form
    {
        public static SqlConnection connection = new SqlConnection(@"Data Source=DESKTOP-CDGBUN2;Initial Catalog=ir;User ID=sa;Password=123456");
        public static Queue<string> Parsed_Urls = new Queue<string>();

        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            Dictionary<string, Inverted_Index> Index_Row = new Dictionary<string, Inverted_Index>();
            Dictionary<string, string> Before_Stemming = new Dictionary<string, string>();
           // Dictionary<string, string> Unstemmed_Words = new Dictionary<string, string>();

            string[] arrToCheck = File.ReadAllLines(@"C:\Users\dell\source\repos\Indexing\Indexing\stop_words_english.txt");

            //-----------------------------------------Parseing HTML-----------------------------------------
            ///1) Parsing text
            /// Retrive the parsed Urls from Database and parse it 
            Retrive_Pages();
            int pages = 0;
            connection.Open();
            // MessageBox.Show("finsh1");
            //loop htmshe 3ala el URL ale at3mlohom parseing 
            Inverted_Index row;
            while (pages < Parsed_Urls.Count)
            {
                //connection.Open();
                //MessageBox.Show("Toko");
                //-----------------------------------------Tokenizatio----------------------------------------------
                ///2) Start Tokenizatio ==> split into spaces(" ") and (,)
                /// ElementAt ==> return the element into specific element 
                string parsed_page = Parsed_Urls.ElementAt(pages);

                ///split the page text
                ///splited_words_Array==> BY3ML split ll gomla kolha w y5znha f list 
                ///3ndena two list ==> 1) wa7da ll gomla b3d el tokonization ==> (splited_words)  2) w wa7da ll stop word ale hnshlha 
                string[] splited_words_Array = parsed_page.Split(' ', ',').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                List<string> splited_words = splited_words_Array.ToList();
                List<int> StopWordsIndex = new List<int>();

                var stemmer = new PorterStemmer();

                //loop htmshe 3ala el lista ale at3mlha tokonization ll document kolo 
                for (int i = 0; i < splited_words.Count; i++)
                {
                    //-------------------------------------Apply linguistics algorithms-------------------------------
                    ///1-Remove any punctuation character 
                    ///'-','(',')',';',':','!','_','^','*','&','$',"/",'?','.',',','"','{','}','[',']'
                    ///splited_words[i] = splited_words[i].Trim('-', '(', ')', ';','|','<','>', ':', '!', '_', '^', '*', '&', '$', '/', '?', '.', ',', '\'', '"', '{', '}', '[', ']');

                    var character = from ch in splited_words[i]
                                    where !Char.IsPunctuation(ch)
                                    select ch;
                    var bytes = UnicodeEncoding.ASCII.GetBytes(character.ToArray());
                    splited_words[i] = UnicodeEncoding.ASCII.GetString(bytes);

                    //2-CaseFolding => Make all chars small
                    string s = (splited_words[i]).ToLower();
                    splited_words[i] = s;

                    //Calculating the postion of text(doc)
                    //3-Removing stop words 
                    row = new Inverted_Index();
                    Boolean is_Stop_Word = false;
                    if (!splited_words[i].Equals("") || !string.IsNullOrWhiteSpace(splited_words[i]) || !string.IsNullOrEmpty(splited_words[i]))
                    {
                        foreach (string word in arrToCheck)
                        {
                            if (splited_words[i].Equals(word))
                            {
                                splited_words[i] = null;
                                is_Stop_Word = true;
                                break;
                            }
                        }
                    }
                    //if is not Stop_Word
                    if (is_Stop_Word != true)
                    {
                        int Docid;
                        if (!Before_Stemming.ContainsKey(splited_words[i]) && splited_words[i] != null)
                        {
                            Docid = pages + 1;
                            Before_Stemming.Add(splited_words[i], Docid.ToString());

                        }
                        else if (Before_Stemming.ContainsKey(splited_words[i]) && splited_words[i] != null)
                        {
                            Docid = pages + 1;
                            string value = Before_Stemming[splited_words[i]] + ("," + Docid.ToString());
                            Before_Stemming[splited_words[i]] = value;
                        }
                        // Do Stemming using Porter Stemmer
                        splited_words[i] = stemmer.StemWord(splited_words[i]);
                        splited_words[i] = splited_words[i].Trim('-', '(', ')', ';', '|', '<', '>', ':', '!', '_', '^', '*', '&', '$', '/', '?', '.', ',', '\'', '"', '{', '}', '[', ']');

                        row.Term = splited_words[i];
                        if (Index_Row.ContainsKey(splited_words[i])) 
                        { 
                            row.Frequency = Index_Row[row.Term].Frequency + 1;
                            row.DocID_Position = Index_Row[row.Term].DocID_Position + ("," + (pages + 1) + ':' + i);
                            row.DocID = Index_Row[row.Term].DocID + ("," + (pages + 1));
                            row.Position = Index_Row[row.Term].Position + ("," + i);
                            Index_Row[row.Term] = row;
                        }
                        else
                        {
                            row.Frequency = 1;
                            row.DocID_Position += "" + (pages + 1) + ':' + i;
                            row.DocID += "" + (pages + 1);
                            row.Position += "" + i;
                            Index_Row.Add(row.Term, row);
                        }
                    }
                  
                }
                for (int i = 0; i < Before_Stemming.Count; i++)
                {
                    try
                    {
                        string sql = "INSERT INTO Before_Stemming (Term , DocID) values('" + Before_Stemming.ElementAt(i).Key + "','" + Before_Stemming.ElementAt(i).Value + "') ";
                        SqlCommand cmd = new SqlCommand(sql, connection);
                        cmd.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex.Message);
                    }
                }
                for (int i = 0; i < Index_Row.Count; i++)
                {
                    if (Index_Row.ElementAt(i).Key.Equals(null) || Index_Row.ElementAt(i).Key.Equals(""))
                    {
                        Index_Row.Remove(Index_Row.ElementAt(i).Key);
                    }
                }
                listView1.Items.Add("" + (pages + 1) + "");
                pages++;
            }

            for (int i = 0; i < Index_Row.Count; i++)
            {
                try
                {
                    //connection.Open();
                    string sql = "INSERT INTO Stemming_Word(Term , DocID, Frequancy,Postion,[DocID:Postion]) values('" 
                            + Index_Row.ElementAt(i).Key + "','" + Index_Row.ElementAt(i).Value.DocID + "','" + Index_Row.ElementAt(i).Value.Frequency 
                            + "','" + Index_Row.ElementAt(i).Value.Position + "','" + Index_Row.ElementAt(i).Value.DocID_Position + "') ";
                    SqlCommand cmd = new SqlCommand(sql, connection);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
                //connection.Close();
            }
            connection.Close();
        }

        // Goza2 el parseing html text (Retrive_Pages,Parse_Pages)
        public static void Retrive_Pages()
        {
            try
            {
                connection.Open();
                //MessageBox.Show("X");
                string sql = "SELECT TOP 1500 [URL_Body] from Web_carwling";
                SqlCommand cmd = new SqlCommand(sql, connection);
                SqlDataReader reader = cmd.ExecuteReader();
                //HasRows ==>get values that indecates sql contains one or more row
                //ya3ne lw 3nde akter men row hya2rah ll URL bta3e 
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        //return specific coloum
                        //3shan a3ml parse l kol link men ale mwgoden 3nde f el DB
                        string original = WebUtility.HtmlDecode(reader.GetString(0));

                        // Call Parse Function return Inner text of page
                        // btl3 el text ele gowa el link dh w el function b3mlo parse 
                        string parsed_page = Parse_Pages(original);
                        parsed_page = parsed_page.Replace("\n", "").Replace("\r", "");
                        Parsed_Urls.Enqueue(parsed_page);
                    }
                }
                else
                {
                    MessageBox.Show("No Rows Found.");
                }
                reader.Close();
                connection.Close();
            }
            catch (Exception ex)
            { Console.Write(ex.Message); }
        }
        public static string Parse_Pages(string rString)
        {
            // Parse_Pages ==> btgeb el html file ele 3mlnalo save f el database(containt)  
            //btl3 el text men el URL ale ana msglah
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(rString);

            string ParsedPage = " ";
            StringBuilder sb = new StringBuilder();
            IEnumerable<HtmlNode> nodes = doc.DocumentNode.Descendants().Where(n =>
               n.NodeType == HtmlNodeType.Text &&
               n.ParentNode.Name != "script" &&
               n.ParentNode.Name != "style");
            //loop mshya 3ala el inner html document w btsgl kol 7aga f el html page swa space aw text 
            foreach (HtmlNode node in nodes)
            {
                if (!((node.InnerText).Equals("")) || !String.IsNullOrWhiteSpace(node.InnerText))
                {
                    ParsedPage += " ";
                    ParsedPage += node.InnerText;
                }
            }
            return ParsedPage;
        }
        ///STemmer class
        ///The Stemmer class transforms a word into its root form.
        /// Implementing the Porter Stemming Algorithm\
        /// <remarks>
        /// Modified from: http://tartarus.org/martin/PorterStemmer/csharp2.txt
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
        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
