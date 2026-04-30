import React, { useState, FormEvent } from 'react';
import { Link, Navigate } from 'react-router-dom';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { motion, AnimatePresence } from 'framer-motion';

export function Auth() {
  const { auth, login, register, status } = usePostPebble();
  const [isLogin, setIsLogin] = useState(true);
  
  const [email, setEmail] = useState('demo@postpebble.local');
  const [password, setPassword] = useState('Passw0rd!123');
  const [tenantName, setTenantName] = useState('Demo Tenant');

  if (auth) {
    return <Navigate to="/dashboard" replace />;
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (isLogin) {
      await login(email, password);
    } else {
      await register(email, password, tenantName);
    }
  };

  return (
    <div className="auth-container">
      <div className="auth-card">
        <div className="flex-col items-center gap-2 mb-8">
          <div style={{ background: 'var(--highlight-blue)', width: '48px', height: '48px', borderRadius: '12px' }} />
          <h2>Welcome to PostPebble</h2>
          <p className="text-sm">Manage your social presence</p>
        </div>

        <form onSubmit={handleSubmit} className="flex-col gap-4">
          <AnimatePresence mode="wait">
            {!isLogin && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                transition={{ duration: 0.2 }}
              >
                <input
                  type="text"
                  placeholder="Tenant Name"
                  className="glass-input"
                  value={tenantName}
                  onChange={(e) => setTenantName(e.target.value)}
                  required={!isLogin}
                />
              </motion.div>
            )}
          </AnimatePresence>

          <input
            type="email"
            placeholder="Email Address"
            className="glass-input"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
          <input
            type="password"
            placeholder="Password"
            className="glass-input"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />

          <button type="submit" className="btn-primary mt-4 w-full">
            {isLogin ? 'Sign In' : 'Create Account'}
          </button>
        </form>

        <div className="mt-6 text-center text-sm" style={{ color: status.includes('failed') ? '#fca5a5' : 'inherit' }}>
          {status !== 'Ready.' && status}
        </div>

        <div className="mt-6 text-center">
          <button 
            type="button" 
            className="btn-secondary" 
            style={{ background: 'transparent', border: 'none', color: 'var(--highlight-blue)' }}
            onClick={() => setIsLogin(!isLogin)}
          >
            {isLogin ? "Don't have an account? Sign up" : "Already have an account? Sign in"}
          </button>
        </div>

        <div className="mt-4 text-center">
          <Link to="/accept-invite" style={{ color: 'var(--highlight-blue)', textDecoration: 'none', fontSize: '0.9rem' }}>
            Accept an invite code
          </Link>
        </div>
      </div>
    </div>
  );
}
