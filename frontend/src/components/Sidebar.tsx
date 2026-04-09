import React from 'react';
import { NavLink } from 'react-router-dom';
import { LayoutDashboard, Calendar, Image as ImageIcon, CreditCard, Link as LinkIcon, LogOut } from 'lucide-react';
import { usePostPebble } from '../contexts/PostPebbleContext';

export function Sidebar() {
  const { activeTenant, logout } = usePostPebble();

  const handleLogout = (e: React.MouseEvent) => {
    e.preventDefault();
    logout();
  };

  return (
    <aside className="sidebar">
      <div className="sidebar-logo">
        <div style={{ background: 'var(--highlight-blue)', width: '28px', height: '28px', borderRadius: '8px' }} />
        PostPebble
      </div>

      <nav className="sidebar-nav">
        <NavLink to="/dashboard" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
          <LayoutDashboard size={20} />
          Dashboard
        </NavLink>
        <NavLink to="/scheduler" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
          <Calendar size={20} />
          Scheduler
        </NavLink>
        <NavLink to="/library" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
          <ImageIcon size={20} />
          Library
        </NavLink>
        <NavLink to="/integrations" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
          <LinkIcon size={20} />
          Integrations
        </NavLink>
        <NavLink to="/billing" className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}>
          <CreditCard size={20} />
          Billing
        </NavLink>
      </nav>

      <div className="sidebar-footer">
        <div style={{ marginBottom: '1rem', padding: '0 1rem', fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
          {activeTenant ? activeTenant.tenantName : 'No Tenant'}
        </div>
        <button onClick={handleLogout} className="nav-link" style={{ background: 'none', border: 'none', width: '100%', textAlign: 'left', cursor: 'pointer' }}>
          <LogOut size={20} />
          Logout
        </button>
      </div>
    </aside>
  );
}
