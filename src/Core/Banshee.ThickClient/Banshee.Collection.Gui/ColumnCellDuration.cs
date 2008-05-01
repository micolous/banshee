// 
// ColumnCellDuration.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Hyena.Data.Gui;

namespace Banshee.Collection.Gui
{
    public class ColumnCellDuration : ColumnCellText
    {
        public ColumnCellDuration (string property, bool expand) : base (property, expand)
        {
        }
        
        protected override string Text {
            get {
                if (!(BoundObject is TimeSpan)) {
                    return base.Text;
                }
                
                // Fancy rounding commented out since it's not consistent with what is
                // done in libbanshee.  See http://bugzilla.gnome.org/show_bug.cgi?id=520648
                //int seconds = (int)Math.Round(((TimeSpan)BoundObject).TotalSeconds);
                int seconds = (int) ((TimeSpan)BoundObject).TotalSeconds;
                
                return seconds >= 3600 ? 
                    String.Format ("{0}:{1:00}:{2:00}", seconds / 3600, (seconds / 60) % 60, seconds % 60) :
                    String.Format ("{0}:{1:00}", seconds / 60, seconds % 60);
            }
        }
    }
}
