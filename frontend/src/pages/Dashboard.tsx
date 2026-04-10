import React, { useEffect } from 'react';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { CreditCard, CalendarClock, Link as LinkIcon, Image as ImageIcon, TrendingUp, PieChart, CheckCircle2, AlertTriangle } from 'lucide-react';
import { Link } from 'react-router-dom';
import { AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell, PieChart as RPieChart, Pie, Legend } from 'recharts';

const PLATFORM_COLORS: Record<string, string> = {
  LinkedIn: '#0a66c2',
  X: '#1da1f2',
  Meta: '#1877f2',
  TikTok: '#ff0050',
};

export function Dashboard() {
  const { 
    walletBalance, loadWallet,
    scheduledPosts, loadScheduledPosts,
    mediaAssets, loadMedia,
    linkedInConnections, loadLinkedInConnections,
    analytics, loadAnalytics,
  } = usePostPebble();

  useEffect(() => {
    loadWallet();
    loadScheduledPosts();
    loadMedia();
    loadLinkedInConnections();
    loadAnalytics();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const statCards = [
    { label: 'Available Credits', value: walletBalance ?? '-', icon: CreditCard, link: '/billing', color: 'var(--highlight-blue)' },
    { label: 'Scheduled Posts', value: scheduledPosts.length, icon: CalendarClock, link: '/scheduler', color: '#10b981' },
    { label: 'Media Assets', value: mediaAssets.length, icon: ImageIcon, link: '/library', color: '#8b5cf6' },
    { label: 'Active Integrations', value: linkedInConnections.length, icon: LinkIcon, link: '/integrations', color: '#f59e0b' },
  ];

  const hasAnalytics = analytics !== null;

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

      {/* Analytics Section */}
      {hasAnalytics && (
        <>
          {/* Performance Summary Cards */}
          <div className="grid mt-6" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', maxWidth: '100%' }}>
            <div className="card" style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '1.25rem', marginBottom: 0 }}>
              <div style={{ background: 'rgba(74, 222, 128, 0.15)', padding: '10px', borderRadius: '10px' }}>
                <CheckCircle2 size={22} color="#4ade80" />
              </div>
              <div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Published (7d)</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 700, fontFamily: 'Outfit' }}>{analytics.publishedLast7Days}</div>
              </div>
            </div>
            <div className="card" style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '1.25rem', marginBottom: 0 }}>
              <div style={{ background: 'rgba(51, 183, 255, 0.15)', padding: '10px', borderRadius: '10px' }}>
                <TrendingUp size={22} color="var(--highlight-blue)" />
              </div>
              <div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Published (30d)</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 700, fontFamily: 'Outfit' }}>{analytics.publishedLast30Days}</div>
              </div>
            </div>
            <div className="card" style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '1.25rem', marginBottom: 0 }}>
              <div style={{ background: 'rgba(239, 68, 68, 0.15)', padding: '10px', borderRadius: '10px' }}>
                <AlertTriangle size={22} color="#ef4444" />
              </div>
              <div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Failed (30d)</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 700, fontFamily: 'Outfit' }}>{analytics.failedLast30Days}</div>
              </div>
            </div>
            <div className="card" style={{ display: 'flex', alignItems: 'center', gap: '1rem', padding: '1.25rem', marginBottom: 0 }}>
              <div style={{ background: 'rgba(139, 92, 246, 0.15)', padding: '10px', borderRadius: '10px' }}>
                <PieChart size={22} color="#8b5cf6" />
              </div>
              <div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Success Rate</div>
                <div style={{ fontSize: '1.5rem', fontWeight: 700, fontFamily: 'Outfit' }}>{analytics.successRate}%</div>
              </div>
            </div>
          </div>

          {/* Charts */}
          <div className="grid mt-6" style={{ gridTemplateColumns: analytics.platformBreakdown.length > 0 ? '2fr 1fr' : '1fr', gap: '1.5rem', maxWidth: '100%' }}>
            {/* Posts Over Time */}
            <div className="card">
              <h3 style={{ marginBottom: '1.5rem' }}>Publishing Activity (30 Days)</h3>
              {analytics.postsPerDay.length > 0 ? (
                <div style={{ width: '100%', height: 280 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={analytics.postsPerDay} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
                      <defs>
                        <linearGradient id="colorPosts" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="5%" stopColor="#33b7ff" stopOpacity={0.3}/>
                          <stop offset="95%" stopColor="#33b7ff" stopOpacity={0}/>
                        </linearGradient>
                      </defs>
                      <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.06)" />
                      <XAxis 
                        dataKey="date" 
                        stroke="#9ea8c8" 
                        fontSize={11} 
                        tickFormatter={(val) => {
                          const d = new Date(val);
                          return `${d.getMonth()+1}/${d.getDate()}`;
                        }}
                      />
                      <YAxis stroke="#9ea8c8" fontSize={11} allowDecimals={false} />
                      <Tooltip 
                        contentStyle={{ 
                          background: 'var(--bg-secondary)', 
                          border: '1px solid var(--border-subtle)', 
                          borderRadius: '8px',
                          color: 'var(--text-primary)',
                          fontSize: '0.85rem'
                        }}
                        labelFormatter={(val) => new Date(val).toLocaleDateString()}
                      />
                      <Area 
                        type="monotone" 
                        dataKey="count" 
                        stroke="#33b7ff" 
                        strokeWidth={2}
                        fillOpacity={1} 
                        fill="url(#colorPosts)" 
                        name="Posts Published"
                      />
                    </AreaChart>
                  </ResponsiveContainer>
                </div>
              ) : (
                <p className="text-sm" style={{ textAlign: 'center', padding: '3rem 0' }}>No publishing activity yet. Schedule and publish posts to see trends.</p>
              )}
            </div>

            {/* Platform Breakdown */}
            {analytics.platformBreakdown.length > 0 && (
              <div className="card">
                <h3 style={{ marginBottom: '1.5rem' }}>Platform Split</h3>
                <div style={{ width: '100%', height: 280 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <RPieChart>
                      <Pie
                        data={analytics.platformBreakdown}
                        cx="50%"
                        cy="50%"
                        innerRadius={60}
                        outerRadius={90}
                        paddingAngle={5}
                        dataKey="count"
                        nameKey="platform"
                        label={({ name, percent }: { name?: string; percent?: number }) => `${name ?? ''} ${((percent ?? 0) * 100).toFixed(0)}%`}
                        labelLine={false}
                      >
                        {analytics.platformBreakdown.map((entry, index) => (
                          <Cell 
                            key={`cell-${index}`} 
                            fill={PLATFORM_COLORS[entry.platform] ?? `hsl(${index * 90}, 70%, 60%)`} 
                          />
                        ))}
                      </Pie>
                      <Tooltip 
                        contentStyle={{ 
                          background: 'var(--bg-secondary)', 
                          border: '1px solid var(--border-subtle)', 
                          borderRadius: '8px',
                          color: 'var(--text-primary)',
                        }}
                      />
                      <Legend 
                        verticalAlign="bottom" 
                        iconType="circle" 
                        formatter={(value) => <span style={{ color: 'var(--text-secondary)', fontSize: '0.8rem' }}>{value}</span>}
                      />
                    </RPieChart>
                  </ResponsiveContainer>
                </div>
              </div>
            )}
          </div>

          {/* Credit Usage */}
          <div className="grid mt-6" style={{ gridTemplateColumns: '1fr 1fr', gap: '1.5rem', maxWidth: '100%' }}>
            <div className="card" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Credits Purchased (30d)</div>
                <div style={{ fontSize: '2rem', fontWeight: 700, fontFamily: 'Outfit', color: '#10b981' }}>+{analytics.creditsPurchasedLast30Days}</div>
              </div>
              <CreditCard size={32} color="#10b981" style={{ opacity: 0.5 }} />
            </div>
            <div className="card" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>Credits Used (30d)</div>
                <div style={{ fontSize: '2rem', fontWeight: 700, fontFamily: 'Outfit', color: '#f59e0b' }}>{analytics.creditsConsumedLast30Days}</div>
              </div>
              <TrendingUp size={32} color="#f59e0b" style={{ opacity: 0.5 }} />
            </div>
          </div>
        </>
      )}

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
