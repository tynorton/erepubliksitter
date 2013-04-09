using System;
using System.Collections.Generic;
using System.Text;

namespace eRepublikSitter
{
    public class UserDetails
    {
        public string CitizenName { get; set; }
        public string Password { get; set; }

        public bool IsPopulated()
        {
            return (!string.IsNullOrEmpty(this.CitizenName) && !string.IsNullOrEmpty(this.Password));
        }
    }
}
