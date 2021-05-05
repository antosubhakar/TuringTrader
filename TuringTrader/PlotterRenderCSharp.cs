﻿//==============================================================================
// Project:     TuringTrader, simulator core
// Name:        PlotterRenderCSharp
// Description: Plotter renderer for C# templates
// History:     2019vi20, FUB, created
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
using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using TuringTrader.Simulator;
#endregion

namespace TuringTrader
{
    static class PlotterRenderCSharp
    {
        public static void Register()
        {
            Plotter.Renderer += Renderer;
        }

        #region public static void Renderer(Plotter plotter, string pathToCSharpTemplate)
        public static void Renderer(Plotter plotter, string pathToCSharpTemplate)
        {
            if (Path.GetExtension(pathToCSharpTemplate).ToLower() != ".cs")
                return;

            void uiThread()
            {
                //----- dynamic compile
                MetadataReference[] moreReferences = new MetadataReference[]
                {
                    MetadataReference.CreateFromFile(typeof(OxyPlot.PlotModel).GetTypeInfo().Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(TuringTrader.ReportTemplate).GetTypeInfo().Assembly.Location),
                };
                var assy = DynamicCompile.CompileSource(pathToCSharpTemplate, moreReferences);

                if (assy == null)
                {
                    Output.WriteLine("Plotter: can't compile template {0}", pathToCSharpTemplate);
                    return;
                }

                //----- instantiate template
                var templateType = assy.GetTypes()
                    .Where(t => t.IsSubclassOf(typeof(ReportTemplate)))
                    .FirstOrDefault();

                if (templateType == null)
                {
                    Output.WriteLine("Plotter: can't load template {0}", pathToCSharpTemplate);
                    return;
                }

                ReportTemplate template = (ReportTemplate)Activator.CreateInstance(templateType);
                template.PlotData = plotter.AllData;
                template.PlotTitle = plotter.Title;

                //----- open dialog
                var report = new Report(template);
                report.ShowDialog();
            }

            // The calling thread must be STA, because many UI components require this.
            // https://stackoverflow.com/questions/2329978/the-calling-thread-must-be-sta-because-many-ui-components-require-this

            Thread thread = new Thread(uiThread);
            thread.SetApartmentState(ApartmentState.STA);

            thread.Start();
            //thread.Join(); // wait for window to close
        }
        #endregion
    }
}

//==============================================================================
// end of file