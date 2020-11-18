﻿//-----------------------------------------------------------------------
// <copyright file="ScreenCollection.cs" company="Gavin Kendall">
//     Copyright (c) 2020 Gavin Kendall
// </copyright>
// <author>Gavin Kendall</author>
// <summary>A collection of screens.</summary>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.
//-----------------------------------------------------------------------
using System;
using System.Text;
using System.Xml;

namespace AutoScreenCapture
{
    /// <summary>
    /// A collection class to store and manage Screen objects.
    /// </summary>
    public class ScreenCollection : CollectionTemplate<Screen>
    {
        private const string XML_FILE_INDENT_CHARS = "   ";
        private const string XML_FILE_SCREEN_NODE = "screen";
        private const string XML_FILE_SCREENS_NODE = "screens";
        private const string XML_FILE_ROOT_NODE = "autoscreen";

        private const string SCREEN_VIEWID = "viewid";
        private const string SCREEN_NAME = "name";
        private const string SCREEN_FOLDER = "folder";
        private const string SCREEN_MACRO = "macro";
        private const string SCREEN_COMPONENT = "component";
        private const string SCREEN_FORMAT = "format";
        private const string SCREEN_JPEG_QUALITY = "jpeg_quality";
        private const string SCREEN_RESOLUTION_RATIO = "resolution_ratio";
        private const string SCREEN_MOUSE = "mouse";
        private const string SCREEN_ACTIVE = "active";
        private const string SCREEN_X = "x";
        private const string SCREEN_Y = "y";
        private const string SCREEN_WIDTH = "width";
        private const string SCREEN_HEIGHT = "height";

        private readonly string SCREEN_XPATH;

        private static string AppCodename { get; set; }
        private static string AppVersion { get; set; }

        private ImageFormatCollection _imageFormatCollection;

        /// <summary>
        /// The empty constructor for the screen collection.
        /// </summary>
        public ScreenCollection()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("/");
            sb.Append(XML_FILE_ROOT_NODE);
            sb.Append("/");
            sb.Append(XML_FILE_SCREENS_NODE);
            sb.Append("/");
            sb.Append(XML_FILE_SCREEN_NODE);

            SCREEN_XPATH = sb.ToString();
        }

        /// <summary>
        /// Gets a component by the component index.
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public Screen GetByComponent(int component)
        {
            foreach (Screen screen in base.Collection)
            {
                if (screen.Component == component)
                {
                    return screen;
                }
            }

            return null;
        }

