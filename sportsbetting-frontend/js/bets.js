/**
 * BETS MODULE
 * Handles viewing and managing user's bets
 */

let currentBetFilter = 'all';

// Initialize bets page
document.addEventListener('DOMContentLoaded', () => {
    // Bet status tabs
    const betTabs = document.querySelectorAll('.bet-tab');
    betTabs.forEach(tab => {
        tab.addEventListener('click', () => {
            currentBetFilter = tab.dataset.status;

            // Update active tab
            betTabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');

            // Reload bets
            loadUserBets();
        });
    });
});

/**
 * Load user's bets
 */
async function loadUserBets() {
    const betsContainer = document.getElementById('betsContainer');
    if (!betsContainer) return;

    const user = API.Auth.getCurrentUser();
    if (!user) {
        betsContainer.innerHTML = '<div class="loading">Please log in to view bets</div>';
        return;
    }

    try {
        betsContainer.innerHTML = '<div class="loading">Loading bets...</div>';

        // Get bets with optional status filter
        const params = currentBetFilter !== 'all' ? { status: currentBetFilter } : {};
        const bets = await API.Bets.getUserBets(user.id, params);

        if (bets.length === 0) {
            betsContainer.innerHTML = `
                <div class="loading">
                    No bets found${currentBetFilter !== 'all' ? ' with status: ' + currentBetFilter : ''}
                </div>
            `;
            return;
        }

        betsContainer.innerHTML = '';

        // Render each bet as a card
        bets.forEach(bet => {
            const betCard = createBetCard(bet);
            betsContainer.appendChild(betCard);
        });

    } catch (error) {
        console.error('Failed to load bets:', error);

        // Check if it's an unauthorized error
        if (error.message.includes('401') || error.message.toLowerCase().includes('unauthorized')) {
            betsContainer.innerHTML = '<div class="loading">Please log in to view bets</div>';
            alert('Please sign in to continue');
            navigateTo('login');
            return;
        }

        betsContainer.innerHTML = `
            <div class="error-message">
                Failed to load bets: ${error.message}
            </div>
        `;
    }
}

/**
 * Create a bet card element
 */
function createBetCard(bet) {
    const card = document.createElement('div');
    card.className = 'bet-card';

    const placedAt = new Date(bet.placedAt).toLocaleString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit'
    });

    const settledAt = bet.settledAt ? new Date(bet.settledAt).toLocaleString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit'
    }) : null;

    // Status badge class
    const statusClass = bet.status.toLowerCase();

    card.innerHTML = `
        <div class="bet-card-header">
            <div>
                <div class="bet-ticket">Ticket: ${bet.ticketNumber}</div>
                <div style="font-size: 0.85rem; color: rgba(255, 255, 255, 0.5); margin-top: 0.25rem;">
                    ${bet.type} Bet • Placed ${placedAt}
                </div>
            </div>
            <div class="bet-status ${statusClass}">${bet.status}</div>
        </div>

        <div class="bet-selections">
            ${bet.selections.map(selection => `
                <div class="bet-selection">
                    <div class="selection-event">${selection.eventName}</div>
                    <div class="selection-outcome">
                        ${selection.marketName}: <strong>${selection.outcomeName}</strong> @ ${selection.lockedOdds.toFixed(2)}
                        ${selection.line ? ` (${selection.line > 0 ? '+' : ''}${selection.line})` : ''}
                    </div>
                    ${selection.result !== 'Pending' ? `
                        <div style="margin-top: 0.5rem; font-size: 0.85rem; color: ${selection.result === 'Won' ? 'var(--primary)' : '#ef4444'};">
                            Result: <strong>${selection.result}</strong>
                        </div>
                    ` : ''}
                </div>
            `).join('')}
        </div>

        <div class="bet-amounts">
            <div class="bet-amount">
                <div class="bet-amount-label">Stake</div>
                <div class="bet-amount-value">$${bet.stake.toFixed(2)}</div>
            </div>
            <div class="bet-amount">
                <div class="bet-amount-label">Odds</div>
                <div class="bet-amount-value">${bet.combinedOdds.toFixed(2)}</div>
            </div>
            <div class="bet-amount">
                <div class="bet-amount-label">${bet.status === 'Won' ? 'Payout' : 'Potential'}</div>
                <div class="bet-amount-value ${bet.status === 'Won' ? 'green' : ''}">
                    $${(bet.actualPayout || bet.potentialPayout).toFixed(2)}
                </div>
            </div>
        </div>

        ${settledAt ? `
            <div style="margin-top: 1rem; padding-top: 1rem; border-top: 1px solid rgba(255, 255, 255, 0.1); font-size: 0.85rem; color: rgba(255, 255, 255, 0.5);">
                Settled: ${settledAt}
            </div>
        ` : ''}
    `;

    return card;
}

/**
 * Get user's active bets count (for dashboard)
 */
async function getActiveBetsCount() {
    const user = API.Auth.getCurrentUser();
    if (!user) return 0;

    try {
        const bets = await API.Bets.getUserBets(user.id, { status: 'Pending' });
        return bets.length;
    } catch (error) {
        console.error('Failed to get active bets count:', error);
        return 0;
    }
}
