import { FormEvent, useEffect, useMemo, useState } from "react";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";
const authStorageKey = "postpebble_auth";

type TenantSummary = {
  tenantId: string;
  tenantName: string;
  role: string;
};

type AuthResponse = {
  accessToken: string;
  expiresAtUtc: string;
  tenants: TenantSummary[];
};

type CreditTransaction = {
  id: string;
  type: string;
  amountCredits: number;
  referenceId: string;
  description: string;
  createdAtUtc: string;
};

type ScheduledPost = {
  id: string;
  textContent: string;
  scheduledAtUtc: string;
  reservationId: string;
  status: "Queued" | "Publishing" | "Published" | "Failed" | "Refunded";
  failureReason?: string | null;
  targets: { platform: string; externalAccountId: string }[];
  media?: { mediaAssetId: string; fileName: string; publicUrl: string; contentType: string }[];
};

type MediaAsset = {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  publicUrl: string;
};

type StripeWebhookEvent = {
  id: string;
  eventType: string;
  status: string;
  errorMessage?: string | null;
  receivedAtUtc: string;
  processedAtUtc?: string | null;
};

type LinkedInConnection = {
  id: string;
  tenantId: string;
  connectedByUserId: string;
  accessTokenExpiresAtUtc?: string | null;
  scope: string;
  memberUrn?: string | null;
  updatedAtUtc: string;
};

