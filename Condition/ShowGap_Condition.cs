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
/// Version: 1.0
/// -------------------------------------------------------------------------
/// Simon Pucher 2016
/// Christian Kovar 2016
/// -------------------------------------------------------------------------
/// ToDo
/// 
/// 
/// 
/// -------------------------------------------------------------------------
/// Namespace holds all indicators and is required. Do not change it.
/// </summary>
namespace AgenaTrader.UserCode
{
    [Description("Geben Sie bitte hier die Beschreibung für die neue Condition ein")]
    [IsEntryAttribute(true)]
    [IsStopAttribute(false)]
    [IsTargetAttribute(false)]
    [OverrulePreviousStopPrice(false)]
    public class ShowGap_Condition : UserScriptedCondition
    {
        #region Variables

        //private int _myCondition1 = 1;

        int _PunkteGapMin = 50;
        int _PunkteGapMax = 100;
        private Color _col_gap = Color.Turquoise;
        private Color colWin = Color.Yellow;
        private Color colFail = Color.Brown;
        bool existgap;
        bool GapTradeShort;
        bool GapTradeLong;
        bool sessionprocessed;
        decimal GapTradeCounterShort;
        decimal GapTradeCounterLong;
        decimal GapTradeResultTotalShort;
        decimal GapTradeResultTotalLong;
        decimal GapTradeFailCounterShort;
        decimal GapTradeFailCounterLong;
        decimal GapTradeWinCounterShort;
        decimal GapTradeWinCounterLong;
        double GapOpen;
        double GapSize;

        #endregion

        protected override void Initialize()
        {
            IsEntry = true;
            IsStop = false;
            IsTarget = false;
            Add(new Plot(Color.FromKnownColor(KnownColor.Black), "Occurred"));
            Add(new Plot(Color.FromArgb(255, 187, 128, 238), "Entry"));
            Overlay = true;
            CalculateOnBarClose = true;
        }

        protected override void OnBarUpdate()
        {
            //MyGap.Set(Input[0]);

            if (Bars != null && Bars.Count > 0)
            //             && TimeFrame.Periodicity == DatafeedHistoryPeriodicity.Minute
            //             && TimeFrame.PeriodicityValue == 15) 
            { }
            else
            {
                return;
            }

            if (Bars.BarsSinceSession == 0)
            {
                sessionprocessed = false;
            }

            TimeSpan openingtime = GlobalUtilities.GetOfficialMarketOpeningTime(Instrument.Symbol);

            if (Bars.GetTime(CurrentBar).TimeOfDay > openingtime //größer 09.00 geht für 15M und 1Std (und 1Tag?)
            && sessionprocessed == false)        //Tag noch nicht verarbeitet
            {
                sessionprocessed = true;
                GapTradeLong = GapTradeShort = false;

                IBar GapOpenBar = GlobalUtilities.GetFirstBarOfCurrentSession(Bars);
                GapOpen = GapOpenBar.Open;

                double LastDayClose = PriorDayOHLC().PriorClose[0];

                //Wenn es keinen LastDayClose Wert gibt -> raus (immer am Anfang des Charts)
                if (LastDayClose == 0)
                {
                    return;
                }


                GapSize = GapOpen - LastDayClose;
                DateTime LastDayCloseDate = Bars.GetTime(Count - 7);
                DateTime LastPeriod = Time[1];

                if (LastDayClose != null
                && Math.Abs(LastDayClose - GapOpen) > 0
                && Math.Abs(LastDayClose - GapOpen) < 0)
                {
                    existgap = true;


                    //Gap markieren (08.00 - 09.15)
                    string strMyRect = "MyRect" + Count;
                    string strMyGapSize = "MyGapSize" + Count;


                    //                    if (LastDayClose - GapOpen < 0)   
                    if (IsDownwardsGap() == true)
                    {
                        //Long
                        DrawRectangle(strMyRect, true, LastDayCloseDate, LastDayClose, LastPeriod, GlobalUtilities.GetHighestHigh(Bars, 5), _col_gap, _col_gap, 70);
                        DrawText(strMyGapSize, true, Math.Round(GapSize, 1).ToString(), LastDayCloseDate, LastDayClose + 25, 9, Color.Black, new Font("Areal", 9), StringAlignment.Center, Color.Black, Color.Azure, 1);


                        if (GapIndicatesTradeLong() == true)
                        {
                            //Chancenreicher SuccessTrade
                            PrepareTradeLong();
                        }
                    }
                    else
                    {
                        //Short                 
                        DrawRectangle(strMyRect, true, LastDayCloseDate, LastDayClose, LastPeriod, GlobalUtilities.GetLowestLow(Bars,5), _col_gap, _col_gap, 70);
                        DrawText(strMyGapSize, true, Math.Round(GapSize, 1).ToString(), LastDayCloseDate, LastDayClose - 25, 9, Color.Black, new Font("Areal", 9), StringAlignment.Center, Color.Black, Color.Azure, 1);

                        if (GapIndicatesTradeShort() == true)
                        {
                            ////Chancenreicher SuccessTrade
                            PrepareTradeShort();
                        }
                    }

                    if (GapTradeShort == true || GapTradeLong == true)
                    {
                        PrintBasicTradeInfo();
                    }

                }
                else
                {
                    existgap = false;
                }
            }

//09.15. - 09.30 Kerze
            else if (Bars.BarsSinceSession == 6 && existgap == true)
            {
                //Auswertung    
                printAuswertung();
            }
        }



