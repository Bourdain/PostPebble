import React, { useEffect } from 'react';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { CreditCard, CalendarClock, Link as LinkIcon, Image as ImageIcon } from 'lucide-react';
import { Link } from 'react-router-dom';

export function Dashboard() {
  const { 
    walletBalance, loadWallet,
    scheduledPosts, loadScheduledPosts,
    mediaAssets, loadMedia,
    linkedInConnections, loadLinkedInConnections 
  } = usePostPebble();

  useEffect(() => {
    loadWallet();
    loadScheduledPosts();
    loadMedia();
    loadLinkedInConnections();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const statCards = [
    { label: 'Available Credits', value: walletBalance ?? '-', icon: CreditCard, link: '/billing', color: 'var(--highlight-blue)' },
    { label: 'Scheduled Posts', value: scheduledPosts.length, icon: CalendarClock, link: '/scheduler', color: '#10b981' },
    { label: 'Media Assets', value: mediaAssets.length, icon: ImageIcon, link: '/library', color: '#8b5cf6' },
    { label: 'Active Integrations', value: linkedInConnections.length, icon: LinkIcon, link: '/integrations', color: '#f59e0b' },
  ];

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <div>
          <h2>Dashboard</h2>
          <p className="text-sm">Overview of your PostPebble workspace</p>
        </div>
      </div>

      <div className="grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))', maxWidth: '100%' }}>
        {statCards.map((stat, idx) => (
          <Link to={stat.link} key={idx} style={{ textDecoration: 'none' }}>
            <div className="card h-full" style={{ display: 'flex', flexDirection: 'column', padding: '1.5rem', marginBottom: 0 }}>
              <div className="flex justify-between items-center mb-4">
                <span className="text-sm">{stat.label}</span>
                <stat.icon size={24} color={stat.color} />
              </div>
              <h3 style={{ fontSize: '2rem', margin: 0 }}>{stat.value}</h3>
            </div>
          </Link>
        ))}
      </div>

      <div className="card mt-6">
        <h3>Recent Queue</h3>
        {scheduledPosts.length === 0 ? (
          <p className="text-sm">No recent posts scheduled. Head over to the Scheduler.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Content</th>
                <th>Scheduled For</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {scheduledPosts.slice(0, 5).map(post => (
                <tr key={post.id}>
                  <td>
                    <div style={{ maxWidth: '400px', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
                      {post.textContent}
                    </div>
                  </td>
                  <td>{new Date(post.scheduledAtUtc).toLocaleString()}</td>
                  <td>
                    <span className={`statusBadge ${post.status.toLowerCase()}`}>{post.status}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