function App() {
  const currentPath = window.location.pathname;
  const query = new URLSearchParams(window.location.search);
  const isBillingSuccess = currentPath === "/billing/success";
  const isBillingCancel = currentPath === "/billing/cancel";
  const isLinkedInSuccess = currentPath === "/integrations/linkedin/success";
  const isLinkedInError = currentPath === "/integrations/linkedin/error";
  const linkedInErrorReason = query.get("reason");
  const linkedInErrorDescription = query.get("description");
  const [email, setEmail] = useState("demo@postpebble.local");
  const [password, setPassword] = useState("Passw0rd!123");
  const [tenantName, setTenantName] = useState("Demo Tenant");
  const [auth, setAuth] = useState<AuthResponse | null>(null);
  const [status, setStatus] = useState("Ready.");
  const [walletBalance, setWalletBalance] = useState<number | null>(null);
  const [transactions, setTransactions] = useState<CreditTransaction[]>([]);
  const [buyCreditsAmount, setBuyCreditsAmount] = useState(5);
  const [postText, setPostText] = useState("Hello from PostPebble");
  const [scheduledAtUtc, setScheduledAtUtc] = useState("2026-04-10T12:00:00Z");
  const [targets, setTargets] = useState("X:acc_x_1,LinkedIn:acc_li_1");
  const [scheduledPosts, setScheduledPosts] = useState<ScheduledPost[]>([]);
  const [mediaAssets, setMediaAssets] = useState<MediaAsset[]>([]);
  const [selectedMediaIds, setSelectedMediaIds] = useState<string[]>([]);
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const [webhookEvents, setWebhookEvents] = useState<StripeWebhookEvent[]>([]);
  const [linkedInConnections, setLinkedInConnections] = useState<LinkedInConnection[]>([]);
  const [linkedInMemberUrn, setLinkedInMemberUrn] = useState("");

  const activeTenant = useMemo(() => auth?.tenants?.[0] ?? null, [auth]);

  useEffect(() => {
    const raw = localStorage.getItem(authStorageKey);
    if (!raw) {
      return;
    }

    try {
      const parsed = JSON.parse(raw) as AuthResponse;
      if (!parsed?.accessToken) {
        return;
      }

      setAuth(parsed);
      void loadWalletWithToken(parsed.accessToken, parsed.tenants?.[0]?.tenantId);
      void loadMediaWithToken(parsed.accessToken, parsed.tenants?.[0]?.tenantId);
      void loadScheduledPostsWithToken(parsed.accessToken, parsed.tenants?.[0]?.tenantId);
      void loadLinkedInConnectionsWithToken(parsed.accessToken, parsed.tenants?.[0]?.tenantId);
      setStatus("Session restored.");
    } catch {
      localStorage.removeItem(authStorageKey);
    }
  }, []);

  async function register(event: FormEvent) {
    event.preventDefault();
    setStatus("Registering...");

    const response = await fetch(`${apiBaseUrl}/api/v1/auth/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password, tenantName })
    });

    if (!response.ok) {
      setStatus(`Register failed (${response.status}).`);
      return;
    }

    const json = (await response.json()) as AuthResponse;
    setAuth(json);
    localStorage.setItem(authStorageKey, JSON.stringify(json));
    setStatus("Registered and logged in.");
  }

  async function login() {
    setStatus("Logging in...");
    const response = await fetch(`${apiBaseUrl}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password })
    });
    if (!response.ok) {
      setStatus(`Login failed (${response.status}).`);
      return;
    }

    const json = (await response.json()) as AuthResponse;
    setAuth(json);
    localStorage.setItem(authStorageKey, JSON.stringify(json));
    setStatus("Logged in.");
    await loadWalletWithToken(json.accessToken, json.tenants[0]?.tenantId);
    await loadMediaWithToken(json.accessToken, json.tenants[0]?.tenantId);
    await loadScheduledPostsWithToken(json.accessToken, json.tenants[0]?.tenantId);
    await loadLinkedInConnectionsWithToken(json.accessToken, json.tenants[0]?.tenantId);
  }

  async function loadWalletWithToken(token: string, tenantId?: string) {
    if (!tenantId) {
      return;
    }
    const headers = { Authorization: `Bearer ${token}` };
    const walletResponse = await fetch(`${apiBaseUrl}/api/v1/billing/wallets/${tenantId}`, { headers });
    if (walletResponse.ok) {
      const walletJson = (await walletResponse.json()) as { balanceCredits: number };
      setWalletBalance(walletJson.balanceCredits);
    }
    const txResponse = await fetch(`${apiBaseUrl}/api/v1/billing/wallets/${tenantId}/transactions`, { headers });
    if (txResponse.ok) {
      setTransactions((await txResponse.json()) as CreditTransaction[]);
    }
    const webhookResponse = await fetch(`${apiBaseUrl}/api/v1/billing/stripe/webhook-events/${tenantId}`, { headers });
    if (webhookResponse.ok) {
      setWebhookEvents((await webhookResponse.json()) as StripeWebhookEvent[]);
    }
  }

  async function loadWallet() {
    if (!auth || !activeTenant) {
      return;
    }

    setStatus("Loading wallet...");
    await loadWalletWithToken(auth.accessToken, activeTenant.tenantId);
    setStatus("Wallet loaded.");
  }

  async function loadLinkedInConnectionsWithToken(token: string, tenantId?: string) {
    if (!tenantId) {
      return;
    }
    const response = await fetch(`${apiBaseUrl}/api/v1/integrations/linkedin/connections/${tenantId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (response.ok) {
      setLinkedInConnections((await response.json()) as LinkedInConnection[]);
    }
  }

  async function loadLinkedInConnections() {
    if (!auth || !activeTenant) {
      return;
    }
    await loadLinkedInConnectionsWithToken(auth.accessToken, activeTenant.tenantId);
  }

  async function connectLinkedIn() {
    if (!auth || !activeTenant) {
      return;
    }

    setStatus("Preparing LinkedIn authorization...");
    const response = await fetch(`${apiBaseUrl}/api/v1/integrations/linkedin/authorize`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`
      },
      body: JSON.stringify({ tenantId: activeTenant.tenantId })
    });

    if (!response.ok) {
      setStatus(`LinkedIn authorization setup failed (${response.status}).`);
      return;
    }

    const json = (await response.json()) as { authorizeUrl: string };
    setStatus("Redirecting to LinkedIn...");
    window.location.href = json.authorizeUrl;
  }

  async function saveLinkedInMemberUrn() {
    if (!auth || !activeTenant) {
      return;
    }
    const urn = linkedInMemberUrn.trim();
    if (!urn) {
      setStatus("Enter LinkedIn member URN first.");
      return;
    }
    const response = await fetch(`${apiBaseUrl}/api/v1/integrations/linkedin/connections/${activeTenant.tenantId}/member-urn`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`
      },
      body: JSON.stringify({ memberUrn: urn })
    });
    if (!response.ok) {
      setStatus(`Saving member URN failed (${response.status}).`);
      return;
    }
    setStatus("LinkedIn member URN saved.");
    await loadLinkedInConnections();
  }

  function logout() {
    setAuth(null);
    setWalletBalance(null);
    setTransactions([]);
    setMediaAssets([]);
    setScheduledPosts([]);
    setLinkedInConnections([]);
    setWebhookEvents([]);
    localStorage.removeItem(authStorageKey);
    setStatus("Logged out.");
  }

  async function buyCredits() {
    if (!auth || !activeTenant) {
      return;
    }

    setStatus("Creating Stripe checkout session...");
    const response = await fetch(`${apiBaseUrl}/api/v1/billing/credit-packs/checkout-session`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`
      },
      body: JSON.stringify({
        tenantId: activeTenant.tenantId,
        credits: buyCreditsAmount
      })
    });

    if (!response.ok) {
      setStatus(`Checkout creation failed (${response.status}).`);
      return;
    }

    const json = (await response.json()) as { sessionUrl: string };
    setStatus("Redirecting to Stripe Checkout...");
    window.location.href = json.sessionUrl;
  }

  async function createScheduledPost() {
    if (!auth || !activeTenant) {
      return;
    }

    const parsedTargets = targets
      .split(",")
      .map((x) => x.trim())
      .filter(Boolean)
      .map((raw) => {
        const [platform, externalAccountId] = raw.split(":");
        return { platform, externalAccountId };
      })
      .filter((x) => x.platform && x.externalAccountId);

    if (parsedTargets.length === 0) {
      setStatus("Add at least one target as Platform:ExternalAccountId.");
      return;
    }

    setStatus("Scheduling post...");
    const response = await fetch(`${apiBaseUrl}/api/v1/scheduler/posts`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`
      },
      body: JSON.stringify({
        tenantId: activeTenant.tenantId,
        textContent: postText,
        scheduledAtUtc,
        targets: parsedTargets,
        mediaAssetIds: selectedMediaIds
      })
    });

    if (!response.ok) {
      setStatus(`Schedule failed (${response.status}).`);
      return;
    }

    setStatus("Post scheduled and credits reserved.");
    await loadWallet();
    await loadScheduledPosts();
  }

  async function loadScheduledPostsWithToken(token: string, tenantId?: string) {
    if (!tenantId) {
      return;
    }
    const response = await fetch(`${apiBaseUrl}/api/v1/scheduler/posts/${tenantId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (response.ok) {
      setScheduledPosts((await response.json()) as ScheduledPost[]);
    }
  }

  async function loadScheduledPosts() {
    if (!auth || !activeTenant) {
      return;
    }

    await loadScheduledPostsWithToken(auth.accessToken, activeTenant.tenantId);
    setStatus("Scheduled posts loaded.");
  }

  async function loadMediaWithToken(token: string, tenantId?: string) {
    if (!tenantId) {
      return;
    }
    const response = await fetch(`${apiBaseUrl}/api/v1/media/${tenantId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    if (response.ok) {
      setMediaAssets((await response.json()) as MediaAsset[]);
    }
  }

  async function loadMedia() {
    if (!auth || !activeTenant) {
      return;
    }
    setStatus("Loading media...");
    await loadMediaWithToken(auth.accessToken, activeTenant.tenantId);
    setStatus("Media loaded.");
  }

  async function uploadMedia() {
    if (!auth || !activeTenant || !uploadFile) {
      setStatus("Choose a file first.");
      return;
    }

    const form = new FormData();
    form.append("tenantId", activeTenant.tenantId);
    form.append("file", uploadFile);

    setStatus("Uploading media...");
    const response = await fetch(`${apiBaseUrl}/api/v1/media/upload`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.accessToken}` },
      body: form
    });

    if (!response.ok) {
      setStatus(`Upload failed (${response.status}).`);
      return;
    }

    setUploadFile(null);
    await loadMedia();
    setStatus("Media uploaded.");
  }

  async function settlePost(postId: string, outcome: "success" | "failed") {
    if (!auth) {
      return;
    }

    const endpoint =
      outcome === "success"
        ? `${apiBaseUrl}/api/v1/scheduler/posts/${postId}/mark-success`
        : `${apiBaseUrl}/api/v1/scheduler/posts/${postId}/mark-failed`;
    const isFailed = outcome === "failed";
    const response = await fetch(endpoint, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${auth.accessToken}`,
        ...(isFailed ? { "Content-Type": "application/json" } : {})
      },
      ...(isFailed ? { body: JSON.stringify({ reason: "Manually marked as failed from dashboard." }) } : {})
    });

    if (!response.ok) {
      setStatus(`Settle failed (${response.status}).`);
      return;
    }

    setStatus(`Post marked ${outcome}.`);
    await loadWallet();
    await loadScheduledPosts();
  }

  async function markPublishing(postId: string) {
    if (!auth) {
      return;
    }
    const response = await fetch(`${apiBaseUrl}/api/v1/scheduler/posts/${postId}/mark-publishing`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.accessToken}` }
    });
    if (!response.ok) {
      setStatus(`Mark publishing failed (${response.status}).`);
      return;
    }
    setStatus("Post marked publishing.");
    await loadScheduledPosts();
  }

  function statusClass(status: ScheduledPost["status"]) {
    switch (status) {
      case "Published":
        return "statusBadge success";
      case "Refunded":
        return "statusBadge refunded";
      case "Failed":
        return "statusBadge failed";
      case "Publishing":
        return "statusBadge publishing";
      default:
        return "statusBadge queued";
    }
  }

  return (
    <main className="page">
      <header className="hero">
        <h1>PostPebble</h1>
        <p>Multi-tenant social scheduler with credits, media, and Stripe checkout.</p>
      </header>

      {isBillingSuccess && (
        <section className="card">
          <h2>Payment successful</h2>
          <p>Your Stripe checkout completed. Refresh wallet to see updated credits after webhook processing.</p>
          <button onClick={loadWallet} disabled={!auth || !activeTenant}>Refresh wallet</button>
        </section>
      )}

      {isBillingCancel && (
        <section className="card">
          <h2>Payment canceled</h2>
          <p>No worries — you can try checkout again whenever you want.</p>
        </section>
      )}

      {isLinkedInSuccess && (
        <section className="card">
          <h2>LinkedIn connected</h2>
          <p>Your LinkedIn OAuth flow completed. Refresh connections to verify token details.</p>
          <button onClick={loadLinkedInConnections} disabled={!auth || !activeTenant}>Refresh LinkedIn connections</button>
        </section>
      )}

      {isLinkedInError && (
        <section className="card">
          <h2>LinkedIn connection failed</h2>
          <p>OAuth callback returned an error. Try connecting again and check LinkedIn app configuration.</p>
          {linkedInErrorReason && <p><strong>Reason:</strong> {linkedInErrorReason}</p>}
          {linkedInErrorDescription && <p><strong>Description:</strong> {linkedInErrorDescription}</p>}
        </section>
      )}

      <section className="card">
        <h2>Auth</h2>
        <form onSubmit={register} className="grid">
          <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email" />
          <input value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Password" type="password" />
          <input value={tenantName} onChange={(e) => setTenantName(e.target.value)} placeholder="Tenant name" />
          <div className="row">
            <button type="submit">Register</button>
            <button type="button" onClick={login}>Login</button>
            <button type="button" onClick={logout} disabled={!auth}>Logout</button>
          </div>
        </form>
      </section>

      <section className="card">
        <h2>LinkedIn Integration</h2>
        <div className="row">
          <button onClick={connectLinkedIn} disabled={!auth || !activeTenant}>Connect LinkedIn</button>
          <button onClick={loadLinkedInConnections} disabled={!auth || !activeTenant}>Refresh LinkedIn status</button>
        </div>
        <div className="row" style={{ marginTop: "0.5rem" }}>
          <input
            value={linkedInMemberUrn}
            onChange={(e) => setLinkedInMemberUrn(e.target.value)}
            placeholder="urn:li:person:..."
            style={{ minWidth: 320 }}
          />
          <button onClick={saveLinkedInMemberUrn} disabled={!auth || !activeTenant}>Save member URN</button>
        </div>
        {linkedInConnections.length === 0 ? (
          <p>No LinkedIn connection for this tenant yet.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th align="left">Updated</th>
                <th align="left">Token Expires</th>
                <th align="left">Scopes</th>
                <th align="left">Member URN</th>
              </tr>
            </thead>
            <tbody>
              {linkedInConnections.map((connection) => (
                <tr key={connection.id}>
                  <td>{new Date(connection.updatedAtUtc).toLocaleString()}</td>
                  <td>{connection.accessTokenExpiresAtUtc ? new Date(connection.accessTokenExpiresAtUtc).toLocaleString() : "-"}</td>
                  <td>{connection.scope || "-"}</td>
                  <td>{connection.memberUrn || "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="card">
        <h2>Tenant Wallet</h2>
        <p>
          Active tenant: <strong>{activeTenant?.tenantName ?? "none"}</strong>
        </p>
        <p>Balance: <strong>{walletBalance ?? "-"}</strong> credits</p>
        <div className="row">
          <button onClick={loadWallet} disabled={!auth || !activeTenant}>Refresh wallet</button>
          <input
            type="number"
            min={1}
            value={buyCreditsAmount}
            onChange={(e) => setBuyCreditsAmount(Number(e.target.value))}
            className="smallInput"
          />
          <button onClick={buyCredits} disabled={!auth || !activeTenant}>Buy credits</button>
        </div>
      </section>

      <section className="card">
        <h2>Recent Transactions</h2>
        {transactions.length === 0 ? (
          <p>No transactions yet.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th align="left">Type</th>
                <th align="left">Amount</th>
                <th align="left">Reference</th>
              </tr>
            </thead>
            <tbody>
              {transactions.map((tx) => (
                <tr key={tx.id}>
                  <td>{tx.type}</td>
                  <td>{tx.amountCredits}</td>
                  <td>{tx.referenceId}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="card">
        <h2>Media Library</h2>
        <div className="row">
          <input type="file" onChange={(e) => setUploadFile(e.target.files?.[0] ?? null)} />
          <button onClick={uploadMedia} disabled={!auth || !activeTenant}>Upload media</button>
          <button onClick={loadMedia} disabled={!auth || !activeTenant}>Refresh media</button>
        </div>
        <div className="chips">
          {mediaAssets.map((asset) => {
            const selected = selectedMediaIds.includes(asset.id);
            return (
              <div key={asset.id} className={`assetCard ${selected ? "selected" : ""}`}>
                {asset.contentType.startsWith("image/") ? (
                  <img className="assetPreview" src={`${apiBaseUrl}${asset.publicUrl}`} alt={asset.originalFileName} />
                ) : (
                  <div className="assetPreview assetPlaceholder">Video/File</div>
                )}
                <button
                  className={`chip ${selected ? "selected" : ""}`}
                  onClick={() =>
                    setSelectedMediaIds((prev) =>
                      prev.includes(asset.id) ? prev.filter((x) => x !== asset.id) : [...prev, asset.id]
                    )
                  }
                >
                  {asset.originalFileName}
                </button>
              </div>
            );
          })}
          {mediaAssets.length === 0 && <p>No media yet.</p>}
        </div>
      </section>

      <section className="card">
        <h2>Scheduler</h2>
        <div className="grid">
          <input value={postText} onChange={(e) => setPostText(e.target.value)} placeholder="Post text" />
          <input
            value={scheduledAtUtc}
            onChange={(e) => setScheduledAtUtc(e.target.value)}
            placeholder="UTC date time (e.g. 2026-04-10T12:00:00Z)"
          />
          <input
            value={targets}
            onChange={(e) => setTargets(e.target.value)}
            placeholder="Targets: X:acc_x_1,LinkedIn:acc_li_1"
          />
          <div className="row">
            <button onClick={createScheduledPost} disabled={!auth || !activeTenant}>Schedule post</button>
            <button onClick={loadScheduledPosts} disabled={!auth || !activeTenant}>Refresh scheduled posts</button>
          </div>
          <small>Selected media: {selectedMediaIds.length}</small>
        </div>

        <div className="list">
          {scheduledPosts.length === 0 ? (
            <p>No scheduled posts yet.</p>
          ) : (
            <table className="table">
              <thead>
                <tr>
                  <th align="left">Text</th>
                  <th align="left">When</th>
                  <th align="left">Status</th>
                  <th align="left">Targets</th>
                  <th align="left">Actions</th>
                </tr>
              </thead>
              <tbody>
                {scheduledPosts.map((post) => (
                  <tr key={post.id}>
                    <td>{post.textContent}</td>
                    <td>{new Date(post.scheduledAtUtc).toLocaleString()}</td>
                    <td>
                      <span className={statusClass(post.status)}>{post.status}</span>
                    </td>
                    <td>
                      {post.targets.map((t) => `${t.platform}:${t.externalAccountId}`).join(", ")}
                      {post.media && post.media.length > 0 && (
                        <div className="postMediaRow">
                          {post.media.map((m) =>
                            m.contentType.startsWith("image/") ? (
                              <img key={m.mediaAssetId} className="postMediaThumb" src={`${apiBaseUrl}${m.publicUrl}`} alt={m.fileName} />
                            ) : (
                              <span key={m.mediaAssetId} className="miniTag">{m.fileName}</span>
                            )
                          )}
                        </div>
                      )}
                      {post.failureReason && <div><small>Reason: {post.failureReason}</small></div>}
                    </td>
                    <td>
                      <button onClick={() => markPublishing(post.id)}>Mark publishing</button>{" "}
                      <button onClick={() => settlePost(post.id, "success")}>Mark success</button>{" "}
                      <button onClick={() => settlePost(post.id, "failed")}>Mark failed</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <section className="card">
        <h2>Stripe Webhook Events</h2>
        {webhookEvents.length === 0 ? (
          <p>No webhook events for current tenant yet.</p>
        ) : (
          <table className="table">
            <thead>
              <tr>
                <th align="left">Event</th>
                <th align="left">Status</th>
                <th align="left">Received</th>
                <th align="left">Error</th>
              </tr>
            </thead>
            <tbody>
              {webhookEvents.map((evt) => (
                <tr key={evt.id}>
                  <td>{evt.eventType}</td>
                  <td>{evt.status}</td>
                  <td>{new Date(evt.receivedAtUtc).toLocaleString()}</td>
                  <td>{evt.errorMessage ?? "-"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="statusBar">
        <p>Status: {status}</p>
        <small>API: {apiBaseUrl}</small>
      </section>
    </main>
  );
}

export default App;
