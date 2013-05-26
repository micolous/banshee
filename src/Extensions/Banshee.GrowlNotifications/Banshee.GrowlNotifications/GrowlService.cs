//
// MyClass.cs
//
// Author:
//   Michael Farrell <micolous+git@gmail.com>
//
// Copyright 2013 Michael Farrell
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.ComponentModel;
using System.Drawing;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.Collection.Gui;
using Banshee.MediaEngine;

using Banshee.IO;

using Hyena;
using Hyena.Gui;
using Hyena.Widgets;

using Mono.Unix;

using Growl.Connector;


// based on Banshee.NotificationArea.NotificationAreaService.

namespace Banshee.GrowlNotifications
{
    public class GrowlService : IExtensionService
    {

        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
        private ArtworkManager artwork_manager_service;
        private bool disposed;

        private string notify_last_title;
        private string notify_last_artist;
        private TrackInfo current_track;

        private const int icon_size = 42;

        private GrowlConnector growl;
        private Growl.Connector.Application growl_application;
        private NotificationType change;

        private const string GROWL_APPLICATION = "Banshee";
        private const string GROWL_NOTIFICATION_CHANGE = "CHANGE";


        public GrowlService ()
        {
        }

        string IService.ServiceName {
            get { return "GrowlService"; }
        }

        void IExtensionService.Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();
            
            if (!ServiceStartup ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }
        
        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is Banshee.Gui.InterfaceActionService) {
                interface_action_service = (InterfaceActionService)args.Service;
            } else if (args.Service is GtkElementsService) {
                elements_service = (GtkElementsService)args.Service;
            }
            
            ServiceStartup ();
        }
        
        private bool ServiceStartup ()
        {
            if (elements_service == null || interface_action_service == null) {
                return false;
            }

            Initialize();

            ServiceManager.ServiceStarted -= OnServiceStarted;

            return true;
        }


        private void Initialize ()
        {
            // Fetch ourselves a pretty icon we can pass to Growl, converting it into
            // a format that System.Drawing (and thus GfW) can understand.
            //
            // FIXME: This icon is quite low resolution.
            Gdk.Pixbuf iconBuf = IconThemeUtils.LoadIcon( IconThemeUtils.HasIcon("banshee-panel") ? "banshee-panel" : Banshee.ServiceStack.Application.IconName);
            TypeConverter tc = TypeDescriptor.GetConverter(typeof(Bitmap));
            Bitmap icon = (Bitmap)tc.ConvertFrom (iconBuf.SaveToBuffer("png"));

            // Register ourselves with growl
            // TODO: Implement support for connecting to remote Growl instances, and those with passwords.
            growl = new GrowlConnector();
            growl_application = new Growl.Connector.Application(GROWL_APPLICATION);
            growl_application.Icon = icon;

            change = new NotificationType(GROWL_NOTIFICATION_CHANGE, "Song Change");
            growl.Register (growl_application, new NotificationType[] { change });

            // Register with Banshee
            ServiceManager.PlayerEngine.ConnectEvent (OnPlayerEvent,
                                                  PlayerEvent.StartOfStream |
                                                  PlayerEvent.EndOfStream |
                                                  PlayerEvent.TrackInfoUpdated |
                                                  PlayerEvent.StateChange);

            // Unsure if this is still needed as the artwork is grabbed as a URI from metadata.
            artwork_manager_service = ServiceManager.Get<ArtworkManager> ();
            artwork_manager_service.AddCachedSize (icon_size);
        }

        public void Dispose ()
        {
            if (disposed) {
                return;
            }

            ServiceManager.PlayerEngine.DisconnectEvent (OnPlayerEvent);

            elements_service = null;
            interface_action_service = null;
            
            disposed = true;
        }

        private void OnPlayerEvent (PlayerEventArgs args)
        {
            switch (args.Event) {
            case PlayerEvent.StartOfStream:
            case PlayerEvent.TrackInfoUpdated:
                current_track = ServiceManager.PlayerEngine.CurrentTrack;
                ShowTrackNotification ();
                break;
            case PlayerEvent.EndOfStream:
                current_track = null;
                break;
            }
        }

        private void ShowTrackNotification ()
        {
            // This has to happen before the next if, otherwise the last_* members aren't set correctly.
            if (current_track == null || (notify_last_title == current_track.DisplayTrackTitle
                                          && notify_last_artist == current_track.DisplayArtistName)) {
                return;
            }
            
            notify_last_title = current_track.DisplayTrackTitle;
            notify_last_artist = current_track.DisplayArtistName;

            // don't show a notification if we have focus.
            // disabled: GfW supports forwarding events to other devices / applications so disabling it
            // like this might not be good.
            /*
            foreach (var window in elements_service.ContentWindows) {
                if (window.HasToplevelFocus) {
                    return;
                }
            }
            */

            string message = GetByFrom (
                current_track.ArtistName, current_track.DisplayArtistName,
                current_track.AlbumTitle, current_track.DisplayAlbumTitle);
            
            string image = null;
            
            image = CoverArtSpec.GetPath (current_track.ArtworkId);
            
            if (!File.Exists (new SafeUri(image))) {
                // file does not exist, there is no cover artwork
                image = null;
            }

            // show the notification.

            Hyena.Log.Information(String.Format("GrowlService: change to {0}, img = {1}", message, image));

            Notification notification = new Notification(
                GROWL_APPLICATION,
                GROWL_NOTIFICATION_CHANGE,
                null, // notification identifier, we don't care about these
                current_track.DisplayTrackTitle,
                message
                );

            // some different plugins in Growl may use these attributes.
            if (!String.IsNullOrEmpty(current_track.ArtistName))
                notification.CustomTextAttributes.Add("Artist", current_track.DisplayArtistName);

            if (!String.IsNullOrEmpty(current_track.AlbumTitle))
                notification.CustomTextAttributes.Add("Album", current_track.DisplayAlbumTitle);

            notification.CustomTextAttributes.Add("Rating", current_track.Rating.ToString());

            if (image != null)
                notification.Icon = image;

            growl.Notify(notification);

        }
     
        
        private string GetByFrom (string artist, string display_artist, string album, string display_album)
        {
            // Growl doesn't use markup in notifications, so don't send any.
            bool has_artist = !String.IsNullOrEmpty (artist);
            bool has_album = !String.IsNullOrEmpty (album);
            
            string markup = null;
            if (has_artist && has_album) {
                // Translators: {0} and {1} are Artist Name and Album Title, respectively;
                // e.g. 'by Parkway Drive from Killing with a Smile'
                markup = string.Format (Catalog.GetString ("by {0}\nfrom {1}"), display_artist, display_album);
            } else if (has_album) {
                // Translators: {0} is for Album Title;
                // e.g. 'from Killing with a Smile'
                markup = string.Format (Catalog.GetString ("from {0}"), display_album);
            } else {
                // Translators: {0} is for Artist Name;
                // e.g. 'by Parkway Drive'
                markup = string.Format (Catalog.GetString ("by {0}"), display_artist);
            }
            return markup;
        }
    }
}

