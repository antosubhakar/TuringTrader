﻿//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        Bar
// Description: data structure for single instrument bar
// History:     2018ix11, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2011-2019, Bertram Solutions LLC
//              https://www.bertram.solutions
// License:     This file is part of TuringTrader, an open-source backtesting
//              engine/ market simulator.
//              TuringTrader is free software: you can redistribute it and/or 
//              modify it under the terms of the GNU Affero General Public 
//              License as published by the Free Software Foundation, either 
//              version 3 of the License, or (at your option) any later version.
//              TuringTrader is distributed in the hope that it will be useful,
//              but WITHOUT ANY WARRANTY; without even the implied warranty of
//              MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//              GNU Affero General Public License for more details.
//              You should have received a copy of the GNU Affero General Public
//              License along with TuringTrader. If not, see 
//              https://www.gnu.org/licenses/agpl-3.0.
//==============================================================================

#region libraries
using System;
#endregion

namespace TuringTrader.Simulator
{
    /// <summary>
    /// Container class holding all fields of a single bar, most notably
    /// time stamps and price information. Bar objects are read-only in nature,
    /// therefore all values need to be provided during object construction.
    /// </summary>
    public class Bar
    {
        #region public Bar(...)
        /// <summary>
        /// Create and initialize a bar object.
        /// </summary>
        /// <param name="ticker">ticker, most often same as symbol</param>
        /// <param name="time">Initializer for Time field</param>
        /// <param name="open">Initializer for Open field</param>
        /// <param name="high">Initializer for High field</param>
        /// <param name="low">Initializer for Low field</param>
        /// <param name="close">Initializer for Close field</param>
        /// <param name="volume">Initializer for Volume field</param>
        /// <param name="hasOHLC">Initializer for HasOHLC field</param>
        /// <param name="bid">Initializer for Bid field</param>
        /// <param name="ask">Initializer for Ask field</param>
        /// <param name="bidVolume">Initializer for BidVolume field</param>
        /// <param name="askVolume">Initializer for AskVolume field</param>
        /// <param name="hasBidAsk">Initializer for HasBidAsk field</param>
        /// <param name="optionExpiry">Initializer for OptionExpiry field</param>
        /// <param name="optionStrike">Initializer for OptionStrike field</param>
        /// <param name="optionIsPut">Initializer for OptionIsPut field</param>
        public Bar(
            string ticker, DateTime time,
            double open, double high, double low, double close, long volume, bool hasOHLC,
            double bid, double ask, long bidVolume, long askVolume, bool hasBidAsk,
            DateTime optionExpiry, double optionStrike, bool optionIsPut)
        {
            Symbol = ticker; // default value, changed for options below
            Time = time;

            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
            HasOHLC = hasOHLC;

            Bid = bid;
            Ask = ask;
            BidVolume = bidVolume;
            AskVolume = askVolume;
            HasBidAsk = hasBidAsk;

            OptionExpiry = optionExpiry;
            OptionStrike = optionStrike;
            OptionIsPut = optionIsPut;
            IsOption = optionStrike != default(double);
            if (IsOption)
            {
                Symbol = string.Format("{0}{1:yyMMdd}{2}{3:D8}",
                            Symbol,
                            OptionExpiry,
                            OptionIsPut ? "P" : "C",
                            (int)Math.Floor(1000.0 * OptionStrike));
            }
        }
        #endregion
        #region static public Bar NewOHLC(...)
        /// <summary>
        /// Create new OHLC bar.
        /// </summary>
        /// <param name="ticker"></param>
        /// <param name="t"></param>
        /// <param name="o"></param>
        /// <param name="h"></param>
        /// <param name="l"></param>
        /// <param name="c"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        static public Bar NewOHLC(string ticker, DateTime t, double o, double h, double l, double c, long v)
        {
            return new Bar(
                ticker, t,
                o, h, l, c, v, true,
                default(double), default(double), default(long), default(long), false,
                default(DateTime), default(double), false);
        }
        #endregion

        #region public readonly string Symbol
        /// <summary>
        /// Fully qualified instrument symbol. Examples are AAPL, or
        /// XSP080119C00152000.
        /// </summary>
        public readonly string Symbol;
        #endregion
        #region public readonly DateTime Time
        /// <summary>
        /// Time stamp, with date and time
        /// </summary>
        public readonly DateTime Time;
        #endregion

        #region public readonly double Open
        /// <summary>
        /// Open price.
        /// </summary>
        public readonly double Open;
        #endregion
        #region public readonly double High
        /// <summary>
        /// High price.
        /// </summary>
        public readonly double High;
        #endregion
        #region public readonly double Low
        /// <summary>
        /// Low price.
        /// </summary>
        public readonly double Low;
        #endregion
        #region public readonly double Close
        /// <summary>
        /// Close price.
        /// </summary>
        public readonly double Close;
        #endregion
        #region public readonly long Volume
        /// <summary>
        /// Trading volume.
        /// </summary>
        public readonly long Volume;
        #endregion
        #region public readonly bool HasOHLC
        /// <summary>
        /// Flag indicating availability of Open/ High/ Low/ Close pricing.
        /// </summary>
        public readonly bool HasOHLC;
        #endregion

        #region public readonly double Bid
        /// <summary>
        /// Bid price.
        /// </summary>
        public readonly double Bid;
        #endregion
        #region public readonly double Ask
        /// <summary>
        /// Asking price.
        /// </summary>
        public readonly double Ask;
        #endregion
        #region public readonly long BidVolume
        /// <summary>
        ///  Bid volume.
        /// </summary>
        public readonly long BidVolume;
        #endregion
        #region public readonly long AskVolume
        /// <summary>
        ///  Ask volume.
        /// </summary>
        public readonly long AskVolume;
        #endregion
        #region public readonly bool HasBidAsk;
        /// <summary>
        /// Flag indicating availability of Bid/ Ask pricing.
        /// </summary>
        public readonly bool HasBidAsk;
        #endregion

        #region public readonly DateTime OptionExpiry
        /// <summary>
        /// Only valid for options: Option expiry date.
        /// </summary>
        public readonly DateTime OptionExpiry;
        #endregion
        #region public readonly double OptionStrike
        /// <summary>
        /// Only valid for options: Option strike price. 
        /// </summary>
        public readonly double OptionStrike;
        #endregion
        #region public readonly bool OptionIsPut
        /// <summary>
        /// Only valid for options: true for puts, false for calls.
        /// </summary>
        public readonly bool OptionIsPut;
        #endregion
        #region public readonly bool IsOption
        /// <summary>
        /// Flag indicating validity of option fields.
        /// </summary>
        public readonly bool IsOption;
        #endregion
    }
}
//==============================================================================
// end of file