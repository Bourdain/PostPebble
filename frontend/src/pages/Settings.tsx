import React, { useState, useEffect } from 'react';
import { usePostPebble } from '../contexts/PostPebbleContext';

type Member = {
  userId: string;
  email: string;
  role: string;
  joinedAtUtc: string;
};

export function Settings() {
  const { activeTenant, apiBaseUrl, auth } = usePostPebble();
  const [activeTab, setActiveTab] = useState<'general' | 'roles'>('general');
  const [members, setMembers] = useState<Member[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const pageSize = 10;
  
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('Drafter');
  const [inviteStatus, setInviteStatus] = useState('');

  const isAdminOrOwner = activeTenant?.role === 'Admin' || activeTenant?.role === 'Owner';
  const isOwner = activeTenant?.role === 'Owner';

  useEffect(() => {
    if (activeTab === 'roles' && isAdminOrOwner) {
      loadMembers();
    }
  }, [activeTab, page, isAdminOrOwner]);

  const loadMembers = async () => {
    if (!auth || !activeTenant) return;
    try {
      const res = await fetch(`${apiBaseUrl}/api/v1/tenants/${activeTenant.tenantId}/members?page=${page}&pageSize=${pageSize}`, {
        headers: { Authorization: `Bearer ${auth.accessToken}` }
      });
      if (res.ok) {
        const data = await res.json();
        setMembers(data.items);
        setTotalCount(data.totalCount);
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!auth || !activeTenant || !inviteEmail) return;
    setInviteStatus('Inviting...');
    try {
      const res = await fetch(`${apiBaseUrl}/api/v1/tenants/${activeTenant.tenantId}/members`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${auth.accessToken}`
        },
        body: JSON.stringify({ email: inviteEmail, role: inviteRole })
      });
      if (res.ok) {
        const data = await res.json();
        setInviteStatus(`Invite created. Placeholder code: ${data.inviteCode}`);
        setInviteEmail('');
        loadMembers();
      } else {
        const data = await res.text();
        setInviteStatus(`Failed to invite: ${data}`);
      }
    } catch (err) {
      setInviteStatus('Error inviting user.');
      console.error(err);
    }
  };

  const handleRoleChange = async (userId: string, newRole: string) => {
    if (!auth || !activeTenant) return;
    try {
      const res = await fetch(`${apiBaseUrl}/api/v1/tenants/${activeTenant.tenantId}/members/${userId}/role`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${auth.accessToken}`
        },
        body: JSON.stringify({ newRole })
      });
      if (res.ok) {
        loadMembers();
      } else {
        const errText = await res.text();
        alert(`Failed to change role: ${errText}`);
      }
    } catch (err) {
      console.error(err);
    }
  };

  const handleTransferOwnership = async (userId: string) => {
    if (!auth || !activeTenant) return;
    if (!window.confirm("Are you sure you want to transfer ownership to this user? This is a permanent action and you will be downgraded to an Admin.")) {
      return;
    }
    try {
      const res = await fetch(`${apiBaseUrl}/api/v1/tenants/${activeTenant.tenantId}/transfer-ownership`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${auth.accessToken}`
        },
        body: JSON.stringify({ newOwnerUserId: userId })
      });
      if (res.ok) {
        alert("Ownership transferred successfully. Please reload the application.");
        window.location.reload();
      } else {
        const errText = await res.text();
        alert(`Failed to transfer ownership: ${errText}`);
      }
    } catch (err) {
      console.error(err);
    }
  };

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <div>
          <h2>Settings</h2>
          <p className="text-sm">Manage your tenant settings and users.</p>
        </div>
      </div>

      <div className="flex gap-2 mb-6">
        <button 
          onClick={() => setActiveTab('general')}
          className={`btn-secondary ${activeTab === 'general' ? 'active' : ''}`}
          style={activeTab === 'general' ? { borderColor: 'var(--highlight-blue)', color: 'var(--highlight-blue)' } : {}}
        >
          General
        </button>
        {isAdminOrOwner && (
          <button 
            onClick={() => setActiveTab('roles')}
            className={`btn-secondary ${activeTab === 'roles' ? 'active' : ''}`}
            style={activeTab === 'roles' ? { borderColor: 'var(--highlight-blue)', color: 'var(--highlight-blue)' } : {}}
          >
            Roles & Users
          </button>
        )}
      </div>

      {activeTab === 'general' && (
        <div className="card">
          <h4>Tenant Information</h4>
          <p className="text-sm mt-2" style={{color: 'var(--text-secondary)'}}>
            <strong>Tenant ID:</strong> {activeTenant?.tenantId}
          </p>
          <p className="text-sm mt-1" style={{color: 'var(--text-secondary)'}}>
            <strong>Tenant Name:</strong> {activeTenant?.tenantName}
          </p>
          <p className="text-sm mt-1" style={{color: 'var(--text-secondary)'}}>
            <strong>Your Role:</strong> {activeTenant?.role}
          </p>
        </div>
      )}

      {activeTab === 'roles' && isAdminOrOwner && (
        <div className="flex flex-col gap-6">
          <div className="card">
            <h4>Invite User</h4>
            <form onSubmit={handleInvite} className="flex gap-4 mt-4 items-end">
              <div style={{ flex: 1 }}>
                <label className="block text-sm mb-1">Email Address</label>
                <input 
                  type="email" 
                  required
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  className="glass-input w-full" 
                  placeholder="user@example.com" 
                />
              </div>
              <div style={{ width: '150px' }}>
                <label className="block text-sm mb-1">Role</label>
                <select 
                  value={inviteRole}
                  onChange={(e) => setInviteRole(e.target.value)}
                  className="glass-input w-full"
                >
                  <option value="Drafter">Drafter</option>
                  <option value="Reviewer">Reviewer</option>
                  {isOwner && <option value="Admin">Admin</option>}
                </select>
              </div>
              <button type="submit" className="btn-primary">Invite</button>
            </form>
            {inviteStatus && <div className="mt-2 text-sm" style={{color: 'var(--highlight-blue)'}}>{inviteStatus}</div>}
          </div>

          <div className="card">
            <h4>Tenant Members</h4>
            <table className="table mt-4 w-full text-left border-collapse">
              <thead>
                <tr>
                  <th className="pb-2 text-sm" style={{color: 'var(--text-secondary)'}}>Email</th>
                  <th className="pb-2 text-sm" style={{color: 'var(--text-secondary)'}}>Joined</th>
                  <th className="pb-2 text-sm" style={{color: 'var(--text-secondary)'}}>Role</th>
                  <th className="pb-2 text-sm" style={{color: 'var(--text-secondary)'}}>Actions</th>
                </tr>
              </thead>
              <tbody>
                {members.map(member => (
                  <tr key={member.userId} className="border-t border-gray-800" style={{borderColor: 'var(--border-subtle)'}}>
                    <td className="py-3 text-sm">{member.email}</td>
                    <td className="py-3 text-sm">{new Date(member.joinedAtUtc).toLocaleDateString()}</td>
                    <td className="py-3">
                      {member.role === 'Owner' || (member.role === 'Admin' && !isOwner) ? (
                        <span className="text-sm font-semibold">{member.role}</span>
                      ) : (
                        <select 
                          className="glass-input glass-input-sm"
                          value={member.role}
                          onChange={(e) => handleRoleChange(member.userId, e.target.value)}
                        >
                          <option value="Drafter">Drafter</option>
                          <option value="Reviewer">Reviewer</option>
                          {isOwner && <option value="Admin">Admin</option>}
                        </select>
                      )}
                    </td>
                    <td className="py-3">
                      {isOwner && member.role !== 'Owner' && (
                        <button 
                          className="text-sm border rounded px-2 py-1"
                          style={{ borderColor: 'var(--highlight-blue)', color: 'var(--highlight-blue)', background: 'transparent' }}
                          onClick={() => handleTransferOwnership(member.userId)}
                        >
                          Make Owner
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="flex justify-between mt-4">
              <button 
                disabled={page <= 1} 
                onClick={() => setPage(p => p - 1)}
                className="btn-secondary text-sm px-3 py-1"
              >
                Previous
              </button>
              <span className="text-sm mt-1">Page {page} of {totalPages || 1}</span>
              <button 
                disabled={page >= totalPages} 
                onClick={() => setPage(p => p + 1)}
                className="btn-secondary text-sm px-3 py-1"
              >
                Next
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
