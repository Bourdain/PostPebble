import React, { createContext, useContext, useEffect, useMemo, useState } from "react";

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:8080";
const authStorageKey = "postpebble_auth";

export type TenantSummary = {
  tenantId: string;
  tenantName: string;
  role: string;
};

export type AuthResponse = {
  accessToken: string;
  expiresAtUtc: string;
  tenants: TenantSummary[];
};

export type CreditTransaction = {
  id: string;
  type: string;
  amountCredits: number;
  referenceId: string;
  description: string;
  createdAtUtc: string;
};

export type ScheduledPost = {
  id: string;
  textContent: string;
  scheduledAtUtc: string;
  reservationId: string;
  status: "Draft" | "Queued" | "Publishing" | "Published" | "Failed" | "Refunded" | "Cancelled";
  failureReason?: string | null;
  retryCount: number;
  maxRetries: number;
  nextRetryAtUtc?: string | null;
  targets: { platform: string; externalAccountId: string }[];
  media?: { mediaAssetId: string; fileName: string; publicUrl: string; contentType: string }[];
};

export type MediaAsset = {
  id: string;
  originalFileName: string;
  contentType: string;
  sizeBytes: number;
  publicUrl: string;
  tags?: string[];
};

export type StripeWebhookEvent = {
  id: string;
  eventType: string;
  status: string;
  errorMessage?: string | null;
  receivedAtUtc: string;
  processedAtUtc?: string | null;
};

export type LinkedInConnection = {
  id: string;
  tenantId: string;
  connectedByUserId: string;
  accessTokenExpiresAtUtc?: string | null;
  scope: string;
  memberUrn?: string | null;
  updatedAtUtc: string;
};

export type AnalyticsSummary = {
  totalPosts: number;
  publishedLast7Days: number;
  publishedLast30Days: number;
  failedLast30Days: number;
  queuedCount: number;
  successRate: number;
  creditsPurchasedLast30Days: number;
  creditsConsumedLast30Days: number;
  postsPerDay: { date: string; count: number }[];
  platformBreakdown: { platform: string; count: number }[];
};

type PostPebbleContextType = {
  apiBaseUrl: string;
  auth: AuthResponse | null;
  activeTenant: TenantSummary | null;
  walletBalance: number | null;
  transactions: CreditTransaction[];
  buyCreditsAmount: number;
  setBuyCreditsAmount: (amount: number) => void;
  scheduledPosts: ScheduledPost[];
  mediaAssets: MediaAsset[];
  webhookEvents: StripeWebhookEvent[];
  linkedInConnections: LinkedInConnection[];
  analytics: AnalyticsSummary | null;
  status: string;
  setStatus: (status: string) => void;
  register: (email: string, password: string, tenantName: string) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  loadWallet: () => Promise<void>;
  loadLinkedInConnections: () => Promise<void>;
  connectLinkedIn: () => Promise<void>;
  saveLinkedInMemberUrn: (urn: string) => Promise<void>;
  buyCredits: () => Promise<void>;
  createScheduledPost: (textContent: string, scheduledAtUtc: string, targets: { platform: string; externalAccountId: string }[], mediaAssetIds: string[], queueImmediately?: boolean) => Promise<void>;
  updateScheduledPost: (postId: string, data: { textContent?: string; scheduledAtUtc?: string; targets?: { platform: string; externalAccountId: string }[] }) => Promise<void>;
  cancelScheduledPost: (postId: string) => Promise<void>;
  loadScheduledPosts: () => Promise<void>;
  loadMedia: () => Promise<void>;
  uploadMedia: (file: File, tags?: string) => Promise<void>;
  updateMediaTags: (mediaId: string, tags: string[]) => Promise<void>;
  deleteMedia: (mediaId: string) => Promise<void>;
  settlePost: (postId: string, outcome: "success" | "failed") => Promise<void>;
  markPublishing: (postId: string) => Promise<void>;
  loadAnalytics: () => Promise<void>;
};

const PostPebbleContext = createContext<PostPebbleContextType | null>(null);

