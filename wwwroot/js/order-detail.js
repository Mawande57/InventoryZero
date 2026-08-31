const API = 'https://localhost:7237/api';

// ── AUTH ──────────────────────────────────────────────────
function getToken() { return localStorage.getItem('token'); }
function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

if (!getToken()) {
    window.location.href = '/pages/login.html';
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/pages/login.html';
}

function authHeaders() {
    return {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + getToken()
    };
}

function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

// Distinguishes "your connection looks offline" from "our server didn't
// respond" - used only for genuine connectivity failures, not for a real
// 404 (that's a different, more specific message below).
function getConnectionMessage() {
    if (typeof navigator !== 'undefined' && navigator.onLine === false) {
        return "You appear to be offline. Check your internet connection and try again.";
    }
    return "We're having trouble reaching the server right now. This is usually temporary — please try again in a moment.";
}

// message is always one of our own fixed strings, never server/user data, so
// no escaping is needed here - nothing in this file goes into innerHTML from
// an external source.
function showLoadError(message, showRetry) {
    document.querySelector('.order-detail-container').innerHTML = `
        <div style="text-align:center;padding:3rem;">
            <div style="font-size:48px;margin-bottom:1rem;">🔍</div>
            <h2 style="color:#991B1B;">Order Not Found</h2>
            <p style="color:#6b7280;">${message}</p>
            <div style="margin-top:1rem;display:flex;gap:10px;justify-content:center;">
                ${showRetry ? `<button onclick="loadOrder()" style="padding:10px 24px;background:#1D9E75;color:#fff;border:none;border-radius:10px;cursor:pointer;">Try again</button>` : ''}
                <button onclick="window.location.href='/pages/buyer-dashboard.html'" 
                        style="padding:10px 24px;background:${showRetry ? '#e5e5e5' : '#1D9E75'};color:${showRetry ? '#333' : '#fff'};border:none;border-radius:10px;cursor:pointer;">
                    Back to Dashboard
                </button>
            </div>
        </div>
    `;
}

// ── GET ORDER ID FROM URL ──────────────────────────────
function getOrderId() {
    const params = new URLSearchParams(window.location.search);
    return params.get('id');
}

// ── LOAD ORDER ───────────────────────────────────────────
async function loadOrder() {
    const orderId = getOrderId();
    if (!orderId) {
        window.location.href = '/pages/buyer-dashboard.html';
        return;
    }

    let res;
    try {
        res = await fetch(`${API}/orders/${orderId}`, { headers: authHeaders() });
    } catch (networkErr) {
        // fetch() itself only throws for a genuine network failure - offline,
        // DNS failure, server unreachable. That's different from "this order
        // doesn't exist," and deserves a different message and a retry option.
        console.error('Load order network error:', networkErr);
        showLoadError(getConnectionMessage(), true);
        return;
    }

    if (res.status === 401 || res.status === 403) {
        logout();
        return;
    }

    if (!res.ok) {
        // A real 404/403 response - the order genuinely doesn't exist or
        // isn't this buyer's. Retrying won't change that, so no retry button.
        showLoadError("The order you're looking for doesn't exist or you don't have permission to view it.", false);
        return;
    }

    try {
        const order = await res.json();
        renderOrder(order);
    } catch (e) {
        console.error('Could not read order response:', e);
        showLoadError(getConnectionMessage(), true);
    }
}

