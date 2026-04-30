import React, { FormEvent, useEffect, useState } from 'react';
import { Navigate, useSearchParams } from 'react-router-dom';
import { usePostPebble } from '../contexts/PostPebbleContext';

export function AcceptInvite() {
  const { auth, lookupInvite, acceptInvite, status } = usePostPebble();
  const [searchParams] = useSearchParams();
  const [code, setCode] = useState(searchParams.get('code') ?? '');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [tenantName, setTenantName] = useState('');
  const [role, setRole] = useState('');
  const [inviteStatus, setInviteStatus] = useState('');
  const [loadingInvite, setLoadingInvite] = useState(false);

  useEffect(() => {
    if (!code.trim()) {
      return;
    }

    void handleLookup(code);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (auth) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleLookup = async (value: string) => {
    setLoadingInvite(true);
    const invite = await lookupInvite(value);
    if (!invite) {
      setInviteStatus('Invite not found.');
      setTenantName('');
      setRole('');
      setEmail('');
      setLoadingInvite(false);
      return;
    }

    setTenantName(invite.tenantName);
    setRole(invite.role);
    setEmail(invite.email);
    setInviteStatus(invite.status === 'Pending' ? 'Invite ready.' : `Invite is ${invite.status.toLowerCase()}.`);
    setLoadingInvite(false);
  };

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    const accepted = await acceptInvite(code, email, password);
    if (!accepted) {
      return;
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        <div className="flex-col items-center gap-2 mb-8">
          <h2>Accept Invite</h2>
          <p className="text-sm">Paste your invite code to join a tenant.</p>
        </div>

        <form onSubmit={handleSubmit} className="flex-col gap-4">
          <div className="flex gap-2">
            <input
              type="text"
              placeholder="Invite Code"
              className="glass-input"
              value={code}
              onChange={(event) => setCode(event.target.value)}
              required
            />
            <button type="button" className="btn-secondary" onClick={() => void handleLookup(code)} disabled={loadingInvite}>
              Check
            </button>
          </div>

          <input
            type="email"
            placeholder="Email Address"
            className="glass-input"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />

          <input
            type="password"
            placeholder="Password"
            className="glass-input"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />

          {(tenantName || role) && (
            <div className="invite-preview">
              <div><strong>Tenant:</strong> {tenantName}</div>
              <div><strong>Role:</strong> {role}</div>
            </div>
          )}

          <button type="submit" className="btn-primary mt-4 w-full" disabled={!code || !email || !password}>
            Join Tenant
          </button>
        </form>

        <div className="mt-6 text-center text-sm" style={{ color: status.includes('failed') ? '#fca5a5' : 'inherit' }}>
          {inviteStatus && inviteStatus !== 'Invite ready.' ? inviteStatus : status !== 'Ready.' ? status : inviteStatus}
        </div>
      </div>
    </div>
  );
}
