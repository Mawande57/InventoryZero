//const API = 'https://localhost:7237/api';
const API = '/api';
const EMOJIS = {
    'Clothing': '🧥', 'Electronics': '📱',
    'Food & Drinks': '🥤', 'Furniture': '🛋️',
    'Hardware': '🔧', 'Sport & Fitness': '🏋️',
    'Beauty & Health': '💄', 'Other': '📦'
};

const CAT_COLORS = {
    'Clothing': '#E1F5EE', 'Electronics': '#E6F1FB',
    'Food & Drinks': '#FAECE7', 'Furniture': '#FAEEDA',
    'Hardware': '#EEEDFE', 'Sport & Fitness': '#EAF3DE',
    'Beauty & Health': '#FBEAF0', 'Other': '#F1EFE8'
};

// ── AUTH ─────────────────────────────────────────────────
function getToken() { return localStorage.getItem('token'); }
if (!getToken()) window.location.href = '/pages/login.html';

function authHeaders() {
    return {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + getToken()
    };
}

function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

// Get orderId from URL
const params = new URLSearchParams(window.location.search);
const orderId = params.get('orderId');
const orderNumber = params.get('orderNumber');
const amount = params.get('amount');

if (!orderId) window.location.href = '/pages/browse.html';

// ── LOAD ORDER SUMMARY ───────────────────────────────────
async function loadOrderSummary() {
    try {
        const res = await fetch(`${API}/orders/${orderId}`, { headers: authHeaders() });
        const order = await res.json();

        document.getElementById('pay-amount').textContent = fmt(order.totalAmount);
        document.getElementById('pay-order-num').textContent = order.orderNumber;
        document.getElementById('pay-btn-amount').textContent = fmt(order.totalAmount);

        document.getElementById('order-summary').innerHTML = `
      <div class="summary-row">
        <div class="summary-img" style="background:${CAT_COLORS[order.categoryName] || '#f5f5f3'}">
          ${EMOJIS[order.categoryName] || '📦'}
        </div>
        <div>
          <div class="summary-title">${order.productTitle}</div>
          <div class="summary-shop">${order.shopName} · Qty: ${order.quantity}</div>
        </div>
        <div class="summary-price">${fmt(order.totalAmount)}</div>
      </div>`;

    } catch (e) {
        console.error('Could not load order', e);
    }
}

// ── CARD FORMATTING ──────────────────────────────────────
function formatCard(input) {
    let v = input.value.replace(/\D/g, '').substring(0, 16);
    input.value = v.replace(/(.{4})/g, '$1 ').trim();
}

function formatExpiry(input) {
    let v = input.value.replace(/\D/g, '').substring(0, 4);
    if (v.length >= 2) v = v.substring(0, 2) + '/' + v.substring(2);
    input.value = v;
}

// ── VALIDATE CARD ─────────────────────────────────────────
function validateCard() {
    const name = document.getElementById('card-name').value.trim();
    const number = document.getElementById('card-number').value.replace(/\s/g, '');
    const expiry = document.getElementById('card-expiry').value;
    const cvv = document.getElementById('card-cvv').value;

    if (!name) return 'Please enter the cardholder name.';
    if (number.length < 16) return 'Please enter a valid 16-digit card number.';
    if (expiry.length < 5) return 'Please enter a valid expiry date (MM/YY).';
    if (cvv.length < 3) return 'Please enter a valid CVV.';

    // Check expiry not in past
    const [month, year] = expiry.split('/');
    const expDate = new Date(2000 + parseInt(year), parseInt(month) - 1);
    if (expDate < new Date()) return 'Your card has expired.';

    return null;
}

// ── PROCESS PAYMENT ──────────────────────────────────────
async function processPayment() {
    const errEl = document.getElementById('pay-err');
    const btn = document.getElementById('pay-btn');

    errEl.classList.remove('show');

    // Validate card
    const error = validateCard();
    if (error) {
        errEl.textContent = error;
        errEl.classList.add('show');
        return;
    }

    // Prevent double click
    if (btn.disabled) return;
    btn.disabled = true;

    // Show processing overlay
    document.getElementById('processing-overlay').classList.add('show');

    try {
        // Simulate processing delay — makes it feel real
        await new Promise(resolve => setTimeout(resolve, 2500));

        // Call API to mark order as paid
        const res = await fetch(`${API}/payments/process/${orderId}`, {
            method: 'POST',
            headers: authHeaders()
        });

        const data = await res.json();

        if (!res.ok) {
            document.getElementById('processing-overlay').classList.remove('show');
            errEl.textContent = data.message || 'Payment failed. Please try again.';
            errEl.classList.add('show');
            btn.disabled = false;
            return;
        }

        // Success — redirect to success page
        window.location.href = `/pages/order-success.html?order=${data.orderNumber}`;

    } catch (e) {
        document.getElementById('processing-overlay').classList.remove('show');
        errEl.textContent = 'Network error. Please check your connection and try again.';
        errEl.classList.add('show');
        btn.disabled = false;
    }
}

loadOrderSummary();