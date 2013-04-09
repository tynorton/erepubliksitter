using System;
using System.Collections.Generic;

namespace eRepublikSitter.Wars
{
	public class War
	{
		public War(int warId)
		{
			this.ID = warId;
			this.Attackers = new List<Country>();
			this.Defenders = new List<Country>();
			this.ActiveBattles = new List<Battle>();
			
			if (!this.Validate())
				throw new Exception("Invalid war details specified");
		}
		
		private bool Validate() 
		{
			return (this.ID > 0);
		}
		
		/// <value>
		/// This ID this war will be referenced by
		/// </value>
		public int ID { get; set; }
		
		/// <value>
		/// All countries allowed to attack in this war
		/// </value>
		public List<Country> Attackers { get; set; }
		
		/// <value>
		/// All countries allowed to defend in this war
		/// </value>
		public List<Country> Defenders { get; set; }
		
		/// <value>
		/// List of the current battles in this war
		/// </value>
		public List<Battle> ActiveBattles { get; set; }
		
		/// <value>
		/// returns true if there are currently battles that may be fought in
		/// </value>
		public bool HasActiveBattle 
		{ 
			get 
			{
				return this.ActiveBattles.Count > 0;
			}
		}
	}
}