using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication2.Models
{
    public class Inverted_Index
    {
        public string Term { get; set; }
        public string DocID_Position { get; set; }
        public string Frequency { get; set; }
        public string DocID { get; set; }
        public string Position { get; set; }
    }
}