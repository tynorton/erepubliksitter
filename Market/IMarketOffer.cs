using System;
using System.Collections.Generic;
using System.Text;

namespace eRepublikSitter.Market
{
    public interface IMarketOffer
    {	
        int OfferID { get; set; }
        int CountryID { get; set; }
        int IndustryID { get; set; }
        decimal Price { get; set; }
        int Quantity { get; set; }
        string CompanyName { get; set; }
    }
}