using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Xappy
{
    public class TrayIcon: IDisposable
    {
		private NotifyIcon _notificationIcon;
        private Icon _icon;
        public Icon Icon
        {
            get
            {
                return _icon;
            }
            set
            {
                _icon = value;
                _notificationIcon.Icon = _icon;
            }
        }

        public string Text
        {
            get
            {
                lock(this)
                {
                    return _notificationIcon == null ? null : _notificationIcon.Text;
                }
            }
            set
            {
                lock(this)
                {
                    if (_notificationIcon == null)
                        return;
                    var text = value == null ? "" : (value.Length >= 60) ? value.Substring(0, 60) + "..." : value;
                    _notificationIcon.Text = text;
                }
            }
        }

        public TrayIcon(Icon icon)
		{
            this._icon = icon;
            _notificationIcon = new NotifyIcon();
            _notificationIcon.ContextMenu = new ContextMenu();
		}


        public void AddMenuItem(string withText, Action withCallback)
        {
            lock(this)
            {
                if (_notificationIcon == null) return;
                var menuItem = new MenuItem()
                               {
                                   Text = withText
                               };
                if (withCallback != null)
                    menuItem.Click += (s, e) => withCallback();
                _notificationIcon.ContextMenu.MenuItems.Add(menuItem);
            }
        }

        public void AddMenuSeparator()
        {
            AddMenuItem("-", null);
        }

        public void RemoveMenuItem(string withText)
        {
            lock(this)
            {
                if (_notificationIcon == null) return;
                foreach (MenuItem mi in _notificationIcon.ContextMenu.MenuItems)
                {
                    if (mi.Text == withText)
                    {
                        _notificationIcon.ContextMenu.MenuItems.Remove(mi);
                        return;
                    }
                }
            }
        }

        public void Show()
		{
			_notificationIcon.MouseClick += onIconMouseClick;
			_notificationIcon.Icon = _icon;
			_notificationIcon.Visible = true;
		}

		public void Dispose()
		{
            lock(this)
            {
                if (_notificationIcon != null)
    			    _notificationIcon.Dispose();
                _notificationIcon = null;
            }
		}

		void onIconMouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
			}
		}
	}
}