// ── RENDER ORDER ─────────────────────────────────────────
function renderOrder(order) {
    // Header
    document.getElementById('order-number').textContent = order.orderNumber;
    document.getElementById('order-date').textContent = `Placed on ${new Date(order.createdAt).toLocaleDateString('en-ZA', {
        day: 'numeric', month: 'long', year: 'numeric',
        hour: '2-digit', minute: '2-digit'
    })}`;

    // Status badge
    const badge = document.getElementById('order-status-badge');
    badge.className = `order-status-badge status-${order.orderStatus.toLowerCase()}`;
    document.getElementById('order-status').textContent = order.orderStatus;

    // Product
    document.getElementById('order-product-title').textContent = order.productTitle;
    document.getElementById('order-product-shop').textContent = `Shop: ${order.shopName}`;
    document.getElementById('order-unit-price').textContent = fmt(order.unitPrice);
    document.getElementById('order-quantity').textContent = `× ${order.quantity}`;
    document.getElementById('order-total').textContent = fmt(order.totalAmount);

    // Shipping
    // NOTE: order.shippingRecipientName is always undefined - OrderDetailDto
    // has no such field, and neither the Order model nor PlaceOrderDto ever
    // capture a recipient name at order-placement time (not even when a
    // saved address with its own RecipientName is used). This will always
    // fall through to the '—' placeholder until that's added on the backend
    // (Order model + PlaceOrderDto + OrderService + checkout.js would all
    // need to carry it through). Not something fixable from this file alone.
    document.getElementById('order-shipping-name').textContent = order.shippingRecipientName || '—';
    document.getElementById('order-shipping-address').textContent =
        `${order.shippingAddressLine1}${order.shippingAddressLine2 ? ', ' + order.shippingAddressLine2 : ''}, ${order.shippingCity}, ${order.shippingProvince}, ${order.shippingPostalCode}`;
    document.getElementById('order-shipping-phone').textContent = `Phone: ${order.shippingPhoneNumber || '—'}`;

    // Summary
    document.getElementById('summary-subtotal').textContent = fmt(order.subtotal || order.totalAmount);
    document.getElementById('summary-shipping').textContent = fmt(order.shippingCost || 0);
    document.getElementById('summary-fee').textContent = fmt(order.platformFee || 0);
    document.getElementById('summary-total').textContent = fmt(order.totalAmount);

    // Payment
    document.getElementById('payment-status').textContent = order.paymentStatus || 'Pending';
    document.getElementById('payment-method').textContent = order.paymentMethod || '—';

    // ─── UPDATE TRACKING TIMELINE ──────────────────────────
    updateTimeline(order);
}

// ─── UPDATE TRACKING TIMELINE ─────────────────────────────
function updateTimeline(order) {
    const steps = document.querySelectorAll('.timeline-step');

    // Map order status to step index (0-based)
    const stepMap = {
        'Pending': 0,
        'Processing': 1,
        'Shipped': 2,
        'Delivered': 3,
        'Cancelled': -1
    };

    const activeIndex = stepMap[order.orderStatus];

    // Set dates
    document.getElementById('step-placed-date').textContent =
        new Date(order.createdAt).toLocaleDateString('en-ZA', { day: 'numeric', month: 'short', year: 'numeric' });

    if (order.paidAt) {
        document.getElementById('step-processing-date').textContent =
            new Date(order.paidAt).toLocaleDateString('en-ZA', { day: 'numeric', month: 'short', year: 'numeric' });
    } else if (order.createdAt) {
        // Approximate processing date (1 day after order)
        const processingDate = new Date(order.createdAt);
        processingDate.setDate(processingDate.getDate() + 1);
        document.getElementById('step-processing-date').textContent =
            processingDate.toLocaleDateString('en-ZA', { day: 'numeric', month: 'short', year: 'numeric' }) + ' (estimated)';
    }

    if (order.shippedAt) {
        document.getElementById('step-shipped-date').textContent =
            new Date(order.shippedAt).toLocaleDateString('en-ZA', { day: 'numeric', month: 'short', year: 'numeric' });
    }

    if (order.deliveredAt) {
        document.getElementById('step-delivered-date').textContent =
            new Date(order.deliveredAt).toLocaleDateString('en-ZA', { day: 'numeric', month: 'short', year: 'numeric' });
    }

    // Tracking number
    if (order.trackingNumber) {
        document.getElementById('step-tracking-number').textContent = `📦 Tracking: ${order.trackingNumber}`;
    } else {
        document.getElementById('step-tracking-number').textContent = 'Tracking not available yet';
    }

    // Update step statuses
    steps.forEach((step, index) => {
        step.classList.remove('active', 'completed');

        if (index < activeIndex) {
            step.classList.add('completed');
        } else if (index === activeIndex && activeIndex >= 0) {
            step.classList.add('active');
        }
    });

    // If cancelled
    if (order.orderStatus === 'Cancelled') {
        steps.forEach(step => {
            step.classList.remove('active', 'completed');
        });
        // Add a cancelled badge
        const timeline = document.getElementById('tracking-timeline');
        const cancelMsg = document.createElement('div');
        cancelMsg.style.cssText = `
            position: absolute;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%);
            background: #FEE2E2;
            color: #991B1B;
            padding: 8px 20px;
            border-radius: 999px;
            font-weight: 600;
            font-size: 14px;
        `;
        cancelMsg.textContent = '❌ Order Cancelled';
        timeline.style.position = 'relative';
        timeline.appendChild(cancelMsg);
    }
}

// ─── INIT ──────────────────────────────────────────────────
loadOrder();