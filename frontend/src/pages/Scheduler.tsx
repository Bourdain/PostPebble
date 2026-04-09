import React, { useState, useEffect } from 'react';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { Send, Image as ImageIcon } from 'lucide-react';


export function Scheduler() {
  const { 
    scheduledPosts, loadScheduledPosts, createScheduledPost, 
    linkedInConnections, loadLinkedInConnections,
    mediaAssets, loadMedia
  } = usePostPebble();

  const [textContent, setTextContent] = useState('');
  const [scheduledAtUtc, setScheduledAtUtc] = useState(new Date().toISOString().slice(0,16));
  const [selectedTargets, setSelectedTargets] = useState<{platform: string; externalAccountId: string}[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    loadScheduledPosts();
    loadLinkedInConnections();
    loadMedia();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleToggleTarget = (platform: string, accountId: string) => {
    setSelectedTargets(prev => {
      const exists = prev.find(p => p.platform === platform && p.externalAccountId === accountId);
      if (exists) return prev.filter(p => !(p.platform === platform && p.externalAccountId === accountId));
      return [...prev, { platform, externalAccountId: accountId }];
    });
  };

  const handleSchedule = async () => {
    if (selectedTargets.length === 0) return;
    setIsSubmitting(true);
    // Convert local datetime to UTC for API
    const dateObj = new Date(scheduledAtUtc);
    await createScheduledPost(textContent, dateObj.toISOString(), selectedTargets, []); // Note: leaving media out of scheduler UI to keep it simple, per old app
    setIsSubmitting(false);
    setTextContent('');
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <div>
          <h2>Composer</h2>
          <p className="text-sm">Create and schedule a new post.</p>
        </div>
      </div>

      <div className="grid" style={{ gridTemplateColumns: 'minmax(400px, 1.5fr) 1fr', gap: '2rem', maxWidth: '100%' }}>
        <div className="card">
          <div className="mb-4">
            <h4 style={{ marginBottom: '0.5rem' }}>Select Destinations</h4>
            <div className="flex flex-wrap gap-2">
              {linkedInConnections.map(conn => {
                const isSelected = selectedTargets.some(t => t.platform === 'LinkedIn' && t.externalAccountId === conn.memberUrn);
                return (
                  <button 
                    key={conn.id}
                    className={`btn-secondary ${isSelected ? 'active' : ''}`}
                    style={isSelected ? { background: 'var(--highlight-blue)', color: '#fff', borderColor: 'var(--highlight-blue)' } : {}}
                    onClick={() => conn.memberUrn && handleToggleTarget('LinkedIn', conn.memberUrn)}
                    disabled={!conn.memberUrn}
                  >
                    LinkedIn {conn.memberUrn ? `(${conn.memberUrn.slice(-4)})` : '(No URN)'}
                  </button>
                );
              })}
              {/* Fake X Target for UI purposes */}
              {['1', '2'].map(xId => {
                const isSelected = selectedTargets.some(t => t.platform === 'X' && t.externalAccountId === `acc_x_${xId}`);
                return (
                  <button 
                    key={xId}
                    className={`btn-secondary ${isSelected ? 'active' : ''}`}
                    style={isSelected ? { background: '#1da1f2', color: '#fff', borderColor: '#1da1f2' } : {}}
                    onClick={() => handleToggleTarget('X', `acc_x_${xId}`)}
                  >
                    X Account {xId}
                  </button>
                );
              })}
            </div>
            {selectedTargets.length === 0 && <small className="mt-2 block text-sm" style={{color: '#fca5a5'}}>Select at least one destination</small>}
          </div>

          <div className="mb-4">
            <textarea
              className="glass-input"
              rows={6}
              placeholder="What do you want to share?"
              value={textContent}
              onChange={(e) => setTextContent(e.target.value)}
              style={{ resize: 'vertical' }}
            />
          </div>

          <div className="flex gap-4 items-center">
            <input 
              type="datetime-local" 
              className="glass-input"
              style={{ flex: 1 }}
              value={scheduledAtUtc}
              onChange={(e) => setScheduledAtUtc(e.target.value)}
            />
            <button 
              className="btn-primary flex items-center gap-2"
              onClick={handleSchedule}
              disabled={isSubmitting || selectedTargets.length === 0 || !textContent.trim()}
            >
              <Send size={18} />
              Schedule Post
            </button>
          </div>
        </div>

        <div>
          <div className="card" style={{ padding: '0', overflow: 'hidden' }}>
            <div style={{ padding: '1rem', borderBottom: '1px solid var(--border-subtle)', background: 'rgba(255,255,255,0.02)' }}>
              <h4 style={{ margin: 0 }}>Preview</h4>
            </div>
            <div style={{ padding: '1.5rem', minHeight: '200px' }}>
              <div style={{ display: 'flex', gap: '0.75rem', marginBottom: '1rem' }}>
                <div style={{ width: 40, height: 40, borderRadius: '50%', background: 'var(--border-subtle)' }} />
                <div>
                  <div style={{ fontWeight: 600, fontSize: '0.9rem' }}>PostPebble Author</div>
                  <div style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>Just now</div>
                </div>
              </div>
              <p style={{ whiteSpace: 'pre-wrap', margin: 0, fontSize: '0.95rem' }}>
                {textContent || <span style={{ color: 'var(--text-secondary)', fontStyle: 'italic' }}>Your post preview will appear here...</span>}
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="card mt-6">
        <h3>Queue</h3>
        {scheduledPosts.length === 0 ? (
          <p className="text-sm">No scheduled posts.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Content</th>
                <th>Targets</th>
                <th>When</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {scheduledPosts.map(post => (
                <tr key={post.id}>
                  <td style={{ maxWidth: 300, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{post.textContent}</td>
                  <td>{post.targets.map(t => t.platform).join(', ')}</td>
                  <td>{new Date(post.scheduledAtUtc).toLocaleString()}</td>
                  <td><span className={`statusBadge ${post.status.toLowerCase()}`}>{post.status}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
