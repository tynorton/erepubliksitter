using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;
using eRepublikSitter.Market;

namespace eRepublikSitter
{
    public class Profile
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public List<IMarketOffer> Inventory;

        public void Load(CookieContainer cookieContainer)
        {
            if (this.ID <= 0) 
            	throw new Exception("ProfileID cannot be null");
			
            string profileUrl = string.Format("http://www.erepublik.com/en/citizen/profile/{0}", this.ID);
            string responseText = WebRequestManager.SendGenericWebRequest(profileUrl, cookieContainer);
			
			
        }
    }
}
