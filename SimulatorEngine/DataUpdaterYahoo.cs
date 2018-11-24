﻿//==============================================================================
// Project:     Trading Simulator
// Name:        DataUpdaterYahoo
// Description: Web data updater, Yahoo! finance
// History:     2018x05, FUB, created
//------------------------------------------------------------------------------
// Copyright:   (c) 2017-2018, Bertram Solutions LLC
//              http://www.bertram.solutions
// License:     this code is licensed under GPL-3.0-or-later
//==============================================================================

#region libraries
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataUpdaterCollection
    {
        /// <summary>
        /// Data updater for yahoo.com
        /// </summary>
        private class DataUpdaterYahoo : DataUpdater
        {
            #region internal data & helpers
            // URL discovered with MultiCharts' QuoteManager
            private static readonly string _urlTemplate = @"http://l1-query.finance.yahoo.com/v8/finance/chart/{0}?interval=1d&period1={1}&period2={2}";
            #endregion
            #region internal helpers
            private static readonly DateTime _epochOrigin = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            private static DateTime FromUnixTime(long unixTime)
            {
                return _epochOrigin.AddSeconds(unixTime);
            }

            private static long ToUnixTime(DateTime date)
            {
                return Convert.ToInt64((date - _epochOrigin).TotalSeconds);
            }
            #endregion

            #region public DataUpdaterYahoo(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
            /// <summary>
            /// Create and initialize data updater object.
            /// </summary>
            /// <param name="simulator">parent simulator</param>
            /// <param name="info">info dictionary</param>
            public DataUpdaterYahoo(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
            {
            }
            #endregion

            #region override IEnumerable<Bar> void UpdateData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Run data update.
            /// </summary>
            /// <param name="startTime">start of update range</param>
            /// <param name="endTime">end of update range</param>
            /// <returns>enumerable of updated bars</returns>
            override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
            {
                string url = string.Format(_urlTemplate,
                    Info[DataSourceValue.symbolYahoo], ToUnixTime(startTime), ToUnixTime(endTime));

                using (var client = new WebClient())
                {
                    string rawData = client.DownloadString(url);

                    JObject jsonData = JObject.Parse(rawData);

                    IEnumerator<Int64> timeStamp = jsonData["chart"]["result"][0]["timestamp"]
                        .Select(x => Convert.ToInt64(x))
                        .GetEnumerator();

                    IEnumerator<double> open = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["open"]
                        .Select(x => Convert.ToDouble(x))
                        .GetEnumerator();

                    IEnumerator<double> high = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["high"]
                        .Select(x => Convert.ToDouble(x))
                        .GetEnumerator();

                    IEnumerator<double> low = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["low"]
                        .Select(x => Convert.ToDouble(x))
                        .GetEnumerator();

                    IEnumerator<double> close = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["close"]
                        .Select(x => Convert.ToDouble(x))
                        .GetEnumerator();

                    IEnumerator<double> adjClose = jsonData["chart"]["result"][0]["indicators"]["adjclose"][0]["adjclose"]
                        .Select(x => Convert.ToDouble(x))
                        .GetEnumerator();

                    IEnumerator<Int64> volume = jsonData["chart"]["result"][0]["indicators"]["quote"][0]["volume"]
                        .Select(x => Convert.ToInt64(x))
                        .GetEnumerator();

                    double priceMultiplier = Info.ContainsKey(DataSourceValue.dataUpdaterPriceMultiplier)
                        ? Convert.ToDouble(Info[DataSourceValue.dataUpdaterPriceMultiplier])
                        : 1.0;

                    while (timeStamp.MoveNext())
                    {
                        open.MoveNext();
                        high.MoveNext();
                        low.MoveNext();
                        close.MoveNext();
                        adjClose.MoveNext();
                        volume.MoveNext();

                        DateTime time = FromUnixTime(timeStamp.Current).Date + TimeSpan.FromHours(16);

                        Bar newBar = new Bar(Info[DataSourceValue.ticker],
                            time,
                            open.Current * priceMultiplier,
                            high.Current * priceMultiplier,
                            low.Current * priceMultiplier,
                            close.Current * priceMultiplier,
                            volume.Current,
                            true,
                            0.0, 0.0, 0, 0, false,
                            default(DateTime), 0.00, false);

                        if (newBar.Time >= startTime
                        && newBar.Time <= endTime)
                            yield return newBar;
                    }

                    yield break;
                }
            }
            #endregion

            #region public override string Name
            /// <summary>
            /// Name of updater.
            /// </summary>
            public override string Name
            {
                get
                {
                    return "Yahoo";
                }
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file