// wwwroot/js/admin-dashboard.js
const API = 'https://localhost:7237/api';

// ── AUTH ──────────────────────────────────────────────────
function getToken() { return localStorage.getItem('token'); }

function getUser() {
    const raw = localStorage.getItem('user');
    if (!raw) return null;
    // Corrupted/tampered localStorage shouldn't crash the whole page on load -
    // treat it the same as "not logged in" and let the redirect below handle it.
    try {
        return JSON.parse(raw);
    } catch {
        return null;
    }
}

// Redirect if not admin
const user = getUser();
if (!user || user.role !== 'Admin') {
    window.location.href = '/pages/login.html';
}

function authHeaders() {
    return {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + getToken()
    };
}

function logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.location.href = '/pages/login.html';
}

// ── API HELPER ───────────────────────────────────────────
// Every fetch in this file used to repeat the same headers/res.ok/try-catch
// boilerplate, and several of the read endpoints (loadDashboard, loadShops,
// etc.) skipped the res.ok check entirely - fetch() only rejects on a network
// failure, not on a 401/400/500, so those were silently trying to render an
// error response body as if it were real data. This centralizes that: throws
// a normal Error with the server's message on any non-OK response, and treats
// 401/403 as "your session is over" rather than just another error to log.
async function apiRequest(path, options = {}) {
    const res = await fetch(`${API}${path}`, {
        ...options,
        headers: { ...authHeaders(), ...(options.headers || {}) }
    });

    if (res.status === 401 || res.status === 403) {
        logout();
        throw new Error('Session expired. Please log in again.');
    }

    let data = null;
    try {
        data = await res.json();
    } catch {
        data = null; // e.g. a 204 No Content response has no body to parse
    }

    if (!res.ok) {
        throw new Error(data?.message || `Request failed (${res.status}).`);
    }

    return data;
}

// ── HELPERS ──────────────────────────────────────────────
function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

