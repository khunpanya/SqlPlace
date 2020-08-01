using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject
{
    public class POCO
    {
        public int holder_id { get; set; }
        public string ref_id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string address { get; set; }
        public int? birthday_d { get; set; }
        public int? birthday_m { get; set; }
        public int? birthday_y { get; set; }

    }

    public class MyClass
    {
        public int f1 { get; set; }
        public string f2 { get; set; }
        public int f3 { get; set; }
    }
}
