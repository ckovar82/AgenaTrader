using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using AgenaTrader.API;
using AgenaTrader.Custom;
using AgenaTrader.Plugins;
using AgenaTrader.Helper;


/// <summary>
/// Version: in progress
/// -------------------------------------------------------------------------
/// Simon Pucher 2016
/// Christian Kovar 2016
/// -------------------------------------------------------------------------
/// todo description: http://systemtradersuccess.com/golden-cross-which-is-the-best/
/// -------------------------------------------------------------------------
/// ****** Important ******
/// To compile this script without any error you also need access to the utility indicator to use these global source code elements.
/// You will find this indicator on GitHub: https://github.com/simonpucher/AgenaTrader/blob/master/Utility/GlobalUtilities_Utility.cs
/// -------------------------------------------------------------------------
/// Namespace holds all indicators and is required. Do not change it.
/// </summary>
namespace AgenaTrader.UserCode
{
    [Description("RunningWithTheWolves")]
	[IsEntryAttribute(true)]
	[IsStopAttribute(false)]
	[IsTargetAttribute(false)]
	[OverrulePreviousStopPrice(false)]
    public class RunningWithTheWolves_Condition : UserScriptedCondition
	{
		#region Variables

        //input 
        private Enum_RunningWithTheWolves_Indicator_MA _MA_Selected = Enum_RunningWithTheWolves_Indicator_MA.SMA;

        private int _ma_slow = 200;
        private int _ma_medium = 100;
        private int _ma_fast = 20;


        private bool _IsShortEnabled = true;
        private bool _IsLongEnabled = true;

        //output
		
        //internal
        private RunningWithTheWolves_Indicator _RunningWithTheWolves_Indicator = null;


		#endregion

		protected override void Initialize()
		{
			IsEntry = true;
			IsStop = false;
			IsTarget = false;
			Add(new Plot(Const.DefaultIndicatorColor, "Occurred"));
            Add(new Plot(Const.DefaultIndicatorColor, "Entry"));

			Overlay = false;
			CalculateOnBarClose = true;

            //For SMA200 we need at least 200 Bars.
            this.BarsRequired = 200;
		}

        protected override void InitRequirements()
        {
            base.InitRequirements();

        }


        protected override void OnStartUp()
        {
            base.OnStartUp();

            //Init our indicator to get code access
            this._RunningWithTheWolves_Indicator = new RunningWithTheWolves_Indicator();
        }

       

		protected override void OnBarUpdate()
		{

            //calculate data
            OrderAction? resultdata = this._RunningWithTheWolves_Indicator.calculate(Input, this.MA_Selected, this.MA_Fast, this.MA_Medium, this.MA_Slow);
            if (resultdata.HasValue)
            {
                switch (resultdata)
                {
                    case OrderAction.Buy:
                        Occurred.Set(1);
                        //Entry.Set(Input[0]);
                        break;
                    case OrderAction.SellShort:
                        Occurred.Set(-1);
                        //Entry.Set(Input[0]);
                        break;
                    //case OrderAction.BuyToCover:
                    //    break;
                    //case OrderAction.Sell:
                    //    break;
                    default:
                        //nothing to do
                        Occurred.Set(0);
                        //Entry.Set(Input[0]);
                        break;
                }
            }
            else
            {
                Occurred.Set(0);
            }

		}


        public override string ToString()
        {
            return "Running with the wolves (C)";
        }

        public override string DisplayName
        {
            get
            {
                return "Running with the wolves (C)";
            }
        }

     


        #region Properties


        #region Input


        /// <summary>
        /// </summary>
        [Description("Select the type of MA you would like to use")]
        [Category("Parameters")]
        [DisplayName("Type of MA")]
        public Enum_RunningWithTheWolves_Indicator_MA MA_Selected
        {
            get { return _MA_Selected; }
            set
            {
                _MA_Selected = value;
            }
        }


        /// <summary>
        /// </summary>
        [Description("Period for the slow mean average")]
        [Category("Parameters")]
        [DisplayName("MA Slow")]
        public int MA_Slow
        {
            get { return _ma_slow; }
            set
            {
                _ma_slow = value;
            }
        }

        /// <summary>
        /// </summary>
        [Description("Period for the medium mean average")]
        [Category("Parameters")]
        [DisplayName("MA Medium")]
        public int MA_Medium
        {
            get { return _ma_medium; }
            set
            {
                _ma_medium = value;
            }
        }

        /// <summary>
        /// </summary>
        [Description("Period for the fast mean average")]
        [Category("Parameters")]
        [DisplayName("MA Fast")]
        public int MA_Fast
        {
            get { return _ma_fast; }
            set
            {
                _ma_fast = value;
            }
        }


        /// <summary>
        /// </summary>
        [Description("If true it is allowed to go long")]
        [Category("Parameters")]
        [DisplayName("Allow Long")]
        public bool IsLongEnabled
        {
            get { return _IsLongEnabled; }
            set { _IsLongEnabled = value; }
        }


        /// <summary>
        /// </summary>
        [Description("If true it is allowed to go short")]
        [Category("Parameters")]
        [DisplayName("Allow Short")]
        public bool IsShortEnabled
        {
            get { return _IsShortEnabled; }
            set { _IsShortEnabled = value; }
        }

   

        #endregion


            #region Output

                [Browsable(false)]
                [XmlIgnore()]
                public DataSeries Occurred
                {
                    get { return Values[0]; }
                }

                [Browsable(false)]
                [XmlIgnore()]
                public DataSeries Entry
                {
                    get { return Values[1]; }
                }

                public override IList<DataSeries> GetEntries()
                {
                    return new[] { Entry };
                }

              
            #endregion

        #region Internals



        #endregion


        #endregion
    }
}
