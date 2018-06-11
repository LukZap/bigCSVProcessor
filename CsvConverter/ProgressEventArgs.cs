using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvConverter
{
    public class ProgressEventArgs : EventArgs
    {
        public int Progress { get; set; }
    }
}
