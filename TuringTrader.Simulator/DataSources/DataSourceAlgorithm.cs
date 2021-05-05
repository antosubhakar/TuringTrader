﻿//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataSourceAlgorithm
// Description: Data source, derived from an algorithm.
// History:     2019iii13, FUB, created
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
using System.Linq;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataSourceCollection
    {
        private class DataSourceAlgorithm : DataSource
        {
            #region internal data
            private readonly Algorithm _algo;
            #endregion

            //---------- API
            #region public DataSourceAlgorithm(Dictionary<DataSourceValue, string> info)
            /// <summary>
            /// Create and initialize new data source for algorithm quotes.
            /// </summary>
            /// <param name="info">info dictionary</param>
            public DataSourceAlgorithm(Dictionary<DataSourceParam, string> info) : base(info)
            {
                if (!Info.ContainsKey(DataSourceParam.symbolAlgo))
                    throw new Exception(string.Format("{0}: {1} missing mandatory symbolAlgo key", GetType().Name, info[DataSourceParam.nickName]));

                var items = info[DataSourceParam.symbolAlgo].Split(",").ToList();
                var algoName = items[0];
                var algoParam = items.Count > 1 ? items[1] : null;

                _algo = (Algorithm)AlgorithmLoader.InstantiateAlgorithm(algoName);

                if (_algo == null)
                    throw new Exception(string.Format("DataSourceAlgorithm: failed to instantiate algorithm {0}", info[DataSourceParam.nickName2]));

                _algo.SubclassedParam = algoParam;
                Info[DataSourceParam.name] = _algo.Name;

                // by default, we run the data source through the cache,
                // if it was instantiated from an info dictionary
                // Info[DataSourceParam.allowSync] = "true";
            }
            #endregion
            #region  public DataSourceAlgorithm(SubclassableAlgorithm algo)
            public DataSourceAlgorithm(Algorithm algo) : base(new Dictionary<DataSourceParam, string>())
            {
                _algo = algo;

                Info[DataSourceParam.nickName]
                    = Info[DataSourceParam.nickName2]
                    = Info[DataSourceParam.ticker]
                    = Info[DataSourceParam.symbolAlgo]
                    = string.Format("algorithm:{0}{1}",
                        _algo.GetType().Name,
                        _algo.GetType().Name != _algo.Name ? ";" + _algo.Name : "");

                Info[DataSourceParam.name] = _algo.Name;

                // by default, we run the data source in sync, if it was
                // instantiated from an algorithm instance
                Info[DataSourceParam.allowSync] = "true";
            }
            #endregion
            #region public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            /// <summary>
            /// Run sub-classed algorithm and return bars as enumerable.
            /// </summary>
            /// <param name="startTime">start of load range</param>
            /// <param name="endTime">end of load range</param>
            /// <returns>bars created from sub-classed algo</returns>
            public override IEnumerable<Bar> LoadData(DateTime startTime, DateTime endTime)
            {
#if true
                _algo.IsDataSource = true;

                if (_algo.CanRunAsChild && Info.ContainsKey(DataSourceParam.allowSync))
                {
                    // for child algorithms, we bypass the cache and run the
                    // child bar-for-bar and in sync with its parent

                    foreach (var bar in _algo.Run(startTime, endTime))
                    {
                        yield return bar;

                        if (!_algo.IsLastBar)
                        {
                            // the simulator core needs to know the next bar's
                            // timestamp. at the same time, we want to avoid
                            // child algorithms from running one bar ahead of
                            // the main algo. we fix this issue by returning
                            // a dummy bar, announcing the next timestamp.

                            var dummy = Bar.NewOHLC(
                                null, _algo.NextSimTime,
                                0.0, 0.0, 0.0, 0.0, 0);

                            yield return dummy;
                        }
                    }

                    yield break;
                }
                else
                {
                    // for all other algorithms, we run the algo
                    // all at once and save the result in the cache

                    var algoNick = Info[DataSourceParam.nickName];

                    var cacheKey = new CacheId().AddParameters(
                        algoNick.GetHashCode(), // _algoName.GetHashCode(),
                        startTime.GetHashCode(),
                        endTime.GetHashCode());

                    List<Bar> retrievalFunction()
                    {
                        try
                        {
                            DateTime t1 = DateTime.Now;
                            Output.WriteLine(string.Format("DataSourceAlgorithm: generating data for {0}...", Info[DataSourceParam.nickName]));

                            var bars = _algo.Run(startTime, endTime)
                                .ToList();

                            DateTime t2 = DateTime.Now;
                            Output.WriteLine(string.Format("DataSourceAlgorithm: finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                            return bars;
                        }

                        catch
                        {
                            throw new Exception("DataSourceAlgorithm: failed to run sub-classed algorithm " + algoNick);
                        }
                    }

                    List<Bar> data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction, true);

                    if (data.Count == 0)
                        throw new Exception(string.Format("DataSourceAlgorithm: no data for {0}", Info[DataSourceParam.nickName]));

                    CachedData = data;
                    foreach (var bar in data)
                        yield return bar;
                }
#else
                var algoNick = Info[DataSourceParam.nickName];

                var cacheKey = new CacheId(null, "", 0,
                    algoNick.GetHashCode(), // _algoName.GetHashCode(),
                    startTime.GetHashCode(),
                    endTime.GetHashCode());

                List<Bar> retrievalFunction()
                {
                    try
                    {
                        DateTime t1 = DateTime.Now;
                        Output.WriteLine(string.Format("DataSourceAlgorithm: generating data for {0}...", Info[DataSourceParam.nickName]));

                        _algo.StartTime = startTime;
                        _algo.EndTime = endTime;
                        _algo.ParentDataSource = this;

                        _algo.SubclassedData = new List<Bar>(); ;

                        _algo.Run();

                        DateTime t2 = DateTime.Now;
                        Output.WriteLine(string.Format("DataSourceAlgorithm: finished after {0:F1} seconds", (t2 - t1).TotalSeconds));

                        return _algo.SubclassedData;
                    }

                    catch
                    {
                        throw new Exception("DataSourceAlgorithm: failed to run sub-classed algorithm " + algoNick);
                    }
                }

                List<Bar> data = Cache<List<Bar>>.GetData(cacheKey, retrievalFunction, true);

                if (data.Count == 0)
                    throw new Exception(string.Format("DataSourceAlgorithm: no data for {0}", Info[DataSourceParam.nickName]));

                Data = data;
#endif
            }
            #endregion

            #region public bool IsAlgorithm
            public override bool IsAlgorithm => true;
            #endregion
            #region public Algorithm Algorithm
            public override Algorithm Algorithm => _algo;
            #endregion
        }
    }
}

//==============================================================================
// end of file