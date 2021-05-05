﻿//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceConstantYield
// Description: Data source providing a constant yield
// History:     2019iii03, FUB, created
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
using System.Collections.Generic;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        private class DataSourceConstantYield : DataSource
        {
            #region internal helpers
            private class SimConstantYield : Algorithm
            {
                private static readonly string UNDERLYING_NICK = "$SPX.index";

                private List<Bar> _data;
                private readonly DateTime _startTime;
                private readonly DateTime _endTime;
                private readonly double _yield;
                private readonly string _symbol;

                public SimConstantYield(List<Bar> data, DateTime startTime, DateTime endTime, double yield)
                {
                    _data = data;
                    _startTime = startTime;
                    _endTime = endTime;
                    _yield = yield;

                    _symbol = string.Format("YIELD-{0:F1}%", _yield);
                }

                public override void Run()
                {
                    StartTime = _startTime;
                    EndTime = _endTime;

                    AddDataSource(UNDERLYING_NICK);

                    double price = 100.00;

                    foreach (var simTime in SimTimes)
                    {
                        ITimeSeries<double> underlying = FindInstrument(UNDERLYING_NICK).Close;

                        Bar bar = new Bar(
                            _symbol,
                            SimTime[0],
                            price, price, price, price, 100, true, // OHLC, volume
                            default(double), default(double), default(long), default(long), false,
                            default(DateTime), default(double), default(bool));

                        _data.Add(bar);

                        price *= Math.Pow(10.0, Math.Log10(1.0 + _yield / 100.0) / 252.0);
                    }
                }
            }

            private void LoadData(List<Bar> data, DateTime startTime, DateTime endTime)
            {
                double yield = 7.79031;
                //double yield = 6.00;
                var sim = new SimConstantYield(data, startTime, endTime, yield);
                sim.Run();
            }
            #endregion

            //---------- API
            #region public DataSourceConstantYield(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for constant yield quotes.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceConstantYield(Dictionary<DataSourceParam, string> info) : base(info)
            {
            }
            #endregion
            #region override public void LoadData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Load data into memory.
            /// </summary>
            /// <param name="startTime">start of load range</param>
            /// <param name="endTime">end of load range</param>
            public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            {
                DateTime t1 = DateTime.Now;
                Output.WriteLine(string.Format("DataSourceConstantYield: generating data for {0}...", Info[DataSourceParam.nickName]));

                List<Bar> data = new List<Bar>();
                LoadData(data, startTime, endTime);

                DateTime t2 = DateTime.Now;
                Output.WriteLine(string.Format("DataSourceConstantYield: finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                CachedData = data;
                return data;
            }
            #endregion
        }
    }
}

//==============================================================================
// end of file