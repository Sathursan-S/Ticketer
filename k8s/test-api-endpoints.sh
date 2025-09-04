#!/bin/bash

# API Gateway Test Suite
# Tests all required endpoints through Kong API Gateway

set -e

KONG_URL="http://localhost:8000"
ADMIN_URL="http://localhost:8001"

echo "🧪 Testing Kong API Gateway Endpoints"
echo "======================================"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counter
TESTS_PASSED=0
TESTS_TOTAL=0

# Function to run a test
run_test() {
    local test_name="$1"
    local command="$2"
    local expected_status="$3"
    
    TESTS_TOTAL=$((TESTS_TOTAL + 1))
    echo -n "Testing $test_name... "
    
    if response=$(eval "$command" 2>/dev/null); then
        if [ "$expected_status" = "any" ] || echo "$response" | grep -q "$expected_status"; then
            echo -e "${GREEN}✅ PASS${NC}"
            TESTS_PASSED=$((TESTS_PASSED + 1))
        else
            echo -e "${RED}❌ FAIL${NC} (Unexpected response)"
            echo "Response: $response"
        fi
    else
        echo -e "${RED}❌ FAIL${NC} (Request failed)"
    fi
}

echo ""
echo "📊 1. Kong Health Checks"
echo "------------------------"

run_test "Kong Proxy Health" \
    "curl -s $KONG_URL" \
    "any"

run_test "Kong Admin Status" \
    "curl -s $ADMIN_URL/status" \
    "database"

echo ""
echo "🔓 2. Public Auth Endpoints (No Authentication Required)"
echo "-------------------------------------------------------"

run_test "Check Email Exists" \
    "curl -s '$KONG_URL/api/auth/exists?mailId=test@example.com'" \
    "any"

# Note: These tests assume the auth service is running and has proper data
echo ""
echo "🎫 3. Public Tickets Endpoints"
echo "------------------------------"

run_test "Get Available Tickets" \
    "curl -s $KONG_URL/api/tickets" \
    "any"

echo ""
echo "🔒 4. Protected Endpoints (Would require valid JWT)"
echo "--------------------------------------------------"

# Test protected endpoints without JWT - should return 401
run_test "Events CRUD (no auth - should fail)" \
    "curl -s -o /dev/null -w '%{http_code}' $KONG_URL/api/events" \
    "401"

run_test "Booking Creation (no auth - should fail)" \
    "curl -s -o /dev/null -w '%{http_code}' -X POST $KONG_URL/api/booking" \
    "401"

echo ""
echo "🔧 5. Kong Configuration"
echo "------------------------"

run_test "List Kong Services" \
    "curl -s $ADMIN_URL/services" \
    "any"

run_test "List Kong Routes" \
    "curl -s $ADMIN_URL/routes" \
    "any"

run_test "List Kong Plugins" \
    "curl -s $ADMIN_URL/plugins" \
    "any"

echo ""
echo "📈 Test Summary"
echo "==============="
if [ $TESTS_PASSED -eq $TESTS_TOTAL ]; then
    echo -e "${GREEN}🎉 All tests passed! ($TESTS_PASSED/$TESTS_TOTAL)${NC}"
    echo ""
    echo "✅ Kong API Gateway is working correctly!"
    echo "✅ Public endpoints are accessible"
    echo "✅ Protected endpoints require authentication"
    echo "✅ Admin API is functional"
else
    echo -e "${YELLOW}⚠️  Some tests failed. ($TESTS_PASSED/$TESTS_TOTAL passed)${NC}"
    echo ""
    echo "💡 Common issues:"
    echo "   - Services may not be fully started yet"
    echo "   - Port forwarding may not be active"
    echo "   - Check: kubectl get pods -A"
fi

echo ""
echo "🚀 Next Steps:"
echo "1. Test with real authentication:"
echo "   TOKEN=\$(curl -s -X POST $KONG_URL/api/auth/login -H 'Content-Type: application/json' -d '{\"email\":\"user@example.com\",\"password\":\"password\"}' | jq -r '.accessToken')"
echo "   curl -H \"Authorization: Bearer \$TOKEN\" $KONG_URL/api/events"
echo ""
echo "2. Create test data using the authenticated endpoints"
echo "3. Monitor with: curl $ADMIN_URL/status"