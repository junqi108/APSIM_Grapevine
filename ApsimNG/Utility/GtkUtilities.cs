namespace Utility
{
    using Gtk;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    public static class GtkUtil
    {
        /// <summary>
        /// Detaches all event handlers on the widget and all descendants.
        /// </summary>
        /// <param name="widget">The widget.</param>
        public static void DetachAllHandlers(this Widget widget)
        {
            widget.DetachHandlers();
            if (widget is Container container)
                foreach (Widget child in container.Children)
                    child.DetachAllHandlers();
        }

        /// <summary>
        /// Detach all event handlers defined in ApsimNG from the widget.
        /// This procedure uses reflection to gain access to non-public properties and fields.
        /// The procedure is not entirely safe, in that it makes assumptions about the internal handling of event
        /// signalling in Gtk#. This may break in future versions if the Gtk# internals change. This sort of
        /// breakage has occurred with an earlier version of this routine.
        /// A "breakage" probably won't be immediately apparent, but may lead to memory leaks, as the main
        /// reason for having this routine is to remove references that can prevent garbage collection.
        /// </summary>
        /// <param name="widget">The widget.</param>
        public static void DetachHandlers(this Widget widget)
        {
            PropertyInfo signals = typeof(GLib.Object).GetProperty("Signals", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            FieldInfo afterHandler = typeof(GLib.Signal).GetField("after_handler", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            FieldInfo beforeHandler = typeof(GLib.Signal).GetField("before_handler", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (signals != null && afterHandler != null && beforeHandler != null)
            {
                Dictionary<string, GLib.Signal> widgetSignals = (Dictionary<string, GLib.Signal>) signals.GetValue(widget);
                foreach (KeyValuePair<string, GLib.Signal> signal in widgetSignals)
                {
                    if (signal.Key != "destroy")
                    {
                        GLib.Signal signalVal = signal.Value;
                        Delegate afterDel = (Delegate)afterHandler.GetValue(signalVal);
                        if (afterDel != null)
                            widget.RemoveSignalHandler(signal.Key, afterDel);
                        Delegate beforeDel = (Delegate)beforeHandler.GetValue(signalVal);
                        if (beforeDel != null)
                            widget.RemoveSignalHandler(signal.Key, beforeDel);
                    }
                }
            }
        }

        /// <summary>
        /// Get child widget at the specified row and column.
        /// </summary>
        /// <param name="table">A table</param>
        /// <param name="row">Row of the widget (top-attach).</param>
        /// <param name="col">Column of the widget (left-attach).</param>
        public static Widget GetChild(this Table table, uint row, uint col)
        {
            foreach (Widget child in table.Children)
            {
                object topAttach = table.ChildGetProperty(child, "top-attach").Val;
                object leftAttach = table.ChildGetProperty(child, "left-attach").Val;
                if (topAttach.GetType() == typeof(uint) && leftAttach.GetType() == typeof(uint))
                {
                    uint rowIndex = (uint)topAttach;
                    uint colIndex = (uint)leftAttach;

                    if (rowIndex == row && colIndex == col)
                        return child;
                }
            }

            return null;
        }

        public static IEnumerable<Widget> Descendants(this Widget widget)
        {
            List<Widget> descendants = new List<Widget>();
            if (widget is Container container)
            {
                foreach (Widget child in container.Children)
                {
                    descendants.Add(child);
                    descendants.AddRange(child.Descendants());
                }
            }
            return descendants;
        }

        public static IEnumerable<Widget> Ancestors(this Widget widget)
        {
            Widget parent = widget?.Parent;
            while (parent != null)
            {
                yield return parent;
                parent = parent.Parent;
            }
        }
    }
}