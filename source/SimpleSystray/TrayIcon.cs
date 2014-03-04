using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Xappy
{
    public class TrayIcon: IDisposable
    {
		private NotifyIcon _notificationIcon;
        private Icon _icon;
        private bool _showingBalloon;
        public int BalloonTipLifeTimeInMS { get; set; }

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

        public string TipText { get; set; }
        public string TipTitle { get; set; }

        public TrayIcon(Icon icon)
		{
            this._icon = icon;
            BalloonTipLifeTimeInMS = 2000;
            _notificationIcon = new NotifyIcon();
            _notificationIcon.ContextMenu = new ContextMenu();
            _notificationIcon.MouseMove += ShowBalloonTipForMessage;
		}

        private void ShowBalloonTipForMessage(object sender, MouseEventArgs e)
        {
            if (_showingBalloon) return;
            _showingBalloon = true;
            _notificationIcon.ShowBalloonTip(BalloonTipLifeTimeInMS, TipTitle, TipText, ToolTipIcon.Info);
            Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(BalloonTipLifeTimeInMS);
                    _showingBalloon = false;
                });
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
