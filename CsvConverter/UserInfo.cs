using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvConverter
{
    public class UserInfo
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Country { get; set; }
        public string IP_Address { get; set; }

        public override string ToString()
        {
            return $"{Firstname} {Lastname} {Email} {Country} {IP_Address}";
        }
    }
}
