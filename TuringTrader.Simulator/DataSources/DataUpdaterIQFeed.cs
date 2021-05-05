﻿//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        DataUpdaterIQFeed
// Description: IQFeed/ DTN data updater
// History:     2018x02, FUB, created
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

// the login credentials are taken from HKEY_CURRENT_USER\Software\DTN\IQFeed\Startup,
// which is where the IQFeed launcher will store them.
// if this doesn't work, credentials may be placed in the environment as follows:
// Run rundll32 sysdm.cpl,EditEnvironmentVariables to open the Environment Variables
// In your User variables, create 4 new ones: 
// IQCONNECT_LOGIN
// IQCONNECT_PASSWORD
// IQCONNECT_PRODUCT_ID
// IQCONNECT_PRODUCT_VERSION(not mandatory, will fallback to 1.0.0.0)

#region libraries
using System;
using System.Collections.Generic;
//using IQFeed.CSharpApiClient;
//using IQFeed.CSharpApiClient.Lookup;
//using IQFeed.CSharpApiClient.Lookup.Historical.Messages;
//using IQFeed.CSharpApiClient.Lookup.Chains.Equities;
//using IQFeed.CSharpApiClient.Streaming.Level1;
//using IQFeed.CSharpApiClient.Streaming.Level1.Messages;
//using IQFeed.CSharpApiClient.Lookup.Chains;
#endregion

namespace TuringTrader.Simulator
{
    public partial class DataUpdaterCollection
    {
#if true
        private class DataUpdaterIQFeed : DataUpdater
        {
            public override string Name => "IQFeed";

            public DataUpdaterIQFeed(SimulatorCore simulator, Dictionary<DataSourceParam, string> info) : base(simulator, info)
            {

            }
            override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
            {
                throw new Exception("IQFeed download currently broken, we're working on it. Use Tiingo instead.");
            }
        }
#else
        private class DataUpdaterIQFeed : DataUpdater
        {
        #region internal helpers
            private string _username
            {
                get
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DTN\IQFeed\Startup"))
                    {
                        if (key != null)
                            return (string)key.GetValue("Login");
                        else
                            return null;
                    }
                }
            }
            private string _password
            {
                get
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\DTN\IQFeed\Startup"))
                    {
                        if (key != null)
                            return (string)key.GetValue("Password");
                        else
                            return null;
                    }
                }
            }
        #endregion

        #region public DataUpdaterIQFeed(Dictionary<DataSourceValue, string> info) : base(info)
            public DataUpdaterIQFeed(SimulatorCore simulator, Dictionary<DataSourceValue, string> info) : base(simulator, info)
            {
            }
        #endregion

        #region override IEnumerable<Bar> void UpdateData(DateTime startTime, DateTime endTime)
            override public IEnumerable<Bar> UpdateData(DateTime startTime, DateTime endTime)
            {
                // create clients for IQFeed
                var lookupClient = LookupClientFactory.CreateNew();
                var level1Client = Level1ClientFactory.CreateNew();

                // try to connect
                try
                {
                    lookupClient.Connect();
                    level1Client.Connect();
                }

                // if connection fails: run the launcher and retry
                catch (Exception)
                {
                    IQFeedLauncher.Start(_username, _password, "ONDEMAND_SERVER", "1.0");
                    lookupClient.Connect();
                    level1Client.Connect();
                }

                string symbol = Info[DataSourceValue.symbolIqfeed];

                // TODO: should connect this to DataSource.IsOption
                if (!Info.ContainsKey(DataSourceValue.optionExpiration))
                {
                    IEnumerable<IDailyWeeklyMonthlyMessage> dailyMessages =
                        lookupClient.Historical.ReqHistoryDailyTimeframeAsync(
                            symbol, startTime, endTime).Result;

                    foreach (IDailyWeeklyMonthlyMessage msg in dailyMessages.OrderBy(m => m.Timestamp))
                    {
                        DateTime barTime = msg.Timestamp.Date + DateTime.Parse("16:00").TimeOfDay;

                        Bar newBar = new Bar(
                            Info[DataSourceValue.ticker], barTime,
                            msg.Open, msg.High, msg.Low, msg.Close, msg.PeriodVolume, true,
                            0.0, 0.0, 0, 0, false,
                            default(DateTime), 0.0, false);

                        if (newBar.Time >= startTime
                        && newBar.Time <= endTime)
                            yield return newBar;
                    }
                }
                else
                {
                    //----- get option chain
                    // see http://www.iqfeed.net/symbolguide/index.cfm?symbolguide=guide&displayaction=support&section=guide&web=iqfeed&guide=options&web=IQFeed&type=indices
                    IEnumerable<EquityOption> optionChain = lookupClient.Chains.ReqChainIndexEquityOptionAsync(
                        symbol,
                        OptionSideFilterType.CP,
                        "ABCDEFGHIJKLMNOPQRSTUVWX", // month codes: Calls A-L, Puts M-X
                        null)                       // nearMonth
                        .Result
                        .Where(i => (i.Expiration - DateTime.Now).TotalDays < 180)
                        .ToList();

                    Output.Write("found {0} contracts...", optionChain.Count());

                    // FIXME: this is far from pretty: need better timezone conversion
                    //DateTime time = DateTime.Now + TimeSpan.FromHours(3);
                    DateTime time = DateTime.Now.Date + TimeSpan.FromHours(16);

                    //----- get snapshots for many options in parallel
                    MTJobQueue jobQueue = new MTJobQueue(50);
                    HashSet<Tuple<EquityOption, UpdateSummaryMessage>> quotes = new HashSet<Tuple<EquityOption, UpdateSummaryMessage>>();
                    int num = 0;
                    foreach (var option in optionChain)
                    {
                        jobQueue.QueueJob(() =>
                        {
                            try
                            {
                                UpdateSummaryMessage snapshot = level1Client
                                    .GetUpdateSummarySnapshotAsync(option.Symbol)
                                    .Result;

                                if (snapshot != null)
                                {
                                    lock (quotes)
                                    {
                                        quotes.Add(Tuple.Create(option, snapshot));

                                        if (++num % 1000 == 0) Output.Write("|");
                                        else if (num % 100 == 0) Output.Write(".");
                                    }
                                }
                            }
                            catch
                            {
                            // we occasionally get a task cancelled exception
                            // ignore this, and move on
                        }
                        });
                    }

                    jobQueue.WaitForCompletion();

                    //----- return bars
                    foreach (var quote in quotes)
                    {
                        EquityOption option = quote.Item1;
                        UpdateSummaryMessage snapshot = quote.Item2;

                        Bar newBar = new Bar(
                            symbol, time,
                            default(double), default(double), default(double), default(double), default(long), false,
                            snapshot.Bid, snapshot.Ask, snapshot.BidSize, snapshot.AskSize, true,
                            option.Expiration, option.StrikePrice, option.Side == OptionSide.Put);

                        yield return newBar;
                    }
                } // if (!Info.ContainsKey(DataSourceValue.optionExpiration))

                yield break;
            }
        #endregion

        #region public override string Name
            public override string Name
            {
                get
                {
                    return "IQFeed";
                }
            }
        #endregion
        }
#endif
    }
}

//==============================================================================
// end of file