/**
 * DEPOSIT MODULE
 * Handles depositing funds into user wallet
 */

/**
 * Show deposit modal
 */
function showDepositModal() {
    const modal = document.getElementById('depositModal');
    modal.style.display = 'flex';

    // Clear previous input
    document.getElementById('depositAmount').value = '';
}

/**
 * Close deposit modal
 */
function closeDepositModal() {
    document.getElementById('depositModal').style.display = 'none';
}

/**
 * Confirm deposit
 */
async function confirmDeposit() {
    const amount = parseFloat(document.getElementById('depositAmount').value);

    if (!amount || amount <= 0) {
        alert('Please enter a valid deposit amount');
        return;
    }

    const user = API.Auth.getCurrentUser();
    if (!user) {
        alert('Please log in first');
        return;
    }

    try {
        showLoading('Processing deposit...');

        // Get current wallet
        const wallet = await API.Wallets.getUserWallet(user.id);

        // Try to use deposit endpoint (if it exists)
        try {
            await API.Wallets.deposit(wallet.id, amount);

            hideLoading();
            closeDepositModal();

            // Reload wallet
            await loadWalletDetails();

            alert(`Successfully deposited $${amount.toFixed(2)}!`);

        } catch (depositError) {
            // Deposit endpoint doesn't exist - use manual workaround
            console.warn('Deposit endpoint not available:', depositError);

            hideLoading();
            closeDepositModal();

            // Show manual instructions
            showManualDepositInstructions(user.id, wallet.id, amount);
        }

    } catch (error) {
        hideLoading();
        alert('Failed to process deposit: ' + error.message);
    }
}

/**
 * Show manual deposit instructions (fallback)
 */
function showManualDepositInstructions(userId, walletId, amount) {
    const currentBalance = parseFloat(document.getElementById('walletBalanceDetail').textContent.replace('$', ''));
    const newBalance = currentBalance + amount;

    const instructions = `
To deposit $${amount.toFixed(2)}, run this SQL command:

psql -U calebwilliams -d sportsbetting -c "
UPDATE \\"Wallets\\"
SET
  \\"Balance\\" = ${newBalance.toFixed(2)},
  \\"TotalDeposited\\" = \\"TotalDeposited\\" + ${amount.toFixed(2)},
  \\"LastUpdatedAt\\" = NOW()
WHERE \\"Id\\" = '${walletId}';
"

Then refresh the page to see the updated balance.
    `;

    console.log(instructions);

    alert('Deposit endpoint not available. Manual deposit required.\n\nSQL command has been logged to console.\n\nPress F12 → Console to see the command.');
}

/**
 * Show loading message
 */
function showLoading(message) {
    console.log('Loading:', message);
}

/**
 * Hide loading message
 */
function hideLoading() {
    console.log('Loading complete');
}
