/**
 * EVENTS MODULE
 * Handles browsing events, viewing odds, and placing bets
 */

let currentSport = 'nfl';
let currentStatus = 'Scheduled';
let selectedBet = null;

// Initialize events page
document.addEventListener('DOMContentLoaded', () => {
    // Sport tab switching
    const sportTabs = document.querySelectorAll('.sport-tab');
    sportTabs.forEach(tab => {
        tab.addEventListener('click', () => {
            currentSport = tab.dataset.sport;

            // Update active tab
            sportTabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');

            // Reload events
            loadEvents();
        });
    });

    // Status filter
    const statusFilter = document.getElementById('statusFilter');
    if (statusFilter) {
        statusFilter.addEventListener('change', (e) => {
            currentStatus = e.target.value;
            loadEvents();
        });
    }
});

/**
 * Load events for current sport and status
 */
async function loadEvents() {
    const eventsGrid = document.getElementById('eventsGrid');
    if (!eventsGrid) return;

    try {
        eventsGrid.innerHTML = '<div class="loading">Loading events...</div>';

        const events = await API.Events.getEventsBySport(currentSport, currentStatus);

        if (events.length === 0) {
            eventsGrid.innerHTML = `
                <div class="loading">
                    No ${currentSport.toUpperCase()} events found with status: ${currentStatus}
                </div>
            `;
            return;
        }

        eventsGrid.innerHTML = '';

        // Render each event as a playing card
        events.forEach(event => {
            const eventCard = createEventCard(event);
            eventsGrid.appendChild(eventCard);
        });

    } catch (error) {
        console.error('Failed to load events:', error);
        eventsGrid.innerHTML = `
            <div class="error-message">
                Failed to load events: ${error.message}
            </div>
        `;
    }
}

/**
 * Create an event card element (playing card style)
 */
function createEventCard(event) {
    const card = document.createElement('div');
    card.className = 'event-card';

    // Parse team names from event name
    const teams = event.name.split(' vs ');
    const awayTeam = teams[0] || 'Away Team';
    const homeTeam = teams[1] || 'Home Team';

    // Find moneyline market
    const moneylineMarket = event.markets?.find(m => m.type === 'Moneyline');

    // Format event time
    const eventTime = new Date(event.scheduledStartTime).toLocaleString('en-US', {
        weekday: 'short',
        month: 'short',
        day: 'numeric',
        hour: 'numeric',
        minute: '2-digit'
    });

    // Determine league badge
    const leagueBadge = currentSport === 'nfl' ? '🏈 NFL' : '🏀 NBA';

    // Status badge styling
    const statusClass = event.status.toLowerCase();
    const statusBadge = event.status === 'Scheduled' ? 'Upcoming' :
                       event.status === 'InProgress' ? 'Live' :
                       event.status;

    // Parse score if available (format: "home-away" e.g., "94-126")
    let homeScore = null;
    let awayScore = null;
    if (event.finalScore && event.finalScore !== 'null') {
        const scores = event.finalScore.split('-');
        if (scores.length === 2) {
            homeScore = scores[0].trim();
            awayScore = scores[1].trim();
        }
    }

    // Show score for live/completed games, odds for upcoming games
    const showScore = (event.status === 'InProgress' || event.status === 'Completed') && event.finalScore;

    card.innerHTML = `
        <div class="event-header">
            <div class="event-league">${leagueBadge}</div>
            <div class="event-status ${statusClass}">${statusBadge}</div>
            <div class="event-time">${eventTime}</div>
        </div>

        <div class="event-matchup">
            <div class="team" data-team="away">
                <div class="team-name">${awayTeam}</div>
                ${showScore ? `
                    <div class="team-score">${awayScore}</div>
                ` : `
                    <div class="team-odds">
                        <span class="odds-label">Odds</span>
                        <span class="odds-value" id="odds-away-${event.id}">-</span>
                    </div>
                `}
            </div>

            <div class="vs-divider">VS</div>

            <div class="team" data-team="home">
                <div class="team-name">${homeTeam}</div>
                ${showScore ? `
                    <div class="team-score">${homeScore}</div>
                ` : `
                    <div class="team-odds">
                        <span class="odds-label">Odds</span>
                        <span class="odds-value" id="odds-home-${event.id}">-</span>
                    </div>
                `}
            </div>
        </div>

        <button class="bet-button" onclick="viewBettingOptions('${event.id}', '${moneylineMarket?.id || ''}')">
            View Betting Options
        </button>
    `;

    // Load odds only for upcoming games (not for live/completed with scores)
    if (moneylineMarket && !showScore) {
        loadMarketOdds(event.id, moneylineMarket.id);
    }

    return card;
}

