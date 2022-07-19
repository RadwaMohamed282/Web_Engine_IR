using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using mshtml;
using NTextCat;
using System.Data.SqlClient;
using HtmlAgilityPack;
using System.Windows.Forms;

namespace WinFormsApp13
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SqlConnection connection = new SqlConnection(@"Data Source=DESKTOP-CDGBUN2;Initial Catalog=ir;User ID=sa;Password=123456");

            //string URL = "http://www.cnn.com";
            Queue<string> Visited_Urls = new Queue<string>();
            Queue<string> UnVisited_Urls = new Queue<string>();
            int Link_Number = 1;
            //initial seed 
            UnVisited_Urls.Enqueue("http://www.wikipedia.com");
            //UnVisited_Urls.Enqueue("http://www.cnn.com");
            //UnVisited_Urls.Enqueue("http://www.bbc.com");

            while (Visited_Urls.Count < 3001)
            {
                string url = "";

                if (UnVisited_Urls != null || UnVisited_Urls.Count != 0)
                    url = UnVisited_Urls.Dequeue();

                // The response object of 'WebRequest' is assigned to a WebResponse' variable.
                WebResponse myWebResponse;
                string rString;
                string htmlEncoded;
                try
                {
                    // Create a new 'WebRequest' object to the mentioned URL
                    WebRequest myWebRequest = WebRequest.Create(url);
                    //Returns a response from an Internet resource
                    myWebResponse = myWebRequest.GetResponse();
                    //return the data stream from the internet 
                    Stream streamResponse = myWebResponse.GetResponseStream();
                    //reads the data stream
                    StreamReader sReader = new StreamReader(streamResponse);
                    //reads it to the end
                    rString = sReader.ReadToEnd();
                    //MessageBox.Show(rString);
                    htmlEncoded = WebUtility.HtmlEncode(rString);

                    streamResponse.Close();
                    sReader.Close();
                    myWebResponse.Close();
                }
                catch (Exception ex)
                {
                    listBox1.Items.Add(ex.Message);
                    continue;
                }

                //Checking on language
                var factory = new RankedLanguageIdentifierFactory();
                var identifier = factory.Load("Core14.profile.xml"); 
                var languages = identifier.Identify(rString);
                var mostCertainLanguage = languages.FirstOrDefault();
                if (mostCertainLanguage != null)
                {
                    // english then save the "body"
                    if (mostCertainLanguage.Item1.Iso639_3.Equals("eng"))
                    {
                        Visited_Urls.Enqueue(url);
                        try
                        {
                            connection.Open();
                            string sql = "INSERT INTO Web_carwling (URL_String , URL_Body) values(' " + url + " ' , '" + htmlEncoded.ToString() + "') ";
                            SqlCommand cmd = new SqlCommand(sql, connection);
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                        connection.Close();

                        // at first add reference to mshtml from solution explorer
                        //HTML Parser(URL==>HTML)
                        IHTMLDocument D = new HTMLDocumentClass();
                        IHTMLDocument2 myDoc = (IHTMLDocument2)D;
                        var M_doc = new HTMLDocumentClass();
                        myDoc.write(rString);
                        IHTMLElementCollection elements = myDoc.links;
                        //MessageBox.Show(elements.toString());
                        //extracting urls
                        foreach (IHTMLElement el in elements)
                        {
                            string link;
                            //check if link is vaild or not 
                            try
                            {
                                link = (string)el.getAttribute("href", 0);
                                link = link.Replace("about", "http");
                            }
                            catch (Exception ex)
                            {
                                continue;
                            }
                            //link Not repeated
                            //mfesh 7aga men el links mtkrra
                            bool IsVisited = Visited_Urls.Contains(link);
                            bool NotVisited = UnVisited_Urls.Contains(link);
                            if (IsVisited == false)
                            {
                                if (NotVisited == false)
                                {
                                    try
                                    {
                                        UnVisited_Urls.Enqueue(link);
                                        listBox1.Items.Add(link);
                                        //Console.WriteLine(link);
                                    }
                                    catch (Exception ex)
                                    {    
                                        //Console.WriteLine("Memory Exception");
                                        MessageBox.Show("Memory Exception");
                                    }

                                }
                                
                            }
                        }

                        listBox1.Items.Add("Link Number " + (Link_Number) + "");
                        Link_Number++;
                    }
                    else
                    {
                         MessageBox.Show("The language of the text is not English ==>"+ "Link Number " + (Link_Number) + "");
                         //MessageBox
                    }
                }
            }
            
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
