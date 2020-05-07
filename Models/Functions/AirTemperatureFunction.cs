﻿using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF;

namespace Models.Functions
{
    /// <summary>
    /// A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures.  
    /// </summary>
    [Serializable]
    [Description("Interoplates Daily Min and Max temperatures out to sub daily values using the Interpolation Method, applyes a temperature response function and returns a daily agrigate")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class AirTemperatureFunction : Model, IFunction, ICustomDocumentation
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary> Method for interpolating Max and Min temperature to sub daily values </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IInterpolationMethod InterpolationMethod = null;

        /// <summary>The temperature response function applied to each sub daily temperature and averaged to give daily mean</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IIndexedFunction TemperatureResponse = null;

        /// <summary>Method used to agreagate sub daily values</summary>
        [Description("Method used to agregate sub daily temperature function")]
        public AgregationMethod agregationMethod { get; set; }

        /// <summary>Method used to agreagate sub daily values</summary>
        public enum AgregationMethod
        {
            /// <summary>Return average of sub daily values</summary>
            Average,
            /// <summary>Return sum of sub daily values</summary>
            Sum
        }

        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
        public List<double> SubDailyTemperatures = null;

        /// <summary>Temperatures interpolated to sub daily values from Tmin and Tmax</summary>
        public List<double> SubDailyResponse = null;

        /// <summary>Daily average temperature calculated from sub daily temperature interpolations</summary>
        public double Value(int arrayIndex = -1)
        {
            if (SubDailyResponse != null)
            {
                if (agregationMethod == AgregationMethod.Average)
                    return SubDailyResponse.Average();
                if (agregationMethod == AgregationMethod.Sum)
                    return SubDailyResponse.Sum();
                else
                    throw new Exception("invalid agregation method selected in " + this.Name + "temperature interpolation");
            }
            else
                return 0.0;
        }

        /// <summary> Set the sub dialy temperature values for the day then call temperature response function and set value for each sub daily period</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDailyInitialisation(object sender, EventArgs e)
        {
            SubDailyTemperatures = InterpolationMethod.SubDailyTemperatures();
            SubDailyResponse = new List<double>();
            foreach (double sdt in SubDailyTemperatures)
            {
                SubDailyResponse.Add(TemperatureResponse.ValueIndexed(sdt));
            }

        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write memos.
                foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);
            }
        }

    }

    /// <summary>
    /// A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures.  
    /// </summary>
    [Serializable]
    [Description("A value is calculated from the mean of 3-hourly estimates of air temperature based on daily max and min temperatures\n\n" +
        "Eight interpolations of the air temperature are calculated using a three-hour correction factor." +
        "For each air three-hour air temperature, a value is calculated.  The eight three-hour estimates" +
        "are then averaged to obtain the daily value.")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class ThreeHourSin : Model, IInterpolationMethod
    {
        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        /// <summary>Factors used to multiply daily range to give diurnal pattern of temperatures between Tmax and Tmin</summary>
        public List<double> TempRangeFactors = null;
        
        /// <summary>
        /// Calculate temperatures at 3 hourly intervals from min and max using sin curve
        /// </summary>
        /// <returns>list of 8 temperature estimates for 3 hourly periods</returns>
        public List<double> SubDailyTemperatures()
        {
            List<double> sdts = new List<Double>();
            double diurnal_range = MetData.MaxT - MetData.MinT;
            foreach (double trf in TempRangeFactors)
            {
                sdts.Add(MetData.MinT + trf * diurnal_range);
            }
            return sdts;
        }

        /// <summary> Set the sub daily temperature range factor values at sowing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            TempRangeFactors = t_range_fract();
        }

        /// <summary>Fraction_of of day's range_of for this 3 hr period</summary>
        public List<double> t_range_fract()
        {
            List<int> periods = Enumerable.Range(1, 8).ToList();
            List<double> trfs = new List<double>();
            // pre calculate t_range_fract for speed reasons
            foreach (int period in periods)
            {
                trfs.Add(0.92105
                        + 0.1140 * period
                        - 0.0703 * Math.Pow(period, 2)
                        + 0.0053 * Math.Pow(period, 3));
            }
            if (trfs.Count != 8)
                throw new Exception("Incorrect number of subdaily temperature estimations in " + this.Name + " temperature interpolation");
            return trfs;
        }
    }

    /// <summary>
    /// Junqi, write a summary here
    /// </summary>
    [Serializable]
    [Description("provide a description")]
    [ValidParent(ParentType = typeof(IFunction))]
    public class HourlySinPpAdjusted : Model, IInterpolationMethod
    {

        /// <summary>The met data</summary>
        [Link]
        protected IWeather MetData = null;

        private const double p = 1.5;

        private const double tc = 4.0;

        /// <summary>
        /// Temperature at the most recent sunset
        /// </summary>
        public double Tsset { get; set; }

        /// <summary> Set the sub daily temperature range factor values at sowing</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            Tsset = MetData.MinT + (MetData.MaxT - MetData.MinT) * 0.6;  //To start things off assum SunSet temperature on first day
        }

        /// <summary>Creates a list of temperature range factors used to estimate daily temperature from Min and Max temp</summary>
        /// <returns></returns>
        public List<double> SubDailyTemperatures()
        {
            double d = MetData.CalculateDayLength(-6);
            double Tmin = MetData.MinT;
            double Tmax = MetData.MaxT;
            int Hsrise = (int)Math.Round(MetData.CalculateSunRise());
            int Hsset = (int)Math.Round(MetData.CalculateSunSet());

            List<double> sdts = new List<double>();
            for (int Th = 0; Th <= 23; Th++)
            {
                double Ta = 1.0;
                if ((Th <= Hsrise) || (Th > Hsset))
                {//Use Nocturnal temperature function
                    double n = 24 - d;
                    double numerator = Tmin
                                      - Tsset * Math.Exp(-n / tc)
                                      + (Tsset - Tmin) * Math.Exp(-(Th - Hsset)/tc);
                    Ta =  numerator
                         /(1 - Math.Exp(-n / tc));
                }
                else
                {//Use Sin curve for daylight hours
                     Ta =   Tmin
                          + (Tmax - Tmin) 
                          * Math.Sin(Math.PI * (Th - 12 + d / 2) 
                                                  / (d + 2 * p));
                    
                    if (Th == MetData.CalculateSunSet())
                    {//Record sun set temperature for the upcomming nocturnal period
                        Tsset = Ta;
                    }
                }
                sdts.Add(Ta);
            }
            return sdts;
        }
    }

    /// <summary>An interface that defines what needs to be implemented by an organthat has a water demand.</summary>
    public interface IInterpolationMethod
    {
        /// <summary>Calculate temperature at specified periods during the day.</summary>
        List<double> SubDailyTemperatures();
    }

}