//
// ColumnController.cs
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
using System.Collections;
using System.Collections.Generic;

namespace Hyena.Data.Gui
{    
    public class ColumnController : IEnumerable<Column>
    {
        private List<Column> columns = new List<Column> ();
        
        protected List<Column> Columns {
            get { return columns; }
        }
        
        public event EventHandler Updated;
        
        protected virtual void OnUpdated ()
        {
            EventHandler handler = Updated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        public void Clear ()
        {
            lock (this) {
                columns.Clear ();
            }
            
            OnUpdated ();
        }

        public void AddRange (params Column [] range)
        {
            lock (this) {
                columns.AddRange (range);
            }

            OnUpdated ();
        }

        public void Add (Column column)
        {
            lock (this) {
                columns.Add (column);
            }
            
            OnUpdated ();
        }
        
        public void Insert (Column column, int index)
        {
            lock (this) {
                columns.Insert (index, column);
            }
            
            OnUpdated ();
        }
        
        public void Remove (Column column)
        {
            lock (this) {
                columns.Remove (column);
            }
            
            OnUpdated ();
        }
        
        public void Remove (int index)
        {
            lock (this) {
                columns.RemoveAt (index);
            }
            
            OnUpdated ();
        }
        
        public void Reorder (int index, int newIndex)
        {
            lock (this) {
                Column column = columns[index];
                columns.RemoveAt (index);
                columns.Insert (newIndex, column);
            }
            
            OnUpdated ();
        }
        
        IEnumerator IEnumerable.GetEnumerator ()
        {
            return columns.GetEnumerator ();
        }
        
        IEnumerator<Column> IEnumerable<Column>.GetEnumerator ()
        {
            return columns.GetEnumerator ();
        }
        
        public int IndexOf (Column column)
        {
            lock (this) {
                return columns.IndexOf (column);
            }
        }
        
        public Column this[int index] {
            get { return columns[index]; }
        }
        
        public int Count {
            get { return columns.Count; }
        }
        
        public virtual bool EnableColumnMenu {
            get { return false; }
        }
    }
}