        /// <summary>
        /// Loads the screens.
        /// </summary>
        public bool LoadXmlFileAndAddScreens(ImageFormatCollection imageFormatCollection)
        {
            try
            {
                _imageFormatCollection = imageFormatCollection;

                if (FileSystem.FileExists(FileSystem.ScreensFile))
                {
                    Log.WriteDebugMessage("Screens file \"" + FileSystem.ScreensFile + "\" found. Attempting to load XML document");

                    XmlDocument xDoc = new XmlDocument();
                    xDoc.Load(FileSystem.ScreensFile);

                    Log.WriteDebugMessage("XML document loaded");

                    AppVersion = xDoc.SelectSingleNode("/autoscreen").Attributes["app:version"]?.Value;
                    AppCodename = xDoc.SelectSingleNode("/autoscreen").Attributes["app:codename"]?.Value;

                    XmlNodeList xScreens = xDoc.SelectNodes(SCREEN_XPATH);

                    foreach (XmlNode xScreen in xScreens)
                    {
                        Screen screen = new Screen();
                        XmlNodeReader xReader = new XmlNodeReader(xScreen);

                        while (xReader.Read())
                        {
                            if (xReader.IsStartElement() && !xReader.IsEmptyElement)
                            {
                                switch (xReader.Name)
                                {
                                    case SCREEN_VIEWID:
                                        xReader.Read();
                                        screen.ViewId = Guid.Parse(xReader.Value);
                                        break;

                                    case SCREEN_NAME:
                                        xReader.Read();
                                        screen.Name = xReader.Value;
                                        break;

                                    case SCREEN_FOLDER:
                                        xReader.Read();
                                        screen.Folder = xReader.Value;
                                        break;

                                    case SCREEN_MACRO:
                                        xReader.Read();
                                        screen.Macro = xReader.Value;
                                        break;

                                    case SCREEN_COMPONENT:
                                        xReader.Read();
                                        screen.Component = Convert.ToInt32(xReader.Value);
                                        break;

                                    case SCREEN_FORMAT:
                                        xReader.Read();
                                        screen.Format = imageFormatCollection.GetByName(xReader.Value);
                                        break;

                                    case SCREEN_JPEG_QUALITY:
                                        xReader.Read();
                                        screen.JpegQuality = Convert.ToInt32(xReader.Value);
                                        break;

                                    case SCREEN_RESOLUTION_RATIO:
                                        xReader.Read();
                                        screen.ResolutionRatio = Convert.ToInt32(xReader.Value);
                                        break;

                                    case SCREEN_MOUSE:
                                        xReader.Read();
                                        screen.Mouse = Convert.ToBoolean(xReader.Value);
                                        break;

                                    case SCREEN_ACTIVE:
                                        xReader.Read();
                                        screen.Active = Convert.ToBoolean(xReader.Value);
                                        break;

                                    case SCREEN_X:
                                        xReader.Read();
                                        screen.X = Convert.ToInt32(xReader.Value);
                                        break;

                                    case SCREEN_Y:
                                        xReader.Read();
                                        screen.Y = Convert.ToInt32(xReader.Value);
                                        break;

                                    case SCREEN_WIDTH:
                                        xReader.Read();
                                        screen.Width = Convert.ToInt32(xReader.Value);
                                        break;

                                    case SCREEN_HEIGHT:
                                        xReader.Read();
                                        screen.Height = Convert.ToInt32(xReader.Value);
                                        break;
                                }
                            }
                        }

                        xReader.Close();

                        // Change the data for each Screen that's being loaded if we've detected that
                        // the XML document is from an older version of the application.
                        if (Settings.VersionManager.IsOldAppVersion(AppCodename, AppVersion))
                        {
                            Log.WriteDebugMessage("An old version of the screens.xml file was detected. Attempting upgrade to new schema.");

                            Version v2300 = Settings.VersionManager.Versions.Get("Boombayah", "2.3.0.0");
                            Version v2340 = Settings.VersionManager.Versions.Get("Boombayah", "2.3.4.0");
                            Version configVersion = Settings.VersionManager.Versions.Get(AppCodename, AppVersion);

                            if (v2300 != null && configVersion != null && configVersion.VersionNumber < v2300.VersionNumber)
                            {
                                Log.WriteDebugMessage("Dalek 2.2.4.6 or older detected");

                                // This is a new property for Screen that was introduced in 2.3.0.0
                                // so any version before 2.3.0.0 needs to have it during an upgrade.
                                screen.Active = true;
                            }

                            if (v2340 != null && configVersion != null && configVersion.VersionNumber < v2340.VersionNumber)
                            {
                                Log.WriteDebugMessage("Boombayah 2.3.3.2 or older detected");

                                int component = 1;

                                foreach (System.Windows.Forms.Screen screenFromWindows in System.Windows.Forms.Screen.AllScreens)
                                {
                                    ScreenCapture.DeviceResolution deviceResolution = ScreenCapture.GetDeviceResolution(screenFromWindows);

                                    if (screen.Component.Equals(component))
                                    {
                                        screen.X = screenFromWindows.Bounds.X;
                                        screen.Y = screenFromWindows.Bounds.Y;
                                        screen.Width = deviceResolution.width;
                                        screen.Height = deviceResolution.height;
                                    }

                                    component++;
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(screen.Name))
                        {
                            Add(screen);
                        }
                    }

                    if (Settings.VersionManager.IsOldAppVersion(AppCodename, AppVersion))
                    {
                        Log.WriteDebugMessage("Screens file detected as an old version");
                        SaveToXmlFile();
                    }
                }
                else
                {
                    Log.WriteDebugMessage("WARNING: Unable to load screens. Initializing screen collection with Reset Screen Collection action");

                    Reset();

                    SaveToXmlFile();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteExceptionMessage("ScreenCollection::LoadXmlFileAndAddScreens", ex);

                return false;
            }
        }

        /// <summary>
        /// Saves the screens.
        /// </summary>
        public bool SaveToXmlFile()
        {
            try
            {
                XmlWriterSettings xSettings = new XmlWriterSettings();
                xSettings.Indent = true;
                xSettings.CloseOutput = true;
                xSettings.CheckCharacters = true;
                xSettings.Encoding = Encoding.UTF8;
                xSettings.NewLineChars = Environment.NewLine;
                xSettings.IndentChars = XML_FILE_INDENT_CHARS;
                xSettings.NewLineHandling = NewLineHandling.Entitize;
                xSettings.ConformanceLevel = ConformanceLevel.Document;

                if (string.IsNullOrEmpty(FileSystem.ScreensFile))
                {
                    FileSystem.ScreensFile = FileSystem.DefaultScreensFile;

                    if (FileSystem.FileExists(FileSystem.ConfigFile))
                    {
                        FileSystem.AppendToFile(FileSystem.ConfigFile, "\nScreensFile=" + FileSystem.ScreensFile);
                    }
                }

                if (FileSystem.FileExists(FileSystem.ScreensFile))
                {
                    FileSystem.DeleteFile(FileSystem.ScreensFile);
                }

                using (XmlWriter xWriter =
                    XmlWriter.Create(FileSystem.ScreensFile, xSettings))
                {
                    xWriter.WriteStartDocument();
                    xWriter.WriteStartElement(XML_FILE_ROOT_NODE);
                    xWriter.WriteAttributeString("app", "version", XML_FILE_ROOT_NODE, Settings.ApplicationVersion);
                    xWriter.WriteAttributeString("app", "codename", XML_FILE_ROOT_NODE, Settings.ApplicationCodename);
                    xWriter.WriteStartElement(XML_FILE_SCREENS_NODE);

                    foreach (Screen screen in base.Collection)
                    {
                        xWriter.WriteStartElement(XML_FILE_SCREEN_NODE);

                        xWriter.WriteElementString(SCREEN_ACTIVE, screen.Active.ToString());
                        xWriter.WriteElementString(SCREEN_VIEWID, screen.ViewId.ToString());
                        xWriter.WriteElementString(SCREEN_NAME, screen.Name);
                        xWriter.WriteElementString(SCREEN_FOLDER, FileSystem.CorrectScreenshotsFolderPath(screen.Folder));
                        xWriter.WriteElementString(SCREEN_MACRO, screen.Macro);
                        xWriter.WriteElementString(SCREEN_COMPONENT, screen.Component.ToString());
                        xWriter.WriteElementString(SCREEN_FORMAT, screen.Format.Name);
                        xWriter.WriteElementString(SCREEN_JPEG_QUALITY, screen.JpegQuality.ToString());
                        xWriter.WriteElementString(SCREEN_RESOLUTION_RATIO, screen.ResolutionRatio.ToString());
                        xWriter.WriteElementString(SCREEN_MOUSE, screen.Mouse.ToString());
                        xWriter.WriteElementString(SCREEN_X, screen.X.ToString());
                        xWriter.WriteElementString(SCREEN_Y, screen.Y.ToString());
                        xWriter.WriteElementString(SCREEN_WIDTH, screen.Width.ToString());
                        xWriter.WriteElementString(SCREEN_HEIGHT, screen.Height.ToString());

                        xWriter.WriteEndElement();
                    }

                    xWriter.WriteEndElement();
                    xWriter.WriteEndElement();
                    xWriter.WriteEndDocument();

                    xWriter.Flush();
                    xWriter.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteExceptionMessage("ScreenCollection::SaveToXmlFile", ex);

                return false;
            }
        }

        /// <summary>
        /// Resets the screen collection with the available screens provided by Windows.
        /// </summary>
        public void Reset()
        {
            base.Collection.Clear();

            int component = 1;

            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                ScreenCapture.DeviceResolution deviceResolution = ScreenCapture.GetDeviceResolution(screen);

                Add(new Screen()
                {
                    ViewId = Guid.NewGuid(),
                    Name = "Screen " + component,
                    Folder = FileSystem.ScreenshotsFolder,
                    Macro = MacroParser.DefaultMacro,
                    Component = component,
                    Format = _imageFormatCollection.GetByName(ScreenCapture.DefaultImageFormat),
                    JpegQuality = 100,
                    ResolutionRatio = 100,
                    Mouse = true,
                    Active = true,
                    X = screen.Bounds.X,
                    Y = screen.Bounds.Y,
                    Width = deviceResolution.width,
                    Height = deviceResolution.height
                });

                Log.WriteDebugMessage($"Screen {component} created using \"{FileSystem.ScreenshotsFolder}\" for folder path and \"{MacroParser.DefaultMacro}\" for macro.");

                component++;
            }
        }
    }
}