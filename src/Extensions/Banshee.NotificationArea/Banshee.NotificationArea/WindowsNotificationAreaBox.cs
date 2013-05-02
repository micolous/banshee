//
// WindowsNotificationAreaBox.cs
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

#if WIN32

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using Banshee.Collection;
using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Gtk;

namespace Banshee.NotificationArea
{
    public class WindowsNotificationAreaBox : INotificationAreaBox
    {

        NotifyIcon icon;
        private TrackInfo current_track;
        private string notify_last_title;
        private string notify_last_artist;
        private MouseEventArgs _lastRightClick = null;


        public event EventHandler Disconnected;
        
        public event EventHandler Activated {
            add { icon.Click += value; }
            remove { icon.Click -= value; }
        }
        
        public event PopupMenuHandler PopupMenuEvent;
        
        public Widget Widget {
            get { return null; }
        }
        
        public WindowsNotificationAreaBox (BaseClientWindow window, NotificationAreaService service)
        {
            icon = new NotifyIcon();

            icon.Visible = false;

            Gdk.Pixbuf iconBuf = IconThemeUtils.LoadIcon( IconThemeUtils.HasIcon("banshee-panel") ? "banshee-panel" : Banshee.ServiceStack.Application.IconName);

            TypeConverter tc = TypeDescriptor.GetConverter(typeof(Bitmap));
            Bitmap b = (Bitmap)tc.ConvertFrom (iconBuf.SaveToBuffer("png"));
            icon.Icon = System.Drawing.Icon.FromHandle(b.GetHicon());

            // Note: Windows has a maximum tooltip length on NotifyIcon of 64 bytes.
            // It will throw an exception if it is longer. This is **not** documented on MSDN.
            icon.Text = TrimString(window.Title, 64);
            window.TitleChanged += delegate { icon.Text = TrimString(window.Title, 64); };

            icon.MouseClick += HandleMouseClick;

            service.TrackNotification += ShowTrackNotification;

        }

        public void Dispose() {
            icon.Dispose();
        }

        private string TrimString(string input, int max_length) {
            // trim a string to a maximum length.

            if (input.Length <= max_length) 
                return input;
            return input.Substring(0, max_length);
        }

        void HandleMouseClick (object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) {
                // fire off context menu event
                // we don't need to send arguments because they're not used!
                _lastRightClick = e;
                PopupMenuEvent(this, null);
            }
        }
        
        public void PositionMenu (Gtk.Menu menu, out int x, out int y, out bool push_in)
        {
            if (_lastRightClick != null) {
                // we can try and give the hint of where we were last clicked.
                x = _lastRightClick.X;
                y = _lastRightClick.Y;
                push_in = true;
            } else {
                // we don't know what is going on here...
                x = y = 0;
                push_in = true;
            }
        }


        public void OnPlayerEvent (PlayerEventArgs args)
        {
        }

        public void ShowTrackNotification(object sender, TrackInfo current_track) {
            string message = NotificationAreaService.GetByFrom (
                current_track.ArtistName, current_track.DisplayArtistName,
                current_track.AlbumTitle, current_track.DisplayAlbumTitle, false);


            // per http://msdn.microsoft.com/en-us/library/windows/desktop/ee330740(v=vs.85).aspx#define_notification
            //  Title length: max 48 bytes "to accomodate localisation"
            //  Message length: max 200 bytes "to accomodate localisation"
            //
            // We already have a localised string at this point.  So these probably
            // correspond to powers of 2 (64 and 256 respectively).
            //
            // As the tooltip length limit isn't even documented, this is a guess!!
            icon.ShowBalloonTip(4500,
                                TrimString(current_track.DisplayTrackTitle, 64),
                                TrimString(message, 256),
                                ToolTipIcon.None);

        }






        public void Show ()
        {
            icon.Visible = true;
        }
        
        public void Hide ()
        {
            icon.Visible = false;
        }



    }
}
#endif
