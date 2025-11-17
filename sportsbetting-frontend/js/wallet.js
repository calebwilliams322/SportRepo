/**
 * WALLET MODULE
 * Handles wallet management and balance tracking
 */

/**
 * Load wallet details
 */
async function loadWalletDetails() {
    const user = API.Auth.getCurrentUser();
    if (!user) return;

    try {
        const wallet = await API.Wallets.getUserWallet(user.id);
        updateWalletDisplay(wallet);
    } catch (error) {
        console.error('Failed to load wallet:', error);
    }
}

/**
 * Deposit funds (if endpoint is available)
 */
async function depositFunds(amount) {
    const user = API.Auth.getCurrentUser();
    if (!user) return;

    try {
        const wallet = await API.Wallets.getUserWallet(user.id);
        await API.Wallets.deposit(wallet.id, amount);

        // Reload wallet
        await loadWalletDetails();

        alert(`Successfully deposited $${amount.toFixed(2)}`);

    } catch (error) {
        alert('Deposit failed: ' + error.message);
    }
}

/**
 * Withdraw funds (if endpoint is available)
 */
async function withdrawFunds(amount) {
    const user = API.Auth.getCurrentUser();
    if (!user) return;

    try {
        const wallet = await API.Wallets.getUserWallet(user.id);
        await API.Wallets.withdraw(wallet.id, amount);

        // Reload wallet
        await loadWalletDetails();

        alert(`Successfully withdrew $${amount.toFixed(2)}`);

    } catch (error) {
        alert('Withdrawal failed: ' + error.message);
    }
}
