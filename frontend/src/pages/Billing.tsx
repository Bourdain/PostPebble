import React, { useEffect } from 'react';
import { usePostPebble } from '../contexts/PostPebbleContext';
import { Coins, Zap, Clock, ShieldCheck } from 'lucide-react';

export function Billing() {
  const { 
    walletBalance, loadWallet, buyCredits,
    buyCreditsAmount, setBuyCreditsAmount,
    transactions, webhookEvents
  } = usePostPebble();

  useEffect(() => {
    loadWallet();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div>
      <div className="flex justify-between items-center mb-8">
        <div>
          <h2>Billing & Credits</h2>
          <p className="text-sm">Manage your account balance and purchase history.</p>
        </div>
      </div>

      <div className="grid" style={{ gridTemplateColumns: 'minmax(300px, 1fr) minmax(300px, 1.5fr)', gap: '2rem', maxWidth: '100%' }}>
        
        <div className="flex-col gap-4">
          <div className="card" style={{ background: 'linear-gradient(135deg, rgba(51, 183, 255, 0.1), rgba(0, 136, 204, 0.05))', borderColor: 'var(--border-strong)' }}>
            <div className="flex justify-between items-center mb-2">
              <span className="text-sm" style={{ color: 'var(--highlight-blue)', fontWeight: 600 }}>Active Balance</span>
              <Coins color="var(--highlight-blue)" />
            </div>
            <div style={{ fontSize: '3.5rem', fontWeight: 700, fontFamily: 'Outfit', lineHeight: 1 }}>
              {walletBalance !== null ? walletBalance : '-'}
            </div>
            <p className="text-sm mt-2">Credits</p>
          </div>

          <div className="card">
            <h3>Add Credits</h3>
            <p className="text-sm mb-4">Purchase credits securely via Stripe to schedule more posts.</p>
            
            <div className="flex gap-2">
              {[5, 20, 50, 100].map(amt => (
                <button 
                  key={amt}
                  className={`btn-secondary flex-1 ${buyCreditsAmount === amt ? 'active' : ''}`}
                  style={buyCreditsAmount === amt ? { borderColor: 'var(--highlight-blue)', background: 'rgba(51, 183, 255, 0.1)' } : {}}
                  onClick={() => setBuyCreditsAmount(amt)}
                >
                  {amt}
                </button>
              ))}
            </div>
            
            <div className="flex items-center gap-4 mt-6">
              <div className="flex items-center gap-2" style={{ color: 'var(--text-secondary)' }}>
                <ShieldCheck size={18} />
                <span className="text-sm">Secure Checkout</span>
              </div>
              <button className="btn-primary flex-1" onClick={buyCredits}>
                Checkout
              </button>
            </div>
          </div>
        </div>

        <div className="flex-col gap-4">
          <div className="card mb-0">
            <h3>Transaction History</h3>
            {transactions.length === 0 ? (
              <p className="text-sm">No transactions yet.</p>
            ) : (
              <div style={{ maxHeight: '300px', overflowY: 'auto', paddingRight: '0.5rem' }}>
                <table className="table">
                  <thead>
                    <tr>
                      <th>Type</th>
                      <th>Amount</th>
                      <th>Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {transactions.map(tx => (
                      <tr key={tx.id}>
                        <td>
                          <div className="flex items-center gap-2">
                            {tx.type === 'Purchase' ? <Zap size={14} color="#10b981" /> : <Clock size={14} color="#f59e0b" />}
                            {tx.type}
                          </div>
                        </td>
                        <td style={{ color: tx.type === 'Purchase' ? '#10b981' : 'inherit', fontWeight: tx.type === 'Purchase' ? 600 : 'normal' }}>
                          {tx.type === 'Purchase' ? '+' : ''}{tx.amountCredits}
                        </td>
                        <td>{new Date(tx.createdAtUtc).toLocaleDateString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="card">
            <h3>System Events</h3>
            {webhookEvents.length === 0 ? (
              <p className="text-sm">No webhook events logged.</p>
            ) : (
              <div style={{ maxHeight: '200px', overflowY: 'auto' }}>
                <table className="table">
                  <thead>
                    <tr>
                      <th>Event</th>
                      <th>Status</th>
                      <th>Received</th>
                    </tr>
                  </thead>
                  <tbody>
                    {webhookEvents.map(evt => (
                      <tr key={evt.id}>
                        <td style={{ fontSize: '0.8rem' }}>{evt.eventType}</td>
                        <td><span className={`statusBadge ${evt.status.toLowerCase()}`}>{evt.status}</span></td>
                        <td>{new Date(evt.receivedAtUtc).toLocaleDateString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>

      </div>
    </div>
  );
}
