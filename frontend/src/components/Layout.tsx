import React from 'react';
import { Outlet, Navigate } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { motion } from 'framer-motion';
import { NotificationBell } from './NotificationBell';

export function Layout() {
  const { auth } = usePostPebble();

  if (!auth) {
    return <Navigate to="/auth" replace />;
  }

  return (
    <div className="layout-shell">
      <Sidebar />
      <main className="main-content">
        <div className="topbar">
          <div />
          <NotificationBell />
        </div>
        <motion.div
          initial={{ opacity: 0, y: 10 }}
          animate={{ opacity: 1, y: 0 }}
          exit={{ opacity: 0, y: -10 }}
          transition={{ duration: 0.3 }}
        >
          <Outlet />
        </motion.div>
      </main>
    </div>
  );
}
