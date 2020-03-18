﻿namespace UserInterface.Views
{
    using Gtk;
    using System;
    using Interfaces;

    /// <summary>Encapsulates a menu item.</summary>
    public class MenuItemView : IMenuItemView
    {
        private Gtk.MenuItem menuItem;

        /// <summary>Constructor</summary>
        public MenuItemView(Gtk.MenuItem item)
        {
            menuItem = item;
            menuItem.Activated += OnMenuClicked;
        }

        /// <summary>Menu clicked event.</summary>
        public event EventHandler Clicked;

        /// <summary>Gets or set the checked status of the menu item.</summary>
        public bool Checked
        {
            get
            {
                if (menuItem is CheckMenuItem)
                    return (menuItem as CheckMenuItem).Active;
                else
                    return false;
            }
            set
            {
                if (menuItem is CheckMenuItem)
                    (menuItem as CheckMenuItem).Active = value;
            }
        }

        /// <summary>Destroy the menu</summary>
        public void Destroy()
        {
            menuItem.Activated -= OnMenuClicked;
            menuItem.Destroy();
        }

        private void OnMenuClicked(object sender, EventArgs e)
        {
            try
            {
                Clicked?.Invoke(this, e);
            }
            catch// (Exception err)
            {
                // todo - how should we handle errors in here? For now just
                // swallow any exceptions. It's better than crashing, right?
                //ShowError(err);
            }
        }

    }
}
