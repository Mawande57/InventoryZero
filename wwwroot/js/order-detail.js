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

function authHeaders() {
    return {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + getToken()
    };
}

function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

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

    try {
        const res = await fetch(`${API}/orders/${orderId}`, {
            headers: authHeaders()
        });

        if (!res.ok) {
            throw new Error('Order not found');
        }

        const order = await res.json();
        renderOrder(order);

    } catch (e) {
        console.error('Load order error:', e);
        document.querySelector('.order-detail-container').innerHTML = `
            <div style="text-align:center;padding:3rem;">
                <div style="font-size:48px;margin-bottom:1rem;">🔍</div>
                <h2 style="color:#991B1B;">Order Not Found</h2>
                <p style="color:#6b7280;">The order you're looking for doesn't exist or you don't have permission to view it.</p>
                <button onclick="window.location.href='/pages/buyer-dashboard.html'" 
                        style="margin-top:1rem;padding:10px 24px;background:#1D9E75;color:#fff;border:none;border-radius:10px;cursor:pointer;">
                    Back to Dashboard
                </button>
            </div>
        `;
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
    const statuses = ['Pending', 'Processing', 'Shipped', 'Delivered'];
    const currentIndex = statuses.indexOf(order.orderStatus);

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