/**
 * SPORTSBETTING API CLIENT
 * Comprehensive API wrapper exposing all backend endpoints
 * Base URL: http://localhost:5192/api
 */

const API_BASE_URL = 'http://localhost:5192/api';

// ========================================
// AUTHENTICATION STATE
// ========================================

function getAuthToken() {
    return localStorage.getItem('authToken');
}

function getCurrentUserFromStorage() {
    const userJson = localStorage.getItem('currentUser');
    return userJson ? JSON.parse(userJson) : null;
}

function setAuthToken(token) {
    localStorage.setItem('authToken', token);
    console.log('🔑 Auth token set');
}

function setCurrentUser(user) {
    localStorage.setItem('currentUser', JSON.stringify(user));
    console.log('👤 Current user set:', user.username);
}

function clearAuth() {
    localStorage.removeItem('authToken');
    localStorage.removeItem('currentUser');
    console.log('🚪 Auth cleared');
}

function getAuthHeaders() {
    const token = getAuthToken();
    if (token) {
        console.log('✅ Sending auth header with token');
        return { 'Authorization': `Bearer ${token}` };
    } else {
        console.log('⚠️ No auth token available');
        return {};
    }
}

// ========================================
// HTTP CLIENT
// ========================================

async function apiRequest(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    const defaultHeaders = {
        'Content-Type': 'application/json',
        ...getAuthHeaders()
    };

    const config = {
        ...options,
        headers: { ...defaultHeaders, ...options.headers }
    };

    try {
        const response = await fetch(url, config);

        if (!response.ok) {
            const error = await response.text();
            throw new Error(error || `HTTP ${response.status}: ${response.statusText}`);
        }

        // Some endpoints return 204 No Content
        if (response.status === 204) {
            return null;
        }

        return await response.json();
    } catch (error) {
        console.error(`API Error [${endpoint}]:`, error);
        throw error;
    }
}

// ========================================
// AUTH ENDPOINTS
// ========================================

const AuthAPI = {
    /**
     * Register a new user
     * POST /api/auth/register
     */
    async register(userData) {
        const response = await apiRequest('/auth/register', {
            method: 'POST',
            body: JSON.stringify(userData)
        });

        setAuthToken(response.accessToken);
        setCurrentUser(response.user);

        return response;
    },

    /**
     * Login existing user
     * POST /api/auth/login
     */
    async login(credentials) {
        const response = await apiRequest('/auth/login', {
            method: 'POST',
            body: JSON.stringify(credentials)
        });

        setAuthToken(response.accessToken);
        setCurrentUser(response.user);

        return response;
    },

    /**
     * Refresh access token
     * POST /api/auth/refresh
     */
    async refreshToken(refreshToken) {
        const response = await apiRequest('/auth/refresh', {
            method: 'POST',
            body: JSON.stringify({ refreshToken })
        });

        setAuthToken(response.accessToken);

        return response;
    },

    /**
     * Logout user
     */
    logout() {
        clearAuth();
    },

    /**
     * Get current user
     */
    getCurrentUser() {
        return getCurrentUserFromStorage();
    },

    /**
     * Check if user is authenticated
     */
    isAuthenticated() {
        return !!getAuthToken() && !!getCurrentUserFromStorage();
    }
};

// ========================================
// EVENTS ENDPOINTS
// ========================================

const EventsAPI = {
    /**
     * Get all events with optional filters
     * GET /api/events
     * @param {Object} params - Query parameters
     *   - status: 'Scheduled' | 'InProgress' | 'Completed'
     *   - leagueId: string
     *   - page: number
     *   - pageSize: number
     */
    async getEvents(params = {}) {
        const queryString = new URLSearchParams(params).toString();
        return await apiRequest(`/events${queryString ? '?' + queryString : ''}`);
    },

    /**
     * Get specific event by ID
     * GET /api/events/{id}
     */
    async getEventById(eventId) {
        return await apiRequest(`/events/${eventId}`);
    },

    /**
     * Get market with outcomes and odds
     * GET /api/events/markets/{marketId}
     */
    async getMarket(marketId) {
        return await apiRequest(`/events/markets/${marketId}`);
    },

    /**
     * Get events by sport (helper method)
     */
    async getEventsBySport(sport, status = 'Scheduled') {
        // Map sport to league ID (hardcoded for now - could be fetched from leagues API)
        const leagueIds = {
            'nfl': '8655c224-b950-4f2a-8476-05a374efbc3e',
            'nba': 'a689a590-751c-4e7c-b8b9-134700a55665'
        };

        const events = await this.getEvents({ status, pageSize: 100 });

        // Filter by sport using league ID
        const targetLeagueId = leagueIds[sport.toLowerCase()];
        if (!targetLeagueId) {
            return events;
        }

        return events.filter(event => event.leagueId === targetLeagueId);
    }
};

// ========================================
// BETS ENDPOINTS
// ========================================

