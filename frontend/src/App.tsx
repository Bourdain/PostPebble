import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { PostPebbleProvider } from './contexts/PostPebbleContext';
import { Layout } from './components/Layout';
import { Auth } from './pages/Auth';
import { Dashboard } from './pages/Dashboard';
import { Library } from './pages/Library';
import { Scheduler } from './pages/Scheduler';
import { Integrations } from './pages/Integrations';
import { Billing } from './pages/Billing';
import { Settings } from './pages/Settings';
import { Notifications } from './pages/Notifications';
import { AcceptInvite } from './pages/AcceptInvite';

export default function App() {
  return (
    <PostPebbleProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/auth" element={<Auth />} />
          <Route path="/accept-invite" element={<AcceptInvite />} />
          <Route element={<Layout />}>
            <Route path="/dashboard" element={<Dashboard />} />
            <Route path="/library" element={<Library />} />
            <Route path="/scheduler" element={<Scheduler />} />
            <Route path="/integrations" element={<Integrations />} />
            <Route path="/billing" element={<Billing />} />
            <Route path="/settings" element={<Settings />} />
            <Route path="/notifications" element={<Notifications />} />
            <Route path="*" element={<Navigate to="/dashboard" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </PostPebbleProvider>
  );
}
