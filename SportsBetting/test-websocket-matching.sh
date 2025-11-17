#!/bin/bash

# Sports Betting Exchange - End-to-End WebSocket Test
# Tests bet matching with real-time WebSocket notifications

API_URL="http://localhost:5192"
echo "=========================================="
echo "WebSocket + Bet Matching E2E Test"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Step 1: Register Alice
echo -e "${BLUE}Step 1: Registering Alice...${NC}"
ALICE_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "alice_trader",
    "email": "alice@exchange.com",
    "password": "Alice123!",
    "currency": "USD"
  }')

ALICE_TOKEN=$(echo $ALICE_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$ALICE_TOKEN" ]; then
  echo -e "${RED}Failed to register Alice. Response:${NC}"
  echo $ALICE_RESPONSE
  exit 1
fi

echo -e "${GREEN}✓ Alice registered${NC}"
echo ""

# Step 2: Register Bob
echo -e "${BLUE}Step 2: Registering Bob...${NC}"
BOB_RESPONSE=$(curl -s -X POST "$API_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "bob_trader",
    "email": "bob@exchange.com",
    "password": "Bob123!",
    "currency": "USD"
  }')

BOB_TOKEN=$(echo $BOB_RESPONSE | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$BOB_TOKEN" ]; then
  echo -e "${RED}Failed to register Bob. Response:${NC}"
  echo $BOB_RESPONSE
  exit 1
fi

echo -e "${GREEN}✓ Bob registered${NC}"
echo ""

# Step 3: Create test event data
echo -e "${BLUE}Step 3: Creating test data (Event, Market, Outcome)...${NC}"

# You'll need to run these SQL commands or use an admin endpoint
# For now, we'll assume you have test data in the database
# Example outcome ID (replace with actual from your database):
OUTCOME_ID="replace-with-real-outcome-id-from-database"

echo -e "${YELLOW}⚠ You need to manually get an Outcome ID from your database${NC}"
echo -e "${YELLOW}⚠ Run this SQL query:${NC}"
echo "   SELECT o.\"Id\", o.\"Name\", e.\"Name\" as EventName"
echo "   FROM \"Outcomes\" o"
echo "   JOIN \"Markets\" m ON o.\"MarketId\" = m.\"Id\""
echo "   JOIN \"Events\" e ON m.\"EventId\" = e.\"Id\""
echo "   LIMIT 1;"
echo ""
read -p "Enter Outcome ID (GUID): " OUTCOME_ID

if [ -z "$OUTCOME_ID" ]; then
  echo -e "${RED}No Outcome ID provided. Exiting.${NC}"
  exit 1
fi

echo ""

# Step 4: Alice places a BACK bet
echo -e "${BLUE}Step 4: Alice places BACK bet (wants Lakers to win)${NC}"
echo "   Stake: \$100, Odds: 2.5"
echo ""

ALICE_BET=$(curl -s -X POST "$API_URL/api/bets/exchange" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ALICE_TOKEN" \
  -d "{
    \"outcomeId\": \"$OUTCOME_ID\",
    \"stake\": 100,
    \"odds\": 2.5,
    \"side\": \"Back\"
  }")

echo "$ALICE_BET" | jq '.'
ALICE_BET_ID=$(echo $ALICE_BET | grep -o '"id":"[^"]*' | head -1 | cut -d'"' -f4)

if [ -z "$ALICE_BET_ID" ]; then
  echo -e "${RED}Failed to place Alice's bet${NC}"
  exit 1
fi

echo -e "${GREEN}✓ Alice's BACK bet placed (should be UNMATCHED)${NC}"
echo ""
sleep 2

# Step 5: Check order book
echo -e "${BLUE}Step 5: Checking order book...${NC}"
curl -s "$API_URL/api/bets/orderbook/$OUTCOME_ID" | jq '.'
echo ""
sleep 2

# Step 6: Bob places a LAY bet at SAME odds
echo -e "${BLUE}Step 6: Bob places LAY bet at SAME odds (bet against Lakers)${NC}"
echo "   Stake: \$100, Odds: 2.5"
echo -e "${YELLOW}⚠ WATCH THE WEBSOCKET CLIENT - YOU SHOULD SEE LIVE UPDATES!${NC}"
echo ""

BOB_BET=$(curl -s -X POST "$API_URL/api/bets/exchange" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $BOB_TOKEN" \
  -d "{
    \"outcomeId\": \"$OUTCOME_ID\",
    \"stake\": 100,
    \"odds\": 2.5,
    \"side\": \"Lay\"
  }")

echo "$BOB_BET" | jq '.'
BOB_BET_ID=$(echo $BOB_BET | grep -o '"id":"[^"]*' | head -1 | cut -d'"' -f4)

if [ -z "$BOB_BET_ID" ]; then
  echo -e "${RED}Failed to place Bob's bet${NC}"
  exit 1
fi

echo -e "${GREEN}✓ Bob's LAY bet placed${NC}"
echo ""
sleep 2

# Step 7: Check order book again
echo -e "${BLUE}Step 7: Checking order book after match...${NC}"
curl -s "$API_URL/api/bets/orderbook/$OUTCOME_ID" | jq '.'
echo ""

# Step 8: Check Alice's bet status
echo -e "${BLUE}Step 8: Checking Alice's bet status...${NC}"
curl -s "$API_URL/api/bets/user" \
  -H "Authorization: Bearer $ALICE_TOKEN" | jq '.'
echo ""

# Step 9: Check Bob's bet status
echo -e "${BLUE}Step 9: Checking Bob's bet status...${NC}"
curl -s "$API_URL/api/bets/user" \
  -H "Authorization: Bearer $BOB_TOKEN" | jq '.'
echo ""

echo "=========================================="
echo -e "${GREEN}Test Complete!${NC}"
echo "=========================================="
echo ""
echo "Expected Results:"
echo "1. Alice's BACK bet should be MATCHED"
echo "2. Bob's LAY bet should be MATCHED"
echo "3. Order book should be EMPTY (no unmatched bets)"
echo "4. WebSocket client should have shown:"
echo "   - OrderBookUpdate when Alice placed bet"
echo "   - BetMatched notification to Alice"
echo "   - BetMatched notification to Bob"
echo "   - OrderBookUpdate showing empty book"
echo ""
echo -e "${YELLOW}Check the SignalR client browser window for real-time updates!${NC}"