// Anything rendered via innerHTML has to go through this first. Shop names,
// owner names, descriptions, notes etc. all ultimately come from data other
// users typed in (a seller sets their own shop name/description), so
// rendering them as raw HTML would let a malicious value execute script in
// whoever's browser views this page - and since that's the admin dashboard
// with a JWT sitting in localStorage, that's a direct path to full admin
// account takeover, not just a cosmetic glitch.
function escapeHtml(value) {
    if (value === null || value === undefined) return '';
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

function closeModal(id) {
    document.getElementById(id).classList.remove('open');
}

function showModal(id) {
    document.getElementById(id).classList.add('open');
}

// ── TAB SWITCHING ────────────────────────────────────────
function switchTab(tab) {
    document.querySelectorAll('.tab-content').forEach(el => el.classList.remove('active'));
    document.querySelectorAll('.tab-btn').forEach(el => el.classList.remove('active'));
    document.getElementById('tab-' + tab).classList.add('active');
    document.querySelector(`.tab-btn[data-tab="${tab}"]`).classList.add('active');

    // Load data for tab
    if (tab === 'dashboard') loadDashboard();
    if (tab === 'shops') loadShops();
    if (tab === 'users') loadUsers();
    if (tab === 'products') loadProducts();
    if (tab === 'orders') loadAdminOrders();
    if (tab === 'payouts') loadPayouts();
}

// ── LOAD DASHBOARD ───────────────────────────────────────
async function loadDashboard() {
    try {
        const stats = await apiRequest('/admin/stats');

        // Update stats
        document.querySelector('#quick-stats .qs-card:nth-child(1) .qs-num').textContent = stats.totalOrders;
        document.querySelector('#quick-stats .qs-card:nth-child(2) .qs-num').textContent = fmt(stats.platformFees);
        document.querySelector('#quick-stats .qs-card:nth-child(3) .qs-num').textContent = fmt(stats.pendingPayouts);
        document.querySelector('#quick-stats .qs-card:nth-child(4) .qs-num').textContent = stats.disputesOpen;

        // Update hero stats
        document.querySelector('#stats-grid .dh-stat:nth-child(1) .n').textContent = stats.totalUsers;
        document.querySelector('#stats-grid .dh-stat:nth-child(2) .n').textContent = stats.totalSellers;
        document.querySelector('#stats-grid .dh-stat:nth-child(3) .n').textContent = stats.totalShops;
        document.querySelector('#stats-grid .dh-stat:nth-child(4) .n').textContent = fmt(stats.totalRevenue);

        // Update pending badge
        document.getElementById('pending-badge').textContent = stats.pendingShops;

        // Load recent orders
        loadRecentOrders();

    } catch (e) {
        console.error('Dashboard error:', e);
    }
}

async function loadRecentOrders() {
    try {
        const data = await apiRequest('/admin/orders?page=1&pageSize=5');
        const items = data?.items ?? [];

        const container = document.getElementById('recent-orders');
        if (items.length === 0) {
            container.innerHTML = '<p style="color:#6b7280;font-size:14px">No orders yet.</p>';
            return;
        }

        container.innerHTML = items.map(o => `
            <div class="order-item" style="padding:0.5rem 0.75rem;margin-bottom:2px">
                <span class="order-number">#${escapeHtml(o.orderNumber)}</span>
                <div class="order-info">
                    <div class="order-buyer">${escapeHtml(o.buyerName)}</div>
                    <div class="order-shop">${escapeHtml(o.shopName)}</div>
                </div>
                <div class="order-amount">${fmt(o.totalAmount)}</div>
                <span class="status-badge status-${escapeHtml(o.orderStatus.toLowerCase())}">${escapeHtml(o.orderStatus)}</span>
            </div>
        `).join('');

    } catch (e) {
        console.error('Recent orders error:', e);
    }
}

// ── SHOPS ────────────────────────────────────────────────
let shopsPage = 1;
let shopsTotalPages = 1;

async function loadShops(page = 1) {
    shopsPage = page;
    const status = document.getElementById('shop-status-filter').value;
    const list = document.getElementById('shops-list');
    list.innerHTML = '<div class="shop-skeleton"></div>'.repeat(3);

    try {
        const data = await apiRequest(`/admin/shops?status=${encodeURIComponent(status)}&page=${page}&pageSize=10`);
        const items = data?.items ?? [];
        shopsTotalPages = data.totalPages;

        if (items.length === 0) {
            list.innerHTML = '<p style="color:#6b7280;padding:1rem;text-align:center">No shops found.</p>';
            renderPagination('shops-pagination', page, data.totalPages, loadShops);
            return;
        }

        list.innerHTML = items.map(s => `
            <div class="shop-item">
                <div class="shop-icon">🏪</div>
                <div class="shop-info">
                    <div class="shop-name">${escapeHtml(s.shopName)}</div>
                    <div class="shop-meta">
                        ${escapeHtml(s.ownerName)} · ${escapeHtml(s.city || 'No location')} · ${s.totalProducts} products
                        <br>Created: ${new Date(s.createdAt).toLocaleDateString('en-ZA')}
                    </div>
                </div>
                <span class="shop-status ${escapeHtml(s.status.toLowerCase())}">${escapeHtml(s.status)}</span>
                <div class="shop-actions">
                    ${s.status === 'Pending' ? `
                        <button class="btn-sm approve" onclick="approveShop(${s.id})">Approve</button>
                        <button class="btn-sm reject" onclick="rejectShop(${s.id})">Reject</button>
                    ` : ''}
                    <button class="btn-sm view" onclick="viewShop(${s.id})">View</button>
                </div>
            </div>
        `).join('');

        renderPagination('shops-pagination', page, data.totalPages, loadShops);

    } catch (e) {
        console.error('Shops error:', e);
        list.innerHTML = '<p style="color:#6b7280">Could not load shops.</p>';
    }
}

async function viewShop(id) {
    try {
        const shop = await apiRequest(`/admin/shops/${id}`);

        const content = document.getElementById('shop-detail-content');
        content.innerHTML = `
            <div class="shop-detail-row">
                <span class="shop-detail-label">Shop Name</span>
                <span class="shop-detail-value">${escapeHtml(shop.shopName)}</span>
            </div>
            <div class="shop-detail-row">
                <span class="shop-detail-label">Owner</span>
                <span class="shop-detail-value">${escapeHtml(shop.ownerName)}</span>
            </div>
            <div class="shop-detail-row">
                <span class="shop-detail-label">Email</span>
                <span class="shop-detail-value">${escapeHtml(shop.ownerEmail)}</span>
            </div>
            <div class="shop-detail-row">
                <span class="shop-detail-label">Status</span>
                <span class="shop-detail-value">${escapeHtml(shop.status)}</span>
            </div>
            <div class="shop-detail-row">
                <span class="shop-detail-label">Verified</span>
                <span class="shop-detail-value">${shop.isVerified ? '✅ Yes' : '❌ No'}</span>
            </div>
            <div class="shop-detail-row">
                <span class="shop-detail-label">Total Sales</span>
                <span class="shop-detail-value">${shop.totalSales}</span>
            </div>
            <div class="shop-detail-row">
                <span class="shop-detail-label">Revenue</span>
                <span class="shop-detail-value">${fmt(shop.totalRevenue)}</span>
            </div>
            ${shop.shopDescription ? `
                <div class="shop-detail-row" style="flex-direction:column;align-items:stretch;gap:4px">
                    <span class="shop-detail-label">Description</span>
                    <span class="shop-detail-value">${escapeHtml(shop.shopDescription)}</span>
                </div>
            ` : ''}
            ${shop.verificationNotes ? `
                <div class="shop-detail-row" style="flex-direction:column;align-items:stretch;gap:4px">
                    <span class="shop-detail-label">Notes</span>
                    <span class="shop-detail-value">${escapeHtml(shop.verificationNotes)}</span>
                </div>
            ` : ''}
            ${shop.status === 'Pending' ? `
                <div class="shop-detail-actions">
                    <button class="btn-approve-shop" onclick="approveShop(${shop.id})">Approve Shop</button>
                    <button class="btn-reject-shop" onclick="rejectShop(${shop.id})">Reject Shop</button>
                </div>
            ` : ''}
        `;
        showModal('shop-detail-modal');

    } catch (e) {
        console.error('View shop error:', e);
        alert('Could not load shop details.');
    }
}

async function approveShop(id) {
    if (!confirm('Approve this shop?')) return;

    try {
        await apiRequest(`/admin/shops/${id}/approve`, {
            method: 'PUT',
            body: JSON.stringify({ notes: 'Approved by admin' })
        });
        closeModal('shop-detail-modal');
        loadShops(shopsPage);
        loadDashboard();
    } catch (e) {
        console.error('Approve error:', e);
        alert(e.message || 'Could not approve shop.');
    }
}

async function rejectShop(id) {
    // Keep asking until we get real text or the admin explicitly cancels -
    // previously a blank answer silently turned into "No reason provided"
    // without telling the admin that's what would be recorded.
    let reason = prompt('Please provide a reason for rejection:');
    while (reason !== null && reason.trim() === '') {
        reason = prompt('A reason is required to reject a shop. Please enter one (or Cancel to abort):');
    }
    if (reason === null) return;

    try {
        await apiRequest(`/admin/shops/${id}/reject`, {
            method: 'PUT',
            body: JSON.stringify({ reason: reason.trim() })
        });
        closeModal('shop-detail-modal');
        loadShops(shopsPage);
        loadDashboard();
    } catch (e) {
        console.error('Reject error:', e);
        alert(e.message || 'Could not reject shop.');
    }
}

// ── USERS ─────────────────────────────────────────────────
let usersPage = 1;
let usersTotalPages = 1;

async function loadUsers(page = 1) {
    usersPage = page;
    const role = document.getElementById('user-role-filter').value;
    const list = document.getElementById('users-list');
    list.innerHTML = '<div class="user-skeleton"></div>'.repeat(3);

    try {
        const data = await apiRequest(`/admin/users?role=${encodeURIComponent(role)}&page=${page}&pageSize=10`);
        const items = data?.items ?? [];
        usersTotalPages = data.totalPages;

        if (items.length === 0) {
            list.innerHTML = '<p style="color:#6b7280;padding:1rem;text-align:center">No users found.</p>';
            renderPagination('users-pagination', page, data.totalPages, loadUsers);
            return;
        }

        list.innerHTML = items.map(u => `
            <div class="user-item">
                <div class="user-avatar">${escapeHtml((u.fullName || '?').charAt(0).toUpperCase())}</div>
                <div class="user-info">
                    <div class="user-name">${escapeHtml(u.fullName)}</div>
                    <div class="user-email">${escapeHtml(u.email)}</div>
                </div>
                <span class="user-role ${escapeHtml(u.role.toLowerCase())}">${escapeHtml(u.role)}</span>
                <button class="user-status-toggle ${u.isActive ? 'active' : 'inactive'}" 
                        onclick="toggleUser(${u.id})">
                    ${u.isActive ? 'Active' : 'Inactive'}
                </button>
            </div>
        `).join('');

        renderPagination('users-pagination', page, data.totalPages, loadUsers);

    } catch (e) {
        console.error('Users error:', e);
        list.innerHTML = '<p style="color:#6b7280">Could not load users.</p>';
    }
}

async function toggleUser(id) {
    if (!confirm('Toggle user status?')) return;

    try {
        await apiRequest(`/admin/users/${id}/status`, { method: 'PUT' });
        loadUsers(usersPage);
    } catch (e) {
        console.error('Toggle error:', e);
        alert(e.message || 'Can not deactivate a user with orders in process.');
    }
}

// ── PRODUCTS ─────────────────────────────────────────────
let productsPage = 1;
let productsTotalPages = 1;

async function loadProducts(page = 1) {
    productsPage = page;
    const status = document.getElementById('product-status-filter').value;
    const list = document.getElementById('products-list');
    list.innerHTML = '<div class="product-skeleton"></div>'.repeat(3);

    try {
        const data = await apiRequest(`/admin/products?status=${encodeURIComponent(status)}&page=${page}&pageSize=10`);
        const items = data?.items ?? [];
        productsTotalPages = data.totalPages;

        if (items.length === 0) {
            list.innerHTML = '<p style="color:#6b7280;padding:1rem;text-align:center">No products found.</p>';
            renderPagination('products-pagination', page, data.totalPages, loadProducts);
            return;
        }

        list.innerHTML = items.map(p => `
            <div class="product-item">
                <div class="product-img">📦</div>
                <div class="product-info">
                    <div class="product-title">${escapeHtml(p.title)}</div>
                    <div class="product-meta">
                        ${escapeHtml(p.shopName)} · ${escapeHtml(p.categoryName || 'Uncategorized')} · ${p.views} views
                    </div>
                </div>
                <div class="product-price">${fmt(p.salePrice)}</div>
                <span class="status-badge status-${escapeHtml(p.status.toLowerCase())}">${escapeHtml(p.status)}</span>
                <button class="btn-sm view" onclick="toggleProduct(${p.id})">
                    ${p.status === 'Active' ? 'Deactivate' : 'Activate'}
                </button>
            </div>
        `).join('');

        renderPagination('products-pagination', page, data.totalPages, loadProducts);

    } catch (e) {
        console.error('Products error:', e);
        list.innerHTML = '<p style="color:#6b7280">Could not load products.</p>';
    }
}

async function toggleProduct(id) {
    if (!confirm('Toggle product status?')) return;

    try {
        await apiRequest(`/admin/products/${id}/toggle`, { method: 'PUT' });
        loadProducts(productsPage);
    } catch (e) {
        console.error('Toggle error:', e);
        alert(e.message || 'Could not toggle product status.');
    }
}

// ── ORDERS ───────────────────────────────────────────────
let adminOrdersPage = 1;
let adminOrdersTotalPages = 1;

async function loadAdminOrders(page = 1) {
    adminOrdersPage = page;
    const status = document.getElementById('order-status-filter').value;
    const list = document.getElementById('orders-list');
    list.innerHTML = '<div class="order-skeleton"></div>'.repeat(3);

    try {
        const data = await apiRequest(`/admin/orders?status=${encodeURIComponent(status)}&page=${page}&pageSize=10`);
        const items = data?.items ?? [];
        adminOrdersTotalPages = data.totalPages;

        if (items.length === 0) {
            list.innerHTML = '<p style="color:#6b7280;padding:1rem;text-align:center">No orders found.</p>';
            renderPagination('orders-pagination', page, data.totalPages, loadAdminOrders);
            return;
        }

        list.innerHTML = items.map(o => `
            <div class="order-item">
                <span class="order-number">#${escapeHtml(o.orderNumber)}</span>
                <div class="order-info">
                    <div class="order-buyer">${escapeHtml(o.buyerName)}</div>
                    <div class="order-shop">${escapeHtml(o.shopName)} · ${escapeHtml(o.shippingCity)}</div>
                </div>
                <div class="order-amount">${fmt(o.totalAmount)}</div>
                <span class="status-badge status-${escapeHtml(o.orderStatus.toLowerCase())}">${escapeHtml(o.orderStatus)}</span>
                <span style="font-size:11px;color:#6b7280">
                    ${o.trackingNumber ? '📦 ' + escapeHtml(o.trackingNumber) : ''}
                </span>
            </div>
        `).join('');

        renderPagination('orders-pagination', page, data.totalPages, loadAdminOrders);

    } catch (e) {
        console.error('Orders error:', e);
        list.innerHTML = '<p style="color:#6b7280">Could not load orders.</p>';
    }
}

// ── PAYOUTS ──────────────────────────────────────────────
let payoutsPage = 1;
let payoutsTotalPages = 1;

async function loadPayouts(page = 1) {
    payoutsPage = page;
    const status = document.getElementById('payout-status-filter').value;
    const list = document.getElementById('payouts-list');
    list.innerHTML = '<div class="payout-skeleton"></div>'.repeat(3);

    try {
        const data = await apiRequest(`/admin/payouts?status=${encodeURIComponent(status)}&page=${page}&pageSize=10`);
        const items = data?.items ?? [];
        payoutsTotalPages = data.totalPages;

        if (items.length === 0) {
            list.innerHTML = '<p style="color:#6b7280;padding:1rem;text-align:center">No payouts found.</p>';
            renderPagination('payouts-pagination', page, data.totalPages, loadPayouts);
            return;
        }

        list.innerHTML = items.map(p => `
            <div class="payout-item">
                <div class="payout-amount">${fmt(p.amount)}</div>
                <div class="payout-info">
                    <div class="payout-shop">${escapeHtml(p.shopName)}</div>
                    <div class="payout-meta">
                        ${escapeHtml(p.shopOwner)} · Order #${escapeHtml(p.orderNumber)}
                        <br>${new Date(p.createdAt).toLocaleDateString('en-ZA')}
                    </div>
                </div>
                <span class="payout-status ${escapeHtml(p.status.toLowerCase())}">${escapeHtml(p.status)}</span>
                ${p.status === 'Failed' ? `<span style="font-size:11px;color:#E24B4A">${escapeHtml(p.errorMessage || 'Failed')}</span>` : ''}
            </div>
        `).join('');

        renderPagination('payouts-pagination', page, data.totalPages, loadPayouts);

    } catch (e) {
        console.error('Payouts error:', e);
        list.innerHTML = '<p style="color:#6b7280">Could not load payouts.</p>';
    }
}

async function processPayouts() {
    if (!confirm('Process all pending payouts?')) return;

    try {
        const result = await apiRequest('/admin/payouts/process', { method: 'POST' });
        alert(result?.message || 'Payouts processed successfully!');
        loadPayouts(payoutsPage);
        loadDashboard();
    } catch (e) {
        console.error('Process payouts error:', e);
        alert(e.message || 'Could not process payouts.');
    }
}

// ── PAGINATION ───────────────────────────────────────────
// NOTE: relies on loadFn.name to rebuild the onclick string, which only
// works as long as these function names survive unminified. Fine for now,
// but worth switching to event delegation (data-page attributes + a single
// click listener) if this ever goes through a minifier/bundler.
function renderPagination(containerId, currentPage, totalPages, loadFn) {
    const container = document.getElementById(containerId);
    if (!container || totalPages <= 1) {
        if (container) container.innerHTML = '';
        return;
    }

    let html = `
        <button class="page-btn" onclick="${loadFn.name}(${currentPage - 1})" ${currentPage === 1 ? 'disabled' : ''}>
            <i class="ti ti-arrow-left"></i>
        </button>
    `;

    for (let i = 1; i <= totalPages; i++) {
        if (i === 1 || i === totalPages || Math.abs(i - currentPage) <= 1) {
            html += `<button class="page-btn ${i === currentPage ? 'active' : ''}" onclick="${loadFn.name}(${i})">${i}</button>`;
        } else if (Math.abs(i - currentPage) === 2) {
            html += `<span style="padding:0 4px;color:#6b7280">...</span>`;
        }
    }

    html += `
        <button class="page-btn" onclick="${loadFn.name}(${currentPage + 1})" ${currentPage === totalPages ? 'disabled' : ''}>
            <i class="ti ti-arrow-right"></i>
        </button>
    `;

    container.innerHTML = html;
}

// ── INIT ──────────────────────────────────────────────────
loadDashboard();