﻿using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Graowing Forage Activity</summary>
	/// <summary>This activity sows, grows and harvests forages.</summary>
	/// <summary>This is done by the values entered by the user as well as looking up the file specified in the 
    /// FileAPSIMForage component in the simulation tree.</summary>
	/// <version>1.0</version>
	/// <updates>First implementation of this activity recreating IAT logic</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class IATGrowForage: WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		Clock Clock = null;
		//[Link]
		//ISummary Summary = null;
        [Link]
        FileAPSIMForage FileForage = null;



        /// <summary>
        /// Number for the Climate Region the forages are grown in.
        /// </summary>
        [Description("Climate Region Number")]
        public int Region { get; set; }


        /// <summary>
        /// Name of land type where forage is located
        /// </summary>
        [Description("Land type where forage is located")]
		public string LandTypeNameToUse { get; set; }

		/// <summary>
		/// Name of the forage type to grow
		/// </summary>
		[Description("Name of forage")]
		public string FeedTypeName { get; set; }

        /// <summary>
        /// Percentage of the residue (stover) that is kept
        /// </summary>
        [Description("Proportion of Residue (stover) Kept (%)")]
        public double ResidueKept { get; set; }


        /// <summary>
        /// Area of forage paddock
        /// </summary>
        [XmlIgnore]
		public double Area { get; set; }


		/// <summary>
		/// Area requested
		/// </summary>
		[Description("Area requested")]
		public double AreaRequested { get; set; }



        /// <summary>
        /// Land type
        /// </summary>
        [XmlIgnore]
        public LandType LinkedLandType { get; set; }


        /// <summary>
        /// Feed type
        /// </summary>
        [XmlIgnore]
		public AnimalFoodStoreType LinkedAnimalFoodType { get; set; }

        /// <summary>
        /// Harvest Data retrieved from the Forage File.
        /// </summary>
        [XmlIgnore]
        public List<ForageDataType> HarvestData { get; set; }


        private bool gotLandRequested = false; //was this forage able to get the land it requested ?

        /// <summary>
        /// Units of area to use for this run
        /// </summary>
        private UnitsOfAreaType unitsOfArea;



        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
            //get the units of area for this run from the Land resource.
            unitsOfArea = Resources.Land().UnitsOfArea; 

            // locate Land Type resource for this forage.
            // bool resourceAvailable = false;
            LinkedLandType = Resources.GetResourceItem(this, typeof(Land), LandTypeNameToUse, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as LandType;
            //if (LinkedLandType == null)
            //{
            //    throw new ApsimXException(this, String.Format("Unable to locate land type {0} in Land for {1}", this.LandTypeNameToUse, this.Name));
            //}



            // locate AnimalFoodStore Type resource for this forage.
            //bool resourceAvailable = false;
			LinkedAnimalFoodType = Resources.GetResourceItem(this, typeof(AnimalFoodStore), FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as AnimalFoodStoreType;
			//if (LinkedAnimalFoodType == null)
			//{
   //             throw new ApsimXException(this, String.Format("Unable to locate forage feed type {0} in AnimalFoodStore for {1}", this.FeedTypeName, this.Name));
			//}


            // Retrieve harvest data from the forage file for the entire run. 
            HarvestData = FileForage.GetForageDataForEntireRun(Region, LinkedLandType.SoilType, FeedTypeName, 
                                                               Clock.StartDate, Clock.EndDate);
            if (HarvestData == null)
            {
                throw new ApsimXException(this, String.Format("Unable to locate in forage file {0} any harvest data for Region {1} , SoilType {2}, ForageName {3} between the dates {4} and {5}", 
                    FileForage.FileName, Region, LinkedLandType.SoilType, FeedTypeName, Clock.StartDate, Clock.EndDate));
            }

        }




        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("WFInitialiseActivity")]
        private void OnWFInitialiseActivity(object sender, EventArgs e)
        {

            if (Area == 0 & AreaRequested > 0)
            {
                ResourceRequestList = new List<ResourceRequest>();
                ResourceRequestList.Add(new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = AreaRequested * (double)unitsOfArea,
                    ResourceType = typeof(Land),
                    ResourceTypeName = LandTypeNameToUse,
                    ActivityModel = this,
                    Reason = "Assign",
                    FilterDetails = null
                }
                );
            }

            gotLandRequested = TakeResources(ResourceRequestList);


            //Now the Land has been allocated we have an Area 
            if (gotLandRequested)
            {
                //Assign the area actually got after taking it. It might be less than AreaRequested (if partial)
                Area = ResourceRequestList.FirstOrDefault().Available; //TODO: should this be supplied not available ?
            }

        }



        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>A list of resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }



        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
		{
			return;
		}

		/// <summary>
		/// Method to determine resources required for initialisation of this activity
		/// </summary>
		/// <returns></returns>
		public override List<ResourceRequest> GetResourcesNeededForinitialisation()
		{
			return null;
		}

		/// <summary>
		/// Method used to perform initialisation of this activity.
		/// This will honour ReportErrorAndStop action but will otherwise be preformed regardless of resources available
		/// It is the responsibility of this activity to determine resources provided.
		/// </summary>
		public override void DoInitialisation()
		{
			return;
		}


		/// <summary>An event handler for a Cut and Carry</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFDoCutAndCarry")]
        private void OnWFDoCutAndCarry(object sender, EventArgs e)
        {
            int year = Clock.Today.Year;
            int month = Clock.Today.Month;

            //nb. we are relying on the fact that the HarvestData list is sorted by date.
            ForageDataType nextHarvest = HarvestData.FirstOrDefault();

            if (nextHarvest != null)
            {
                //if this month is a harvest month for this forage
                if ((year == nextHarvest.Year) && (month == nextHarvest.Month))
                {
                    
                    double amount = nextHarvest.Growth * Area *(double)unitsOfArea * (ResidueKept / 100);



					if (amount > 0)
					{
						FoodResourcePacket packet = new FoodResourcePacket()
						{
							Amount = amount,
							PercentN = nextHarvest.NPerCent
						};
						LinkedAnimalFoodType.Add(amount, this.Name, "Harvest");
					}

                    //now remove the first item from the harvest data list because it has happened
                    HarvestData.RemoveAt(0);  
                }
            }
        }



        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

		/// <summary>
		/// Shortfall occurred 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnShortfallOccurred(EventArgs e)
		{
			if (ResourceShortfallOccurred != null)
				ResourceShortfallOccurred(this, e);
		}
	}


    //TODO: sv- don't need this declaration here. It is done already in PastureActivityManage.cs
    //    but sure really move it from there to a common location.
     
	///// <summary>
	///// Types of units of erea to use.
	///// </summary>
	//public enum UnitsOfAreaTypes
	//{
	//	/// <summary>
	//	/// Square km
	//	/// </summary>
	//	Squarekm,
	//	/// <summary>
	//	/// Hectares
	//	/// </summary>
	//	Hectares
	//}
}