        private bool GapIndicatesTradeLong()
        {
            if (LinReg(Closes[0], 5)[0] < GapOpen
            && GapSize > _PunkteGapMin
            && GapSize < _PunkteGapMax)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private bool GapIndicatesTradeShort()
        {
            if (LinReg(Closes[0], 5)[0] > GapOpen
            && GapSize > _PunkteGapMin
            && GapSize < _PunkteGapMax)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private void PrepareTradeShort()
        {
            string strArrowDown = "ArrowDown" + Bars.GetTime(CurrentBar);
            DrawArrowDown(strArrowDown, true, Bars.GetTime(Count - 1), Bars.GetOpen(CurrentBar) + 300 * TickSize, Color.Red);
            GapTradeShort = true;

            Occurred.Set(-1);
            Entry.Set(Bars.GetOpen(CurrentBar));
        }


        private void PrepareTradeLong()
        {
            string strArrowUp = "ArrowUp" + Bars.GetTime(CurrentBar);
            DrawArrowUp(strArrowUp, true, Bars.GetTime(Count - 1), Bars.GetOpen(CurrentBar) - 300 * TickSize, Color.Green);
            GapTradeLong = true;

            Occurred.Set(1);
            Entry.Set(Bars.GetOpen(CurrentBar));
        }

        private bool IsUpwardsGap()
        {
            if (GapSize > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsDownwardsGap()
        {
            if (GapSize < 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void PrintBasicTradeInfo()
        {
            Print("------------------" + Time[5] + "------------------");
            Print("LineReg: " + Math.Round(LinReg(5)[0]), 1);
            Print("Gap Size: " + GapSize);
        }


        private void printAuswertung()
        {
            decimal GapTradeResult;
            Color colorTextBox;

            GapTradeResult = (decimal)Bars.GetClose(CurrentBar - 1) - (decimal)Bars.GetOpen(CurrentBar - 1);
            if (GapTradeLong == true)
            {
                //Long
                GapTradeCounterLong += 1;
                GapTradeResultTotalLong = GapTradeResultTotalLong + GapTradeResult;


                string strGapeTradeLong = "GapTradeLong" + CurrentBar;
                string strTradeResultLong;

                if (GapTradeResult < 0)
                {
                    Print("Fail");
                    GapTradeFailCounterLong += 1;
                    strTradeResultLong = "Fail " + GapTradeResult.ToString();
                    colorTextBox = colFail;
                }
                else
                {
                    GapTradeWinCounterLong += 1;
                    strTradeResultLong = "Win " + GapTradeResult.ToString();
                    colorTextBox = colWin;
                }
                DrawText(strGapeTradeLong, true, strTradeResultLong, Time[1], Bars.GetHigh(CurrentBar - 1) + 10, 9, Color.Black, new Font("Areal", 9), StringAlignment.Center, Color.Black, colorTextBox, 70);

            }
            else if (GapTradeShort == true)
            {
                //Short
                GapTradeCounterShort += 1;
                GapTradeResultTotalShort = GapTradeResultTotalShort - GapTradeResult;


                string strGapeTradeShort = "GapTradeLong" + CurrentBar;
                string strTradeResultShort;

                if (GapTradeResult > 0)
                {
                    Print("FAAAAAAAAAAAAAAAIIIIIIIIIIIIIILLLLLLLLLL");
                    GapTradeFailCounterShort += 1;
                    strTradeResultShort = "Fail " + GapTradeResult.ToString();
                    colorTextBox = colFail;
                }
                else
                {
                    GapTradeWinCounterShort += 1;
                    strTradeResultShort = "Win " + GapTradeResult.ToString();
                    colorTextBox = colWin;
                }
                DrawText(strGapeTradeShort, true, strTradeResultShort, Time[1], Bars.GetLow(CurrentBar - 1) - 10, 9, Color.Black, new Font("Areal", 9), StringAlignment.Center, Color.Black, colorTextBox, 70);
            }
            Print("Gap Trade Result: " + GapTradeResult);
        }





        public override string ToString()
        {
            return "ShowGap";
        }

        public override string DisplayName
        {
            get
            {
                return "ShowGap";
            }
        }

        #region Properties

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

        //[Description("")]
        //[Category("Parameters")]
        //public int MyCondition1
        //{
        //    get { return _myCondition1; }
        //    set { _myCondition1 = Math.Max(1, value); }
        //}

        [Description("Mind. Punkte für Gap")]
        [Category("Parameters")]
        [DisplayName("MinPunkte")]
        public int PunkteGapMin
        {
            get { return _PunkteGapMin; }
            set { _PunkteGapMin = value; }
        }

        [Description("Max. Punkte für Gap")]
        [Category("Parameters")]
        [DisplayName("MaxPunkte")]
        public int PunkteGapMax
        {
            get { return _PunkteGapMax; }
            set { _PunkteGapMax = value; }
        }



        [Description("Select Color")]
        [Category("Colors")]
        [DisplayName("Farbe Gap")]
        public Color Color_Gap
        {
            get { return _col_gap; }
            set { _col_gap = value; }
        }

        #endregion
    }
}
