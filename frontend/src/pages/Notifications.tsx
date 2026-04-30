import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { usePostPebble, type NotificationPage } from '../contexts/PostPebbleContext';

function formatTimestamp(value: string) {
  return new Date(value).toLocaleString();
}

export function Notifications() {
  const { loadNotificationsPage, markNotificationRead, markAllNotificationsRead } = usePostPebble();
  const [page, setPage] = useState(1);
  const [data, setData] = useState<NotificationPage | null>(null);
  const navigate = useNavigate();
  const pageSize = 10;

  useEffect(() => {
    void (async () => {
      const response = await loadNotificationsPage(page, pageSize);
      setData(response);
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  const totalPages = Math.max(1, Math.ceil((data?.totalCount ?? 0) / pageSize));

  const handleOpenNotification = async (notificationId: string, linkUrl?: string | null) => {
    await markNotificationRead(notificationId);
    setData((current) =>
      current
        ? {
            ...current,
            items: current.items.map((notification) =>
              notification.id === notificationId ? { ...notification, isRead: true } : notification
            ),
          }
        : current
    );
    if (linkUrl) {
      navigate(linkUrl);
    }
  };

  const handleMarkAllRead = async () => {
    await markAllNotificationsRead();
    setData((current) =>
      current
        ? {
            ...current,
            items: current.items.map((notification) => ({ ...notification, isRead: true })),
          }
        : current
    );
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <div>
          <h2>Notifications</h2>
          <p className="text-sm">Recent activity across your workspace.</p>
        </div>
        <button type="button" className="btn-secondary" onClick={() => void handleMarkAllRead()}>
          Mark all read
        </button>
      </div>

      <div className="card">
        {!data || data.items.length === 0 ? (
          <div className="notification-empty-page">No notifications yet.</div>
        ) : (
          <div className="notification-page-list">
            {data.items.map((notification) => (
              <button
                type="button"
                key={notification.id}
                className={`notification-page-item ${notification.isRead ? 'read' : 'unread'}`}
                onClick={() => void handleOpenNotification(notification.id, notification.linkUrl)}
              >
                <div className="notification-page-item-header">
                  <div>
                    <div className="notification-page-item-title">{notification.title}</div>
                    <div className="notification-page-item-tenant">{notification.tenantName ?? 'Workspace'}</div>
                  </div>
                  <div className="notification-page-item-date">{formatTimestamp(notification.createdAtUtc)}</div>
                </div>
                <div className="notification-page-item-body">{notification.body}</div>
              </button>
            ))}
          </div>
        )}

        <div className="flex justify-between items-center mt-6">
          <button
            type="button"
            className="btn-secondary"
            disabled={page <= 1}
            onClick={() => setPage((current) => current - 1)}
          >
            Previous
          </button>
          <span className="text-sm">Page {page} of {totalPages}</span>
          <button
            type="button"
            className="btn-secondary"
            disabled={page >= totalPages}
            onClick={() => setPage((current) => current + 1)}
          >
            Next
          </button>
        </div>
      </div>
    </div>
  );
}
