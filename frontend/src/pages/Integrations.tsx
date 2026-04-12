import React, { useEffect, useState } from 'react';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { Link2, AlertCircle, Save } from 'lucide-react';

export function Integrations() {
  const { 
    linkedInConnections, loadLinkedInConnections, connectLinkedIn, saveLinkedInMemberUrn
  } = usePostPebble();

  const [urnInputs, setUrnInputs] = useState<Record<string, string>>({});

  useEffect(() => {
    loadLinkedInConnections();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleUrnChange = (id: string, val: string) => {
    setUrnInputs(prev => ({ ...prev, [id]: val }));
  };

  const hasLinkedIn = linkedInConnections.length > 0;

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <div>
          <h2>Integrations</h2>
          <p className="text-sm">Connect your social accounts to schedule posts.</p>
        </div>
      </div>

      <div className="grid" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(350px, 1fr))', gap: '1.5rem', maxWidth: '100%' }}>
        
        {/* LinkedIn Card */}
        <div className="card" style={{ display: 'flex', flexDirection: 'column' }}>
          <div className="flex justify-between items-center mb-6">
            <div className="flex items-center gap-3">
              <div style={{ background: '#0a66c2', padding: '10px', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', width: 48, height: 48 }}>
                <Link2 color="#fff" size={24} />
              </div>
              <div>
                <h3 style={{ margin: 0, fontSize: '1.2rem' }}>LinkedIn</h3>
                <span className={`statusBadge ${hasLinkedIn ? 'success' : 'queued'}`} style={{ marginTop: '0.25rem' }}>
                  {hasLinkedIn ? 'Connected' : 'Not Connected'}
                </span>
              </div>
            </div>
            {!hasLinkedIn && (
              <button className="btn-primary" onClick={connectLinkedIn}>Connect</button>
            )}
          </div>

          {hasLinkedIn && (
            <div className="flex-col gap-4">
              {linkedInConnections.map(conn => (
                <div key={conn.id} style={{ background: 'rgba(255,255,255,0.02)', padding: '1rem', borderRadius: '8px', border: '1px solid var(--border-subtle)' }}>
                  <div className="flex justify-between text-sm mb-2" style={{ color: 'var(--text-secondary)' }}>
                    <span>Expires: {conn.accessTokenExpiresAtUtc ? new Date(conn.accessTokenExpiresAtUtc).toLocaleDateString() : 'Unknown'}</span>
                    <button onClick={connectLinkedIn} style={{ background: 'none', border: 'none', color: 'var(--highlight-blue)', cursor: 'pointer' }}>Reconnect</button>
                  </div>
                  
                  <div className="mt-4">
                    <label className="text-sm block mb-1">Member URN</label>
                    <div className="flex gap-2">
                      <input 
                        className="glass-input" 
                        value={urnInputs[conn.id] ?? conn.memberUrn ?? ''}
                        onChange={(e) => handleUrnChange(conn.id, e.target.value)}
                        placeholder="urn:li:person:..."
                      />
                      <button 
                        className="btn-secondary flex items-center justify-center p-2"
                        onClick={() => saveLinkedInMemberUrn(urnInputs[conn.id] ?? '')}
                        disabled={!urnInputs[conn.id] && !conn.memberUrn}
                      >
                        <Save size={18} />
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* X / Twitter Card Placeholder */}
        <div className="card" style={{ display: 'flex', flexDirection: 'column', opacity: 0.7 }}>
          <div className="flex justify-between items-center mb-6">
            <div className="flex items-center gap-3">
              <div style={{ background: '#000', border: '1px solid #333', borderRadius: '8px', display: 'flex', alignItems: 'center', justifyContent: 'center', width: 48, height: 48 }}>
                <span style={{ color: '#fff', fontSize: '1.5rem', fontWeight: 'bold' }}>𝕏</span>
              </div>
              <div>
                <h3 style={{ margin: 0, fontSize: '1.2rem' }}>X (Twitter)</h3>
                <span className="statusBadge queued" style={{ marginTop: '0.25rem' }}>Coming Soon</span>
              </div>
            </div>
          </div>
          <p className="text-sm" style={{ color: 'var(--text-secondary)' }}>
            Native integration with X API v2 is currently in development. You can manual target using ID strings via the CLI for now.
          </p>
        </div>

      </div>
    </div>
  );
}
