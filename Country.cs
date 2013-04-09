using System;
using System.Collections.Generic;
using System.Text;

namespace eRepublikSitter
{
    public class Country
    {
        public Country(string name, int id)
        {
            this.Name = name;
            this.ID = id;
        }

        public int ID { get; set; }
        public string Name { get; set; }
    }
}