export function PostPebbleProvider({ children }: { children: React.ReactNode }) {
  const [auth, setAuth] = useState<AuthResponse | null>(null);
  const [status, setStatus] = useState("Ready.");
  const [walletBalance, setWalletBalance] = useState<number | null>(null);
  const [transactions, setTransactions] = useState<CreditTransaction[]>([]);
  const [buyCreditsAmount, setBuyCreditsAmount] = useState(5);
  const [scheduledPosts, setScheduledPosts] = useState<ScheduledPost[]>([]);
  const [mediaAssets, setMediaAssets] = useState<MediaAsset[]>([]);
  const [webhookEvents, setWebhookEvents] = useState<StripeWebhookEvent[]>([]);
  const [linkedInConnections, setLinkedInConnections] = useState<LinkedInConnection[]>([]);
  const [analytics, setAnalytics] = useState<AnalyticsSummary | null>(null);

  const activeTenant = useMemo(() => auth?.tenants?.[0] ?? null, [auth]);

  useEffect(() => {
    const raw = localStorage.getItem(authStorageKey);
    if (!raw) return;

    try {
      const parsed = JSON.parse(raw) as AuthResponse;
      if (!parsed?.accessToken) return;

      setAuth(parsed);
      const tenantId = parsed.tenants?.[0]?.tenantId;
      void loadWalletWithToken(parsed.accessToken, tenantId);
      void loadMediaWithToken(parsed.accessToken, tenantId);
      void loadScheduledPostsWithToken(parsed.accessToken, tenantId);
      void loadLinkedInConnectionsWithToken(parsed.accessToken, tenantId);
      void loadAnalyticsWithToken(parsed.accessToken, tenantId);
      setStatus("Session restored.");
    } catch {
      localStorage.removeItem(authStorageKey);
    }
  }, []);

  async function register(email: string, password: string, tenantName: string) {
    setStatus("Registering...");
    const response = await fetch(`${apiBaseUrl}/api/v1/auth/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password, tenantName }),
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

  async function login(email: string, password: string) {
    setStatus("Logging in...");
    const response = await fetch(`${apiBaseUrl}/api/v1/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
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
    await loadAnalyticsWithToken(json.accessToken, json.tenants[0]?.tenantId);
  }

  async function loadWalletWithToken(token: string, tenantId?: string) {
    if (!tenantId) return;
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
    if (!auth || !activeTenant) return;
    setStatus("Loading wallet...");
    await loadWalletWithToken(auth.accessToken, activeTenant.tenantId);
    setStatus("Wallet loaded.");
  }

  async function loadLinkedInConnectionsWithToken(token: string, tenantId?: string) {
    if (!tenantId) return;
    const response = await fetch(`${apiBaseUrl}/api/v1/integrations/linkedin/connections/${tenantId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (response.ok) {
      setLinkedInConnections((await response.json()) as LinkedInConnection[]);
    }
  }

  async function loadLinkedInConnections() {
    if (!auth || !activeTenant) return;
    await loadLinkedInConnectionsWithToken(auth.accessToken, activeTenant.tenantId);
  }

  async function connectLinkedIn() {
    if (!auth || !activeTenant) return;

    setStatus("Preparing LinkedIn authorization...");
    const response = await fetch(`${apiBaseUrl}/api/v1/integrations/linkedin/authorize`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`,
      },
      body: JSON.stringify({ tenantId: activeTenant.tenantId }),
    });

    if (!response.ok) {
      setStatus(`LinkedIn authorization setup failed (${response.status}).`);
      return;
    }

    const json = (await response.json()) as { authorizeUrl: string };
    setStatus("Redirecting to LinkedIn...");
    window.location.href = json.authorizeUrl;
  }

  async function saveLinkedInMemberUrn(urn: string) {
    if (!auth || !activeTenant) return;
    if (!urn.trim()) {
      setStatus("Enter LinkedIn member URN first.");
      return;
    }
    const response = await fetch(`${apiBaseUrl}/api/v1/integrations/linkedin/connections/${activeTenant.tenantId}/member-urn`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`,
      },
      body: JSON.stringify({ memberUrn: urn.trim() }),
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
    setAnalytics(null);
    localStorage.removeItem(authStorageKey);
    setStatus("Logged out.");
  }

  async function buyCredits() {
    if (!auth || !activeTenant) return;

    setStatus("Creating Stripe checkout session...");
    const response = await fetch(`${apiBaseUrl}/api/v1/billing/credit-packs/checkout-session`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`,
      },
      body: JSON.stringify({
        tenantId: activeTenant.tenantId,
        credits: buyCreditsAmount,
      }),
    });

    if (!response.ok) {
      setStatus(`Checkout creation failed (${response.status}).`);
      return;
    }

    const json = (await response.json()) as { sessionUrl: string };
    setStatus("Redirecting to Stripe Checkout...");
    window.location.href = json.sessionUrl;
  }

  async function createScheduledPost(textContent: string, scheduledAtUtc: string, targets: { platform: string; externalAccountId: string }[], mediaAssetIds: string[], queueImmediately: boolean = false) {
    if (!auth || !activeTenant) return;

    if (targets.length === 0) {
      setStatus("Add at least one target.");
      return;
    }

    setStatus(queueImmediately ? "Scheduling post..." : "Creating draft...");
    const response = await fetch(`${apiBaseUrl}/api/v1/scheduler/posts`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`,
      },
      body: JSON.stringify({
        tenantId: activeTenant.tenantId,
        textContent,
        scheduledAtUtc,
        targets,
        mediaAssetIds,
        queueImmediately
      }),
    });

    if (!response.ok) {
      setStatus(queueImmediately ? `Schedule failed (${response.status}).` : `Draft failed (${response.status}).`);
      return;
    }

    setStatus(queueImmediately ? "Post scheduled and credits reserved." : "Draft created.");
    await loadWallet();
    await loadScheduledPosts();
  }

  async function updateScheduledPost(postId: string, data: { textContent?: string; scheduledAtUtc?: string; targets?: { platform: string; externalAccountId: string }[] }) {
    if (!auth) return;

    setStatus("Updating post...");
    const response = await fetch(`${apiBaseUrl}/api/v1/scheduler/posts/${postId}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`,
      },
      body: JSON.stringify(data),
    });

    if (!response.ok) {
      setStatus(`Update failed (${response.status}).`);
      return;
    }

    setStatus("Post updated.");
    await loadScheduledPosts();
  }

  async function cancelScheduledPost(postId: string) {
    if (!auth) return;

    setStatus("Cancelling post...");
    const response = await fetch(`${apiBaseUrl}/api/v1/scheduler/posts/${postId}/mark-cancelled`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.accessToken}` },
    });

    if (!response.ok) {
      setStatus(`Cancel failed (${response.status}).`);
      return;
    }

    setStatus("Post cancelled. Credits returned.");
    await loadWallet();
    await loadScheduledPosts();
  }

  async function loadScheduledPostsWithToken(token: string, tenantId?: string) {
    if (!tenantId) return;
    const response = await fetch(`${apiBaseUrl}/api/v1/scheduler/posts/${tenantId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (response.ok) {
      setScheduledPosts((await response.json()) as ScheduledPost[]);
    }
  }

  async function loadScheduledPosts() {
    if (!auth || !activeTenant) return;
    await loadScheduledPostsWithToken(auth.accessToken, activeTenant.tenantId);
    setStatus("Scheduled posts loaded.");
  }

  async function loadMediaWithToken(token: string, tenantId?: string) {
    if (!tenantId) return;
    const response = await fetch(`${apiBaseUrl}/api/v1/media/${tenantId}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (response.ok) {
      setMediaAssets((await response.json()) as MediaAsset[]);
    }
  }

  async function loadMedia() {
    if (!auth || !activeTenant) return;
    setStatus("Loading media...");
    await loadMediaWithToken(auth.accessToken, activeTenant.tenantId);
    setStatus("Media loaded.");
  }

  async function uploadMedia(file: File, tags?: string) {
    if (!auth || !activeTenant) return;

    const form = new FormData();
    form.append("tenantId", activeTenant.tenantId);
    form.append("file", file);
    if (tags) {
      form.append("tags", tags);
    }

    setStatus("Uploading media...");
    const response = await fetch(`${apiBaseUrl}/api/v1/media/upload`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.accessToken}` },
      body: form,
    });

    if (!response.ok) {
      setStatus(`Upload failed (${response.status}).`);
      return;
    }

    await loadMedia();
    setStatus("Media uploaded.");
  }

  async function updateMediaTags(mediaId: string, tags: string[]) {
    if (!auth || !activeTenant) return;

    setStatus("Updating tags...");
    const response = await fetch(`${apiBaseUrl}/api/v1/media/${activeTenant.tenantId}/${mediaId}/tags`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${auth.accessToken}`,
      },
      body: JSON.stringify({ tags }),
    });

    if (!response.ok) {
      setStatus(`Update tags failed (${response.status}).`);
      return;
    }

    await loadMedia();
    setStatus("Tags updated.");
  }

  async function deleteMedia(mediaId: string) {
    if (!auth || !activeTenant) return;

    setStatus("Deleting media...");
    const response = await fetch(`${apiBaseUrl}/api/v1/media/${activeTenant.tenantId}/${mediaId}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${auth.accessToken}` },
    });

    if (!response.ok) {
      setStatus(`Delete failed (${response.status}).`);
      return;
    }

    await loadMedia();
    setStatus("Media deleted.");
  }

  async function settlePost(postId: string, outcome: "success" | "failed") {
    if (!auth) return;

    const endpoint =
      outcome === "success"
        ? `${apiBaseUrl}/api/v1/scheduler/posts/${postId}/mark-success`
        : `${apiBaseUrl}/api/v1/scheduler/posts/${postId}/mark-failed`;
    const isFailed = outcome === "failed";
    const response = await fetch(endpoint, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${auth.accessToken}`,
        ...(isFailed ? { "Content-Type": "application/json" } : {}),
      },
      ...(isFailed ? { body: JSON.stringify({ reason: "Manually marked as failed from dashboard." }) } : {}),
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
    if (!auth) return;
    const response = await fetch(`${apiBaseUrl}/api/v1/scheduler/posts/${postId}/mark-publishing`, {
      method: "POST",
      headers: { Authorization: `Bearer ${auth.accessToken}` },
    });
    if (!response.ok) {
      setStatus(`Mark publishing failed (${response.status}).`);
      return;
    }
    setStatus("Post marked publishing.");
    await loadScheduledPosts();
  }

  async function loadAnalyticsWithToken(token: string, tenantId?: string) {
    if (!tenantId) return;
    const response = await fetch(`${apiBaseUrl}/api/v1/analytics/${tenantId}/summary`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (response.ok) {
      setAnalytics((await response.json()) as AnalyticsSummary);
    }
  }

  async function loadAnalytics() {
    if (!auth || !activeTenant) return;
    await loadAnalyticsWithToken(auth.accessToken, activeTenant.tenantId);
  }

  return (
    <PostPebbleContext.Provider
      value={{
        apiBaseUrl,
        auth,
        activeTenant,
        walletBalance,
        transactions,
        buyCreditsAmount,
        setBuyCreditsAmount,
        scheduledPosts,
        mediaAssets,
        webhookEvents,
        linkedInConnections,
        analytics,
        status,
        setStatus,
        register,
        login,
        logout,
        loadWallet,
        loadLinkedInConnections,
        connectLinkedIn,
        saveLinkedInMemberUrn,
        buyCredits,
        createScheduledPost,
        updateScheduledPost,
        cancelScheduledPost,
        loadScheduledPosts,
        loadMedia,
        uploadMedia,
        updateMediaTags,
        deleteMedia,
        settlePost,
        markPublishing,
        loadAnalytics,
      }}
    >
      {children}
    </PostPebbleContext.Provider>
  );
}

export function usePostPebble() {
  const context = useContext(PostPebbleContext);
  if (!context) {
    throw new Error("usePostPebble must be used within a PostPebbleProvider");
  }
  return context;
}