const BetsAPI = {
    /**
     * Place a single bet
     * POST /api/bets/single
     */
    async placeSingleBet(betData) {
        return await apiRequest('/bets/single', {
            method: 'POST',
            body: JSON.stringify(betData)
        });
    },

    /**
     * Place a parlay bet
     * POST /api/bets/parlay
     */
    async placeParlayBet(betData) {
        return await apiRequest('/bets/parlay', {
            method: 'POST',
            body: JSON.stringify(betData)
        });
    },

    /**
     * Get user's bets
     * GET /api/bets/user/{userId}
     * @param {string} userId
     * @param {Object} params - Optional filters
     *   - status: 'Pending' | 'Won' | 'Lost' | 'Void' | 'Pushed'
     *   - page: number
     *   - pageSize: number
     */
    async getUserBets(userId, params = {}) {
        const queryString = new URLSearchParams(params).toString();
        return await apiRequest(`/bets/user/${userId}${queryString ? '?' + queryString : ''}`);
    },

    /**
     * Get bet by ID
     * GET /api/bets/{betId}
     */
    async getBetById(betId) {
        return await apiRequest(`/bets/${betId}`);
    }
};

// ========================================
// WALLET ENDPOINTS
// ========================================

const WalletsAPI = {
    /**
     * Create a new wallet
     * POST /api/wallets
     */
    async createWallet(walletData) {
        return await apiRequest('/wallets', {
            method: 'POST',
            body: JSON.stringify(walletData)
        });
    },

    /**
     * Get user's wallet
     * GET /api/wallets/user/{userId}
     */
    async getUserWallet(userId) {
        return await apiRequest(`/wallets/user/${userId}`);
    },

    /**
     * Get wallet by ID
     * GET /api/wallets/{walletId}
     */
    async getWalletById(walletId) {
        return await apiRequest(`/wallets/${walletId}`);
    },

    /**
     * Deposit funds (if endpoint exists)
     * POST /api/wallets/{walletId}/deposit
     */
    async deposit(walletId, amount) {
        return await apiRequest(`/wallets/${walletId}/deposit`, {
            method: 'POST',
            body: JSON.stringify({ amount })
        });
    },

    /**
     * Withdraw funds (if endpoint exists)
     * POST /api/wallets/{walletId}/withdraw
     */
    async withdraw(walletId, amount) {
        return await apiRequest(`/wallets/${walletId}/withdraw`, {
            method: 'POST',
            body: JSON.stringify({ amount })
        });
    }
};

// ========================================
// EXCHANGE ENDPOINTS (if available)
// ========================================

const ExchangeAPI = {
    /**
     * Place exchange bet (back or lay)
     * POST /api/exchange/place-bet
     */
    async placeExchangeBet(betData) {
        return await apiRequest('/exchange/place-bet', {
            method: 'POST',
            body: JSON.stringify(betData)
        });
    },

    /**
     * Get order book for a market
     * GET /api/exchange/markets/{marketId}/orderbook
     */
    async getOrderBook(marketId) {
        return await apiRequest(`/exchange/markets/${marketId}/orderbook`);
    },

    /**
     * Cancel an exchange bet
     * POST /api/exchange/cancel/{betId}
     */
    async cancelBet(betId) {
        return await apiRequest(`/exchange/cancel/${betId}`, {
            method: 'POST'
        });
    }
};

// ========================================
// ADMIN ENDPOINTS (if user has admin role)
// ========================================

const AdminAPI = {
    /**
     * Get all users
     * GET /api/admin/users
     */
    async getAllUsers() {
        return await apiRequest('/admin/users');
    },

    /**
     * Get all bets (admin view)
     * GET /api/admin/bets
     */
    async getAllBets(params = {}) {
        const queryString = new URLSearchParams(params).toString();
        return await apiRequest(`/admin/bets${queryString ? '?' + queryString : ''}`);
    },

    /**
     * Manually settle a bet
     * POST /api/admin/settle/{betId}
     */
    async settleBet(betId, result) {
        return await apiRequest(`/admin/settle/${betId}`, {
            method: 'POST',
            body: JSON.stringify({ result })
        });
    }
};

// ========================================
// SETTLEMENT ENDPOINTS
// ========================================

const SettlementAPI = {
    /**
     * Manually trigger settlement for an event
     * POST /api/settlement/events/{eventId}/settle
     */
    async settleEvent(eventId, finalScore) {
        return await apiRequest(`/settlement/events/${eventId}/settle`, {
            method: 'POST',
            body: JSON.stringify({ finalScore })
        });
    },

    /**
     * Get settlement history
     * GET /api/settlement/history
     */
    async getSettlementHistory(params = {}) {
        const queryString = new URLSearchParams(params).toString();
        return await apiRequest(`/settlement/history${queryString ? '?' + queryString : ''}`);
    }
};

// ========================================
// REVENUE ENDPOINTS (if available)
// ========================================

const RevenueAPI = {
    /**
     * Get house revenue statistics
     * GET /api/revenue/stats
     */
    async getRevenueStats(params = {}) {
        const queryString = new URLSearchParams(params).toString();
        return await apiRequest(`/revenue/stats${queryString ? '?' + queryString : ''}`);
    }
};

// ========================================
// EXPORT ALL APIs
// ========================================

window.API = {
    Auth: AuthAPI,
    Events: EventsAPI,
    Bets: BetsAPI,
    Wallets: WalletsAPI,
    Exchange: ExchangeAPI,
    Admin: AdminAPI,
    Settlement: SettlementAPI,
    Revenue: RevenueAPI
};

console.log('✅ SportsBetting API Client loaded');
console.log('Available APIs:', Object.keys(window.API));
