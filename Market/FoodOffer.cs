using System;
using System.Collections.Generic;
using System.Text;

namespace eRepublikSitter.Market
{
    public class FoodOffer : IMarketOffer
    {
		public FoodOffer(int offerId, int countryId, int industryId, decimal price, int quantity, string companyName) 
		{
			this.OfferID = offerId;
			this.CountryID = countryId;
			this.IndustryID = industryId;
			this.Price = price;
			this.Quantity = quantity;
			this.CompanyName = companyName;
			
			if (!this.Validate())
				throw new Exception("Invalid offer details specified");
		}
		
		private bool Validate() 
		{
			return
			(
				(this.OfferID > 0) &&
				(this.CountryID > 0) &&
				(this.IndustryID > 0) &&
				(this.Price > 0) &&
				(this.Quantity > 0)
			);
		}
		
        public int OfferID { get; set; }
        public int CountryID { get; set; }
        public int IndustryID { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string CompanyName { get; set; }
    }
}