import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Bell } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { usePostPebble } from '../contexts/PostPebbleContext';

function formatRelativeDate(value: string) {
  return new Date(value).toLocaleString();
}

export function NotificationBell() {
  const {
    unreadNotificationCount,
    recentNotifications,
    loadNotificationSummary,
    loadRecentNotifications,
    markNotificationRead,
    markAllNotificationsRead,
  } = usePostPebble();
  const [open, setOpen] = useState(false);
  const panelRef = useRef<HTMLDivElement | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    loadNotificationSummary();
    loadRecentNotifications();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (!panelRef.current) return;
      if (!panelRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    }

    if (open) {
      window.addEventListener('mousedown', handleClickOutside);
    }

    return () => window.removeEventListener('mousedown', handleClickOutside);
  }, [open]);

  const badgeText = useMemo(() => {
    if (unreadNotificationCount <= 0) return '';
    return unreadNotificationCount > 9 ? '9+' : String(unreadNotificationCount);
  }, [unreadNotificationCount]);

  const handleOpenPage = () => {
    setOpen(false);
    navigate('/notifications');
  };

  const handleNotificationClick = async (notificationId: string, linkUrl?: string | null) => {
    await markNotificationRead(notificationId);
    setOpen(false);
    navigate(linkUrl || '/notifications');
  };

  return (
    <div className="notification-bell-shell" ref={panelRef}>
      <button
        type="button"
        className="notification-bell-button"
        onClick={() => setOpen((current) => !current)}
        aria-label="Notifications"
      >
        <Bell size={18} />
        {badgeText && <span className="notification-bell-badge">{badgeText}</span>}
      </button>

      {open && (
        <div className="notification-panel">
          <div className="notification-panel-header">
            <div>
              <h4 style={{ marginBottom: '0.2rem' }}>Notifications</h4>
              <div className="text-sm">{unreadNotificationCount} unread</div>
            </div>
            <button type="button" className="btn-secondary" style={{ padding: '0.5rem 0.75rem' }} onClick={() => void markAllNotificationsRead()}>
              Mark all read
            </button>
          </div>

          <div className="notification-list">
            {recentNotifications.length === 0 ? (
              <div className="notification-empty">No notifications yet.</div>
            ) : (
              recentNotifications.map((notification) => (
                <button
                  key={notification.id}
                  type="button"
                  className={`notification-list-item ${notification.isRead ? 'read' : 'unread'}`}
                  onClick={() => void handleNotificationClick(notification.id, notification.linkUrl)}
                >
                  <div className="notification-list-item-title-row">
                    <span className="notification-list-item-title">{notification.title}</span>
                    {!notification.isRead && <span className="notification-dot" />}
                  </div>
                  <div className="notification-list-item-body">{notification.body}</div>
                  <div className="notification-list-item-meta">
                    <span>{notification.tenantName ?? 'Workspace'}</span>
                    <span>{formatRelativeDate(notification.createdAtUtc)}</span>
                  </div>
                </button>
              ))
            )}
          </div>

          <button type="button" className="notification-show-more" onClick={handleOpenPage}>
            Show more
          </button>
        </div>
      )}
    </div>
  );
}