/**
 * Load odds for a specific market
 */
async function loadMarketOdds(eventId, marketId) {
    try {
        const event = await API.Events.getEventById(eventId);
        const market = await API.Events.getMarket(marketId);

        // Match outcomes to teams based on event team names
        market.outcomes.forEach(outcome => {
            const outcomeName = outcome.name.toLowerCase();
            const homeTeamName = event.homeTeamName.toLowerCase();
            const awayTeamName = event.awayTeamName.toLowerCase();

            // Check if this outcome matches the home or away team
            // Use includes() to handle partial matches (e.g., "Thunder" vs "Oklahoma City Thunder")
            if (outcomeName.includes(homeTeamName) || homeTeamName.includes(outcomeName.split(' ').pop())) {
                const homeOdds = document.getElementById(`odds-home-${eventId}`);
                if (homeOdds) {
                    homeOdds.textContent = outcome.currentOdds.toFixed(2);
                }
            } else if (outcomeName.includes(awayTeamName) || awayTeamName.includes(outcomeName.split(' ').pop())) {
                const awayOdds = document.getElementById(`odds-away-${eventId}`);
                if (awayOdds) {
                    awayOdds.textContent = outcome.currentOdds.toFixed(2);
                }
            }
        });

    } catch (error) {
        console.error('Failed to load market odds:', error);
    }
}

/**
 * View betting options for an event
 */
async function viewBettingOptions(eventId, marketId) {
    try {
        const market = await API.Events.getMarket(marketId);
        const event = await API.Events.getEventById(eventId);

        showBettingModal(event, market);

    } catch (error) {
        alert('Failed to load betting options: ' + error.message);
    }
}

/**
 * Show betting modal with all outcomes
 */
function showBettingModal(event, market) {
    const betSlip = document.getElementById('betSlip');
    const betSlipContent = document.getElementById('betSlipContent');

    // Format market type for display
    const marketTypeDisplay = market.type === 'Totals' ? 'Total Points' : market.type;

    betSlipContent.innerHTML = `
        <div>
            <h4>${event.name}</h4>
            <p style="color: rgba(255, 255, 255, 0.6); font-size: 0.9rem; margin: 0.5rem 0;">
                <strong>${marketTypeDisplay}</strong>
            </p>
        </div>

        <div style="margin: 1rem 0;">
            ${market.outcomes.map(outcome => {
                // Format outcome display with line if applicable
                let outcomeDisplay = outcome.name;
                if (outcome.line !== null && outcome.line !== undefined) {
                    const lineStr = outcome.line > 0 ? `+${outcome.line}` : outcome.line;
                    outcomeDisplay = `${outcome.name} (${lineStr})`;
                }

                // Store line info in data attribute
                return `
                    <div class="team" style="margin-bottom: 0.5rem; cursor: pointer;"
                         onclick="selectOutcome('${event.id}', '${market.id}', '${market.type}', '${outcome.id}', '${outcome.name}', ${outcome.currentOdds}, ${outcome.line || 'null'})">
                        <div class="team-name">${outcomeDisplay}</div>
                        <div class="odds-value">${outcome.currentOdds.toFixed(2)}</div>
                    </div>
                `;
            }).join('')}
        </div>
    `;

    betSlip.style.display = 'block';
}

/**
 * Select an outcome to bet on
 */
