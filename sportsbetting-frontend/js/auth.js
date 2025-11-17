/**
 * AUTHENTICATION MODULE
 * Handles user login, registration, and session management
 */

// Handle tab switching between login and register
document.addEventListener('DOMContentLoaded', () => {
    const tabs = document.querySelectorAll('.tab');
    const forms = {
        login: document.getElementById('loginForm'),
        register: document.getElementById('registerForm')
    };

    tabs.forEach(tab => {
        tab.addEventListener('click', () => {
            const tabName = tab.dataset.tab;

            // Update active tab
            tabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');

            // Show corresponding form
            Object.values(forms).forEach(form => form.classList.remove('active'));
            forms[tabName].classList.add('active');
        });
    });

    // Handle login form submission
    forms.login.addEventListener('submit', async (e) => {
        e.preventDefault();
        await handleLogin();
    });

    // Handle register form submission
    forms.register.addEventListener('submit', async (e) => {
        e.preventDefault();
        await handleRegister();
    });

    // Handle logout
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) {
        logoutBtn.addEventListener('click', handleLogout);
    }

    // Check if user is already logged in
    checkAuthStatus();
});

/**
 * Handle user login
 */
async function handleLogin() {
    const email = document.getElementById('loginEmail').value;
    const password = document.getElementById('loginPassword').value;

    try {
        showLoading('Logging in...');

        const response = await API.Auth.login({ usernameOrEmail: email, password });

        console.log('Login successful:', response);

        // Initialize user session
        await initializeUserSession();

        hideLoading();
        showSuccessMessage('Welcome back!');

        // Navigate to home
        setTimeout(() => navigateTo('home'), 1000);

    } catch (error) {
        hideLoading();
        showErrorMessage('Login failed: ' + error.message);
    }
}

/**
 * Handle user registration
 */
async function handleRegister() {
    const username = document.getElementById('regUsername').value;
    const email = document.getElementById('regEmail').value;
    const firstName = document.getElementById('regFirstName').value;
    const lastName = document.getElementById('regLastName').value;
    const password = document.getElementById('regPassword').value;

    try {
        showLoading('Creating account...');

        const response = await API.Auth.register({
            username,
            email,
            firstName,
            lastName,
            password
        });

        console.log('Registration successful:', response);

        hideLoading();

        // Show initial balance modal
        showInitialBalanceModal();

    } catch (error) {
        hideLoading();
        showErrorMessage('Registration failed: ' + error.message);
    }
}

/**
 * Show initial balance selection modal
 */
function showInitialBalanceModal() {
    const modal = document.getElementById('initialBalanceModal');
    modal.style.display = 'flex';
}

/**
 * Create wallet with initial balance
 */
async function createWalletWithBalance() {
    const initialBalance = parseFloat(document.getElementById('initialBalance').value);

    if (!initialBalance || initialBalance <= 0) {
        alert('Please enter a valid initial balance');
        return;
    }

    const user = API.Auth.getCurrentUser();

    try {
        showLoading('Creating wallet...');

        // Create wallet via API
        await API.Wallets.createWallet({
            userId: user.id,
            initialBalance: initialBalance,
            currency: 'USD',
            description: 'Initial deposit'
        });

        hideLoading();

        // Close modal
        document.getElementById('initialBalanceModal').style.display = 'none';

        // Initialize session
        await initializeUserSession();

        showSuccessMessage(`Account created with $${initialBalance.toFixed(2)} balance!`);

        // Navigate to home
        setTimeout(() => navigateTo('home'), 1000);

    } catch (error) {
        hideLoading();
        showErrorMessage('Failed to create wallet: ' + error.message);
    }
}


/**
 * Handle user logout
 */
function handleLogout() {
    API.Auth.logout();

    // Reset UI
    document.getElementById('userInfo').innerHTML = '';
    document.getElementById('logoutBtn').style.display = 'none';

    // Show login button in top right
    const loginTopBtn = document.getElementById('loginTopBtn');
    if (loginTopBtn) loginTopBtn.style.display = 'block';

    // Navigate to login page
    navigateTo('login');

    showSuccessMessage('Logged out successfully');
}

/**
 * Check if user is already authenticated
 */
