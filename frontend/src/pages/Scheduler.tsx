import React, { useState, useEffect } from 'react';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { Send, Image as ImageIcon, X, Pencil, RotateCcw } from 'lucide-react';


export function Scheduler() {
  const { 
    scheduledPosts, loadScheduledPosts, createScheduledPost, updateScheduledPost, cancelScheduledPost,
    linkedInConnections, loadLinkedInConnections,
    mediaAssets, loadMedia, apiBaseUrl
  } = usePostPebble();

  const [textContent, setTextContent] = useState('');
  const [scheduledAtUtc, setScheduledAtUtc] = useState(new Date().toISOString().slice(0,16));
  const [selectedTargets, setSelectedTargets] = useState<{platform: string; externalAccountId: string}[]>([]);
  const [selectedMedia, setSelectedMedia] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  
  const [viewMode, setViewMode] = useState<'list'|'calendar'>('list');
  const [selectedDate, setSelectedDate] = useState<string | null>(null);

  // Edit state
  const [editingPostId, setEditingPostId] = useState<string | null>(null);
  const [editText, setEditText] = useState('');
  const [editDate, setEditDate] = useState('');

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

  const handleToggleMedia = (id: string) => {
    setSelectedMedia(prev => prev.includes(id) ? prev.filter(m => m !== id) : [...prev, id]);
  };

  const handleSchedule = async () => {
    if (selectedTargets.length === 0) return;
    setIsSubmitting(true);
    const dateObj = new Date(scheduledAtUtc);
    await createScheduledPost(textContent, dateObj.toISOString(), selectedTargets, selectedMedia);
    setIsSubmitting(false);
    setTextContent('');
    setSelectedMedia([]);
  };

  const handleCancel = async (postId: string) => {
    if (!window.confirm('Cancel this post? Credits will be returned.')) return;
    await cancelScheduledPost(postId);
  };

  const handleStartEdit = (post: typeof scheduledPosts[0]) => {
    setEditingPostId(post.id);
    setEditText(post.textContent);
    setEditDate(new Date(post.scheduledAtUtc).toISOString().slice(0, 16));
  };

  const handleSaveEdit = async () => {
    if (!editingPostId) return;
    await updateScheduledPost(editingPostId, {
      textContent: editText,
      scheduledAtUtc: new Date(editDate).toISOString(),
    });
    setEditingPostId(null);
  };

  const handleCancelEdit = () => {
    setEditingPostId(null);
  };

  const today = new Date();
  const currentYear = today.getFullYear();
  const currentMonth = today.getMonth();
  const daysInMonth = new Date(currentYear, currentMonth + 1, 0).getDate();
  const firstDayOfMonth = new Date(currentYear, currentMonth, 1).getDay(); 

  const filteredQueue = selectedDate 
    ? scheduledPosts.filter(p => new Date(p.scheduledAtUtc).toDateString() === new Date(selectedDate).toDateString())
    : scheduledPosts;

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

          {mediaAssets.length > 0 && (
            <div className="mb-4">
              <h4 style={{ marginBottom: '0.5rem' }}>Attach Media</h4>
              <div className="flex gap-2" style={{ overflowX: 'auto', paddingBottom: '0.5rem' }}>
                {mediaAssets.map(asset => {
                  const isSelected = selectedMedia.includes(asset.id);
                  return (
                    <div 
                      key={asset.id} 
                      onClick={() => handleToggleMedia(asset.id)}
                      style={{
                         minWidth: 80, maxWidth: 80, height: 80, borderRadius: 8, overflow: 'hidden', cursor: 'pointer',
                         border: isSelected ? '2px solid var(--highlight-blue)' : '2px solid transparent',
                         background: 'var(--bg-primary)'
                      }}
                    >
                      {asset.contentType.startsWith('image/') ? (
                        <img src={`${apiBaseUrl}${asset.publicUrl}`} style={{width: '100%', height: '100%', objectFit: 'cover'}} />
                      ) : (
                        <div className="flex items-center justify-center h-full text-xs text-center p-1" style={{color:'var(--text-secondary)'}}>
                          {asset.originalFileName}
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            </div>
          )}

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
              {selectedMedia.length > 0 && (
                <div className="mt-4 flex gap-2 flex-wrap">
                  {selectedMedia.map(mid => {
                    const ast = mediaAssets.find(m => m.id === mid);
                    if (!ast) return null;
                    if (!ast.contentType.startsWith('image/')) return (
                       <div key={mid} style={{width:80,height:80,background:'rgba(255,255,255,0.1)',borderRadius:8,display:'flex',alignItems:'center',justifyContent:'center',fontSize:'10px',textAlign:'center',overflow:'hidden'}}>{ast.originalFileName}</div>
                    );
                    return <img key={mid} src={`${apiBaseUrl}${ast.publicUrl}`} style={{width: 80, height: 80, objectFit: 'cover', borderRadius: '8px'}} />;
                  })}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      <div className="card mt-6">
        <div className="flex justify-between items-center mb-4">
          <h3 style={{ margin: 0 }}>Queue</h3>
          <div className="flex gap-2">
            <button className={`btn-secondary ${viewMode === 'list' ? 'active' : ''}`} onClick={() => setViewMode('list')} style={viewMode === 'list' ? {borderColor: 'var(--highlight-blue)', color:'var(--highlight-blue)'} : {}}>List View</button>
            <button className={`btn-secondary ${viewMode === 'calendar' ? 'active' : ''}`} onClick={() => setViewMode('calendar')} style={viewMode === 'calendar' ? {borderColor: 'var(--highlight-blue)', color:'var(--highlight-blue)'} : {}}>Calendar View</button>
          </div>
        </div>

        {viewMode === 'calendar' && (
          <div className="mb-6">
             <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: '4px', textAlign: 'center', marginBottom: '8px', fontWeight: 'bold' }}>
               <div>Sun</div><div>Mon</div><div>Tue</div><div>Wed</div><div>Thu</div><div>Fri</div><div>Sat</div>
             </div>
             <div style={{ display: 'grid', gridTemplateColumns: 'repeat(7, 1fr)', gap: '4px' }}>
                {Array.from({length: firstDayOfMonth}).map((_, i) => <div key={`blank-${i}`} />)}
                {Array.from({length: daysInMonth}).map((_, i) => {
                   const day = i + 1;
                   const dateString = new Date(currentYear, currentMonth, day).toDateString();
                   const postsOnDay = scheduledPosts.filter(p => new Date(p.scheduledAtUtc).toDateString() === dateString);
                   const isSelected = selectedDate === dateString;
                   return (
                     <div 
                       key={day}
                       onClick={() => setSelectedDate(isSelected ? null : dateString)}
                       style={{ 
                         minHeight: '80px', background: isSelected ? 'rgba(51, 183, 255, 0.2)' : 'rgba(255,255,255,0.05)', 
                         borderRadius: '8px', padding: '8px', cursor: 'pointer',
                         border: isSelected ? '1px solid var(--highlight-blue)' : '1px solid transparent'
                       }}
                     >
                        <div style={{ opacity: 0.8, fontSize: '0.85rem' }}>{day}</div>
                        <div className="mt-1 flex flex-wrap gap-1">
                          {postsOnDay.map((p, idx) => (
                             <div key={idx} style={{width: 6, height: 6, borderRadius: '50%', background: p.status === 'Published' ? '#4ade80' : 'var(--highlight-blue)'}} title={p.textContent} />
                          ))}
                        </div>
                     </div>
                   );
                })}
             </div>
             {selectedDate && <div className="mt-4 font-bold text-sm" style={{color: 'var(--highlight-blue)'}}>Viewing posts for: {selectedDate}</div>}
          </div>
        )}

        {filteredQueue.length === 0 ? (
          <p className="text-sm">No scheduled posts.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th>Content</th>
                <th>Targets</th>
                <th>When</th>
                <th>Status</th>
                <th>Retry</th>
                <th style={{ width: '100px' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredQueue.map(post => (
                <tr key={post.id}>
                  <td style={{ maxWidth: 250 }}>
                    {editingPostId === post.id ? (
                      <textarea
                        className="glass-input"
                        rows={3}
                        value={editText}
                        onChange={(e) => setEditText(e.target.value)}
                        style={{ fontSize: '0.85rem', resize: 'vertical' }}
                      />
                    ) : (
                      <div style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{post.textContent}</div>
                    )}
                  </td>
                  <td>{post.targets.map(t => t.platform).join(', ')}</td>
                  <td>
                    {editingPostId === post.id ? (
                      <input
                        type="datetime-local"
                        className="glass-input"
                        value={editDate}
                        onChange={(e) => setEditDate(e.target.value)}
                        style={{ fontSize: '0.85rem' }}
                      />
                    ) : (
                      new Date(post.scheduledAtUtc).toLocaleString()
                    )}
                  </td>
                  <td>
                    <span className={`statusBadge ${post.status.toLowerCase()}`}>{post.status}</span>
                    {post.failureReason && post.status !== 'Published' && (
                      <div style={{ marginTop: '4px', color: '#fca5a5', fontSize: '0.7rem', maxWidth: '180px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }} title={post.failureReason}>
                        {post.failureReason}
                      </div>
                    )}
                  </td>
                  <td>
                    {post.retryCount > 0 ? (
                      <div className="flex items-center gap-1" style={{ fontSize: '0.75rem' }}>
                        <RotateCcw size={12} />
                        <span>{post.retryCount}/{post.maxRetries}</span>
                        {post.nextRetryAtUtc && post.status === 'Queued' && (
                          <div style={{ color: 'var(--text-secondary)', fontSize: '0.65rem' }}>
                            Next: {new Date(post.nextRetryAtUtc).toLocaleTimeString()}
                          </div>
                        )}
                      </div>
                    ) : (
                      <span style={{ fontSize: '0.75rem', color: 'var(--text-secondary)' }}>—</span>
                    )}
                  </td>
                  <td>
                    {post.status === 'Queued' && editingPostId !== post.id && (
                      <div className="flex gap-1">
                        <button
                          onClick={() => handleStartEdit(post)}
                          style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--highlight-blue)', padding: '4px' }}
                          title="Edit post"
                        >
                          <Pencil size={16} />
                        </button>
                        <button
                          onClick={() => handleCancel(post.id)}
                          style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#ef4444', padding: '4px' }}
                          title="Cancel post"
                        >
                          <X size={16} />
                        </button>
                      </div>
                    )}
                    {editingPostId === post.id && (
                      <div className="flex gap-1">
                        <button className="btn-primary" onClick={handleSaveEdit} style={{ padding: '4px 10px', fontSize: '0.75rem' }}>Save</button>
                        <button className="btn-secondary" onClick={handleCancelEdit} style={{ padding: '4px 10px', fontSize: '0.75rem' }}>Cancel</button>
                      </div>
                    )}
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
