using System;

namespace eRepublikSitter.Wars
{
	public class Battle
	{
		public Battle(int warId, string regionName, int defensePoints)
		{
			this.WarID = warId;
			this.RegionName = regionName;
			this.DefensePoints = defensePoints;
			
			if (!this.Validate())
				throw new Exception("Invalid battle details specified");
		}
		
		private bool Validate() 
		{
			return 
			(
		        (this.WarID > 0) && 
		        (this.DefensePoints > 0) && 
		        !string.IsNullOrEmpty(this.RegionName)
			);
		}
		
		public int WarID { get; set; }
		public int DefensePoints { get; set; }
		public string RegionName { get; set; }
	}
}