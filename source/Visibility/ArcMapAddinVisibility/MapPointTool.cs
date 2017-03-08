﻿// Copyright 2016 Esri 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;
using VisibilityLibrary.Helpers;
using VisibilityLibrary;
using System.Windows.Forms;

namespace ArcMapAddinVisibility
{
    public class MapPointTool : ESRI.ArcGIS.Desktop.AddIns.Tool
    {
        ISnappingEnvironment m_SnappingEnv;
        IPointSnapper m_Snapper;
        ISnappingFeedback m_SnappingFeedback;

        /// <summary>
        /// save last active tool used, so we can set back to this 
        /// </summary>
        private string lastActiveToolGuid;

        public MapPointTool()
        {
            if ((ArcMap.Application != null) && (ArcMap.Application.CurrentTool != null))
                lastActiveToolGuid = ArcMap.Application.CurrentTool.ID.Value as string;
        }

        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;

            if ((ArcMap.Application == null) || (ArcMap.Application.CurrentTool == null))
                return;

            // Keep track of the last tool used here so we can set it back to address:
            // https://github.com/Esri/visibility-addin-dotnet/issues/159
            // This is not a very efficient place to do this because it is called repeatedly
            // but only place I could find that knew the previous tool in use

            if (ArcMap.Application.CurrentTool.ID.Value.ToString().Equals(lastActiveToolGuid))
                return;

            // this is a GUID - with no way to get the progID 
            // (except PInvoke of Win32 ProgIDFromCLSID) so using GUIDs instead of more readable ProgID
            lastActiveToolGuid = ArcMap.Application.CurrentTool.ID.Value as string;
        }

        protected override void OnActivate()
        {
			//Get the snap environment and initialize the feedback
			UID snapUID = new UID();

            this.Cursor = Cursors.Cross;

			snapUID.Value = "{E07B4C52-C894-4558-B8D4-D4050018D1DA}";
			m_SnappingEnv = ArcMap.Application.FindExtensionByCLSID(snapUID) as ISnappingEnvironment;
			m_Snapper = m_SnappingEnv.PointSnapper;
			m_SnappingFeedback = new SnappingFeedbackClass();
			m_SnappingFeedback.Initialize(ArcMap.Application, m_SnappingEnv, true);

            Mediator.NotifyColleagues(VisibilityLibrary.Constants.MAP_POINT_TOOL_ACTIVATED, true);

            // Also notify what the previous tool was so it can be set back
            Mediator.NotifyColleagues(VisibilityLibrary.Constants.MAP_TOOL_CHANGED, lastActiveToolGuid);
        }

        protected override bool OnDeactivate()
        {
            Mediator.NotifyColleagues(VisibilityLibrary.Constants.MAP_POINT_TOOL_DEACTIVATED, false);

            return base.OnDeactivate();
        }

        protected override void OnMouseDown(ESRI.ArcGIS.Desktop.AddIns.Tool.MouseEventArgs arg)
        {
            if (arg.Button != System.Windows.Forms.MouseButtons.Left)
                return;

            try
            {
                //Get the active view from the ArcMap static class.
                IActiveView activeView = ArcMap.Document.FocusMap as IActiveView;

                var point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(arg.X, arg.Y) as IPoint;
                ISnappingResult snapResult = null;
                //Try to snap the current position
                snapResult = m_Snapper.Snap(point);
                m_SnappingFeedback.Update(null, 0);
                if (snapResult != null && snapResult.Location != null)
                    point = snapResult.Location;

                Mediator.NotifyColleagues(Constants.NEW_MAP_POINT, point);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        protected override void OnMouseMove(MouseEventArgs arg)
        {
            IActiveView activeView = ArcMap.Document.FocusMap as IActiveView;

            var point = activeView.ScreenDisplay.DisplayTransformation.ToMapPoint(arg.X, arg.Y) as IPoint;
            ISnappingResult snapResult = null;
            //Try to snap the current position
            snapResult = m_Snapper.Snap(point);
            m_SnappingFeedback.Update(snapResult, 0);
            if (snapResult != null && snapResult.Location != null)
                point = snapResult.Location;

            Mediator.NotifyColleagues(Constants.MOUSE_MOVE_POINT, point);
        }
    }

}