function selectOutcome(eventId, marketId, marketType, outcomeId, outcomeName, odds, line) {
    selectedBet = { eventId, marketId, outcomeId, outcomeName, odds, marketType, line };

    const betSlipContent = document.getElementById('betSlipContent');

    // Format bet description
    let betDescription = outcomeName;
    if (line !== null && line !== undefined) {
        const lineStr = line > 0 ? `+${line}` : line;
        betDescription = `${outcomeName} (${lineStr})`;
    }

    // Market type display
    const marketTypeDisplay = marketType === 'Totals' ? 'Total Points' : marketType;

    betSlipContent.innerHTML = `
        <div>
            <h4>Place Bet</h4>
            <div style="margin: 0.5rem 0; padding: 0.75rem; background: rgba(255, 255, 255, 0.05); border-radius: 0.5rem;">
                <div style="font-size: 0.85rem; color: rgba(255, 255, 255, 0.5); margin-bottom: 0.25rem;">
                    ${marketTypeDisplay}
                </div>
                <div style="font-size: 1rem; color: #fff; font-weight: 600;">
                    ${betDescription}
                </div>
                <div style="font-size: 0.9rem; color: var(--primary); margin-top: 0.25rem;">
                    Odds: ${odds.toFixed(2)}
                </div>
            </div>
        </div>

        <div>
            <label style="display: block; margin: 1rem 0 0.5rem 0; color: rgba(255, 255, 255, 0.8);">
                Stake Amount ($)
            </label>
            <input type="number" id="stakeAmount" placeholder="100.00" min="1" step="0.01">

            <div style="margin: 1rem 0; padding: 1rem; background: rgba(255, 255, 255, 0.05); border-radius: 0.5rem;">
                <div style="display: flex; justify-content: space-between; margin-bottom: 0.5rem;">
                    <span>Stake:</span>
                    <span id="displayStake">$0.00</span>
                </div>
                <div style="display: flex; justify-content: space-between; margin-bottom: 0.5rem;">
                    <span>Odds:</span>
                    <span>${odds.toFixed(2)}</span>
                </div>
                <div style="display: flex; justify-content: space-between; font-weight: 700; color: var(--primary); font-size: 1.1rem;">
                    <span>Potential Payout:</span>
                    <span id="potentialPayout">$0.00</span>
                </div>
            </div>

            <button class="btn-primary" onclick="confirmBet()" style="margin-top: 1rem;">
                Place Bet
            </button>
        </div>
    `;

    // Update payout calculation on stake input
    const stakeInput = document.getElementById('stakeAmount');
    stakeInput.addEventListener('input', (e) => {
        const stake = parseFloat(e.target.value) || 0;
        document.getElementById('displayStake').textContent = `$${stake.toFixed(2)}`;
        document.getElementById('potentialPayout').textContent = `$${(stake * odds).toFixed(2)}`;
    });
}

/**
 * Confirm and place the bet
 */
async function confirmBet() {
    if (!selectedBet) {
        alert('No bet selected');
        return;
    }

    const stake = parseFloat(document.getElementById('stakeAmount').value);

    if (!stake || stake <= 0) {
        alert('Please enter a valid stake amount');
        return;
    }

    try {
        const betData = {
            eventId: selectedBet.eventId,
            marketId: selectedBet.marketId,
            outcomeId: selectedBet.outcomeId,
            stake: stake
        };

        const result = await API.Bets.placeSingleBet(betData);

        console.log('Bet placed successfully:', result);

        // Show success modal
        showBetConfirmation(result);

        // Close bet slip
        closeBetSlip();

        // Reload wallet
        const user = API.Auth.getCurrentUser();
        const wallet = await API.Wallets.getUserWallet(user.id);
        updateWalletDisplay(wallet);

        // Reload dashboard stats
        loadDashboardStats();

    } catch (error) {
        alert('Failed to place bet: ' + error.message);
    }
}

/**
 * Show bet confirmation modal
 */
function showBetConfirmation(bet) {
    const modal = document.getElementById('betModal');
    const confirmation = document.getElementById('betConfirmation');

    confirmation.innerHTML = `
        <div style="margin: 2rem 0;">
            <div style="font-size: 1.2rem; margin-bottom: 1rem;">
                <strong>Ticket #:</strong> ${bet.ticketNumber}
            </div>
            <div style="margin-bottom: 0.5rem;">
                <strong>Stake:</strong> $${bet.stake.toFixed(2)}
            </div>
            <div style="margin-bottom: 0.5rem;">
                <strong>Odds:</strong> ${bet.combinedOdds.toFixed(2)}
            </div>
            <div style="font-size: 1.3rem; color: var(--primary); margin-top: 1rem;">
                <strong>Potential Payout:</strong> $${bet.potentialPayout.toFixed(2)}
            </div>
        </div>
    `;

    modal.style.display = 'flex';
}

/**
 * Close bet slip
 */
function closeBetSlip() {
    document.getElementById('betSlip').style.display = 'none';
    selectedBet = null;
}

/**
 * Close modal
 */
function closeModal() {
    document.getElementById('betModal').style.display = 'none';
}
