/**
 * MAIN APP MODULE
 * Handles navigation, routing, and dashboard stats
 */

/**
 * Navigate to a page
 */
function navigateTo(pageName) {
    // Update nav items
    const navItems = document.querySelectorAll('.nav-item');
    navItems.forEach(item => {
        if (item.dataset.page === pageName) {
            item.classList.add('active');
        } else {
            item.classList.remove('active');
        }
    });

    // Update pages
    const pages = document.querySelectorAll('.page');
    pages.forEach(page => {
        const pageId = page.id.replace('page-', '');
        if (pageId === pageName) {
            page.classList.add('active');
        } else {
            page.classList.remove('active');
        }
    });

    // Load page content
    switch (pageName) {
        case 'home':
            loadDashboardStats();
            break;
        case 'events':
            loadEvents();
            break;
        case 'mybets':
            loadUserBets();
            break;
        case 'wallet':
            loadWalletDetails();
            break;
    }
}

/**
 * Load dashboard statistics
 */
async function loadDashboardStats() {
    const user = API.Auth.getCurrentUser();
    if (!user) return;

    try {
        // Get NFL events count
        const nflEvents = await API.Events.getEventsBySport('nfl', 'Scheduled');
        document.getElementById('nflCount').textContent = nflEvents.length;

        // Get NBA events count
        const nbaEvents = await API.Events.getEventsBySport('nba', 'Scheduled');
        document.getElementById('nbaCount').textContent = nbaEvents.length;

        // Get active bets count
        const activeBetsCount = await getActiveBetsCount();
        document.getElementById('activeBetsCount').textContent = activeBetsCount;

        // Wallet balance is already updated by auth module

    } catch (error) {
        console.error('Failed to load dashboard stats:', error);
    }
}

// Initialize app
document.addEventListener('DOMContentLoaded', () => {
    // Set up navigation
    const navItems = document.querySelectorAll('.nav-item');
    navItems.forEach(item => {
        item.addEventListener('click', (e) => {
            e.preventDefault();
            const pageName = item.dataset.page;
            navigateTo(pageName);
        });
    });

    console.log('✅ SportsBetting Frontend loaded');
    console.log('API Base URL:', 'http://localhost:5192/api');
});