function checkAuthStatus() {
    const loginTopBtn = document.getElementById('loginTopBtn');

    if (API.Auth.isAuthenticated()) {
        const user = API.Auth.getCurrentUser();
        console.log('✅ User is authenticated:', user.username, user.email);

        // Show user info in sidebar (do this immediately)
        displayUserInfo(user);

        // Hide login button (user is logged in)
        if (loginTopBtn) loginTopBtn.style.display = 'none';

        // Show logout button
        const logoutBtn = document.getElementById('logoutBtn');
        if (logoutBtn) logoutBtn.style.display = 'flex';

        // Initialize user session (async - loads wallet, stats, etc.)
        initializeUserSession().catch(err => {
            console.error('Failed to initialize session:', err);
        });

        // Navigate to home if on login page
        const loginPage = document.getElementById('page-login');
        if (loginPage.classList.contains('active')) {
            navigateTo('home');
        }
    } else {
        console.log('❌ User is not authenticated');

        // Show login button (user not logged in)
        if (loginTopBtn) loginTopBtn.style.display = 'block';

        // Hide logout button
        const logoutBtn = document.getElementById('logoutBtn');
        if (logoutBtn) logoutBtn.style.display = 'none';

        // Clear user info
        const userInfo = document.getElementById('userInfo');
        if (userInfo) userInfo.innerHTML = '';

        // Show login page
        navigateTo('login');
    }
}

/**
 * Initialize user session (load wallet, bets, etc.)
 */
async function initializeUserSession() {
    const user = API.Auth.getCurrentUser();
    if (!user) return;

    console.log('Initializing user session for:', user.username);

    // Display user info in sidebar (always do this first)
    displayUserInfo(user);

    // Load wallet balance
    try {
        const wallet = await API.Wallets.getUserWallet(user.id);
        updateWalletDisplay(wallet);
        console.log('Wallet loaded:', wallet.balance);
    } catch (error) {
        console.error('Failed to load wallet:', error);
        // If wallet fails, set default values to prevent UI issues
        const walletBalance = document.getElementById('walletBalance');
        if (walletBalance) walletBalance.textContent = '$0.00';
        document.getElementById('walletBalanceDetail').textContent = '$0.00';
        document.getElementById('totalDeposited').textContent = '$0.00';
        document.getElementById('totalBet').textContent = '$0.00';
        document.getElementById('totalWon').textContent = '$0.00';
        document.getElementById('netProfitLoss').textContent = '$0.00';
    }

    // Load user stats (only if on home page)
    if (document.getElementById('page-home').classList.contains('active')) {
        loadDashboardStats();
    }
}

/**
 * Display user info in sidebar
 */
function displayUserInfo(user) {
    const userInfo = document.getElementById('userInfo');
    const logoutBtn = document.getElementById('logoutBtn');
    const loginTopBtn = document.getElementById('loginTopBtn');

    userInfo.innerHTML = `
        <div style="text-align: center;">
            <div style="font-size: 2rem; margin-bottom: 0.5rem;">👤</div>
            <div style="font-weight: 600; margin-bottom: 0.25rem;">${user.username}</div>
            <div style="font-size: 0.85rem; color: rgba(255, 255, 255, 0.5);">${user.email}</div>
        </div>
    `;

    logoutBtn.style.display = 'flex';

    // Hide login button when user is logged in
    if (loginTopBtn) loginTopBtn.style.display = 'none';
}

/**
 * Update wallet balance display
 */
function updateWalletDisplay(wallet) {
    // Update home page stats
    const walletBalance = document.getElementById('walletBalance');
    if (walletBalance) {
        walletBalance.textContent = `$${wallet.balance.toFixed(2)}`;
    }

    // Update wallet page
    document.getElementById('walletBalanceDetail').textContent = `$${wallet.balance.toFixed(2)}`;
    document.getElementById('totalDeposited').textContent = `$${wallet.totalDeposited.toFixed(2)}`;
    document.getElementById('totalBet').textContent = `$${wallet.totalBet.toFixed(2)}`;
    document.getElementById('totalWon').textContent = `$${wallet.totalWon.toFixed(2)}`;

    const netPL = document.getElementById('netProfitLoss');
    netPL.textContent = `$${wallet.netProfitLoss.toFixed(2)}`;
    netPL.className = 'wallet-stat-value ' + (wallet.netProfitLoss >= 0 ? 'green' : 'red');
}

/**
 * Show loading message
 */
function showLoading(message) {
    // Could implement a loading spinner here
    console.log('Loading:', message);
}

/**
 * Hide loading message
 */
function hideLoading() {
    console.log('Loading complete');
}

/**
 * Show error message
 */
function showErrorMessage(message) {
    alert('❌ ' + message);
}

/**
 * Show success message
 */
function showSuccessMessage(message) {
    console.log('✅', message);
}
