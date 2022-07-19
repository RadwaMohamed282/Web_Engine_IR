using System;
using System.Collections.Generic;
using System.Text;

namespace Indexing
{
    class Inverted_Index
    {
        public string Term { get; set; }
        public string DocID_Position { get; set; }
        public int Frequency { get; set; }
        public string DocID { get; set; }
        public string Position { get; set; }
    }
}
