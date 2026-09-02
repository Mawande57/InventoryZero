//const API = 'https://localhost:7237/api';
const API = '/api';
// ── AUTH ──────────────────────────────────────────────────
function getToken() { return localStorage.getItem('token'); }
function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

const user = getUser();

// Was three sequential `if` statements with no return/else - since
// window.location.href doesn't stop execution, a missing user would fall
// through into `!user.hasShop` and throw on `null.hasShop` before the
// redirect even completed. else-if avoids that entirely.
let shouldRedirect = false;

if (!user) {
    shouldRedirect = true;
    window.location.href = '/pages/login.html';
} else if (!user.hasShop) {
    shouldRedirect = true;
    window.location.href = '/pages/create-shop.html';
} else if (user.role === 'Admin') {
    shouldRedirect = true;
    window.location.href = '/pages/admin-dashboard.html';
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

// ── HELPERS ──────────────────────────────────────────────
function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

// Anything rendered via innerHTML has to go through this first. Order/
// product/shop titles and names all come from data other users typed in, so
// treating them as trusted HTML would let a malicious value execute script
// in the seller's own browser while managing their store.
function escapeHtml(value) {
    if (value === null || value === undefined) return '';
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

// South African phone numbers in local format: a leading 0 followed by 9
// digits (10 digits total). Spaces/dashes are stripped before checking.
function isValidSaPhone(value) {
    const digitsOnly = value.replace(/[\s-]/g, '');
    return /^0\d{9}$/.test(digitsOnly);
}

// Distinguishes "your connection looks offline" from "our server didn't
// respond" for genuine connectivity failures.
function getConnectionMessage() {
    if (typeof navigator !== 'undefined' && navigator.onLine === false) {
        return "You appear to be offline. Check your internet connection and try again.";
    }
    return "We're having trouble reaching the server right now. This is usually temporary — please try again in a moment.";
}

// Centralizes fetch + auth headers + error handling for the plain-JSON calls
// in this file. Several load functions (stats, orders, products, shops,
// payouts) previously called res.json() with no res.ok check at all.
// Deliberately NOT used for saveProduct()'s image upload - that request
// needs to omit Content-Type so the browser sets its own multipart boundary,
// and forcing the JSON default onto it would break file uploads.
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
        data = null;
    }

    if (!res.ok) {
        throw new Error(data?.message || `Request failed (${res.status}).`);
    }

    return data;
}

function statusClass(status) {
    const map = {
        'Pending': 'status-pending',
        'Processing': 'status-processing',
        'Shipped': 'status-shipped',
        'Delivered': 'status-delivered',
        'Cancelled': 'status-cancelled',
        'Refunded': 'status-refunded'
    };
    return map[status] || 'status-pending';
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
    if (tab === 'orders') loadSellerOrders();
    if (tab === 'products') loadSellerProducts();
    if (tab === 'shops') loadSellerShops();
    if (tab === 'payouts') loadSellerPayouts();
}

// ── LOAD DASHBOARD STATS ─────────────────────────────────
async function loadStats() {
    try {
        const stats = await apiRequest('/seller/stats');

        document.getElementById('stat-shops').textContent = stats.shops || 0;
        document.getElementById('stat-products').textContent = stats.products || 0;
        document.getElementById('stat-orders').textContent = stats.orders || 0;
        document.getElementById('stat-revenue').textContent = fmt(stats.revenue || 0);

        document.getElementById('nav-user').textContent = 'Hi, ' + (user.fullName.split(' ')[0]);
        document.getElementById('welcome-msg').textContent = 'Welcome back, ' + user.fullName.split(' ')[0];
    } catch (e) {
        console.error('Stats error:', e);
    }
}

// ── LOAD SELLER ORDERS ───────────────────────────────────
async function loadSellerOrders() {
    const statusFilter = document.getElementById('order-status-filter').value;
    const list = document.getElementById('orders-list');
    list.innerHTML = '<div class="order-skeleton"></div>'.repeat(3);

    try {
        const path = statusFilter
            ? `/seller/orders?status=${encodeURIComponent(statusFilter)}`
            : '/seller/orders';
        const orders = await apiRequest(path);

        if (orders.length === 0) {
            list.innerHTML = `
                <div class="empty-state" style="text-align:center;padding:3rem 1rem;grid-column:1/-1">
                    <div style="font-size:40px;margin-bottom:0.5rem">📭</div>
                    <h3 style="font-size:16px;color:#0a0a0a">No orders yet</h3>
                    <p style="font-size:13px;color:#6b7280">Orders will appear here once buyers purchase your products.</p>
                </div>
            `;
            return;
        }

        // NOTE: o.productImage will always be null here - SellerService's own
        // OrderSummaryDto mapping never sets it (OrderService's mapping for
        // the buyer's own order history does, but this endpoint uses a
        // different mapping that omits it). The field name below is correct;
        // the gap is server-side.
        list.innerHTML = orders.map(o => `
            <div class="order-item">
                <div class="order-img" style="background:#f8f8f6">
                    ${o.productImage ? `<img src="${escapeHtml(o.productImage)}" style="width:40px;height:40px;object-fit:cover;border-radius:8px" />` : '📦'}
                </div>
                <div class="order-info">
                    <div class="order-title">${escapeHtml(o.productTitle)}</div>
                    <div class="order-meta">
                        <span>${escapeHtml(o.shopName)}</span>
                        <span>·</span>
                        <span class="order-num">#${escapeHtml(o.orderNumber)}</span>
                        <span>·</span>
                        <span>${new Date(o.createdAt).toLocaleDateString('en-ZA')}</span>
                    </div>
                </div>
                <div class="order-right">
                    <div class="order-amount">${fmt(o.totalAmount)}</div>
                    <div class="status-badge ${statusClass(o.orderStatus)}">${escapeHtml(o.orderStatus)}</div>
                    <div class="order-actions">
                        ${o.orderStatus === 'Pending' ? `
                            <button class="btn-small primary" onclick="openStatusModal(${o.id}, '${o.orderNumber}')">
                                <i class="ti ti-check"></i> Process
                            </button>
                        ` : o.orderStatus === 'Processing' ? `
                            <button class="btn-small primary" onclick="openStatusModal(${o.id}, '${o.orderNumber}')">
                                <i class="ti ti-truck"></i> Ship
                            </button>
                        ` : o.orderStatus === 'Shipped' ? `
                            <button class="btn-small primary" onclick="openStatusModal(${o.id}, '${o.orderNumber}')">
                                <i class="ti ti-check"></i> Deliver
                            </button>
                        ` : ''}
                        ${o.orderStatus !== 'Delivered' && o.orderStatus !== 'Cancelled' ? `
                            <button class="btn-small danger" onclick="cancelOrder(${o.id})">Cancel</button>
                        ` : ''}
                    </div>
                </div>
            </div>
        `).join('');

    } catch (e) {
        console.error('Orders error:', e);
        list.innerHTML = `
            <div style="grid-column:1/-1;text-align:center;padding:1.5rem;">
                <p style="font-size:13px;color:#666;margin-bottom:8px;">${escapeHtml(getConnectionMessage())}</p>
                <button onclick="loadSellerOrders()" style="background:#1D9E75;color:#fff;border:none;padding:8px 20px;border-radius:8px;cursor:pointer;font-size:13px;">
                    Try again
                </button>
            </div>`;
    }
}

// ── LOAD SELLER PRODUCTS ─────────────────────────────────
async function loadSellerProducts() {
    const grid = document.getElementById('products-grid');
    grid.innerHTML = '<div class="product-skeleton"></div>'.repeat(4);

    try {
        const products = await apiRequest('/seller/products');

        if (products.length === 0) {
            grid.innerHTML = `
                <div class="empty-state" style="text-align:center;padding:3rem 1rem;grid-column:1/-1">
                    <div style="font-size:40px;margin-bottom:0.5rem">📦</div>
                    <h3 style="font-size:16px;color:#0a0a0a">No products listed</h3>
                    <p style="font-size:13px;color:#6b7280">Start selling by adding your first product.</p>
                    <button class="btn-primary" style="margin-top:1rem" onclick="showAddProductModal()">
                        <i class="ti ti-plus"></i> Add Product
                    </button>
                </div>
            `;
            return;
        }

        grid.innerHTML = products.map(p => `
            <div class="product-card">
                <div class="product-card-img" style="background:#f8f8f6">
                    ${p.mainImageUrl ? `<img src="${escapeHtml(p.mainImageUrl)}" style="width:60px;height:60px;object-fit:cover;border-radius:8px" />` : '📦'}
                </div>
                <div class="product-card-body">
                    <div class="product-card-title">${escapeHtml(p.title)}</div>
                    <div class="product-card-price">${fmt(p.salePrice)}</div>
                    <div style="font-size:11px;color:#6b7280;margin-top:2px">
                        ${p.remainingQuantity} left · ${escapeHtml(p.shopName)}
                    </div>
                    <span class="product-card-status ${p.status.toLowerCase()}">${escapeHtml(p.status)}</span>
                </div>
                <div class="product-card-actions">
                    <button class="btn-small primary" onclick="editProduct(${p.id})">Edit</button>
                    <button class="btn-small danger" onclick="deleteProduct(${p.id})">Delete</button>
                </div>
            </div>
        `).join('');

    } catch (e) {
        console.error('Products error:', e);
        grid.innerHTML = `
            <div style="grid-column:1/-1;text-align:center;padding:1.5rem;">
                <p style="font-size:13px;color:#666;margin-bottom:8px;">${escapeHtml(getConnectionMessage())}</p>
                <button onclick="loadSellerProducts()" style="background:#1D9E75;color:#fff;border:none;padding:8px 20px;border-radius:8px;cursor:pointer;font-size:13px;">
                    Try again
                </button>
            </div>`;
    }
}

// ── LOAD SELLER SHOPS ────────────────────────────────────
async function loadSellerShops() {
    const list = document.getElementById('shops-list');
    list.innerHTML = '<div class="shop-skeleton"></div>';

    try {
        // The seller's own shops (approved and pending alike), for display.
        const shops = await apiRequest('/seller/shops');

        if (!shops || shops.length === 0) {
            list.innerHTML = `
                <div class="empty-state" style="text-align:center;padding:3rem 1rem">
                    <div style="font-size:40px;margin-bottom:0.5rem">🏪</div>
                    <h3 style="font-size:16px;color:#0a0a0a">No shops yet</h3>
                    <p style="font-size:13px;color:#6b7280">Create your first shop to start selling.</p>
                    <button class="btn-primary" style="margin-top:1rem" onclick="showAddShopModal()">
                        <i class="ti ti-plus"></i> New Shop
                    </button>
                </div>
            `;
            return;
        }

        list.innerHTML = shops.map(s => {
            // Determine status display. Renamed from `statusClass` to
            // `shopStatusClass` - it was shadowing the statusClass() function
            // declared above. Harmless today since it's block-scoped to this
            // callback, but exactly the kind of thing that bites the next
            // person who edits this expecting statusClass to mean the function.
            let statusText = s.status || 'Pending';
            let shopStatusClass = s.status?.toLowerCase() || 'pending';
            let verifiedText = '';

            if (s.status === 'Active' && s.isVerified) {
                verifiedText = '✅ Verified';
            } else if (s.status === 'Pending') {
                verifiedText = '⏳ Pending approval';
            } else if (s.status === 'Rejected') {
                verifiedText = '❌ Rejected';
            } else if (s.status === 'Inactive') {
                verifiedText = '⛔ Inactive';
            }

            return `
                <div class="shop-card">
                    <div class="shop-card-avatar">🏪</div>
                    <div class="shop-card-info">
                        <div class="shop-card-name">${escapeHtml(s.shopName)}</div>
                        <div class="shop-card-meta">
                            ${escapeHtml(s.city || 'No location')} · ${verifiedText}
                        </div>
                    </div>
                    <span class="shop-card-status ${shopStatusClass}">${escapeHtml(statusText)}</span>
                    <button class="btn-small primary" onclick="window.location.href='/pages/shop-profile.html?id=${s.id}'">
                        View Shop
                    </button>
                </div>
            `;
        }).join('');

    } catch (e) {
        console.error('Shops error:', e);
        list.innerHTML = `
            <div style="text-align:center;padding:1.5rem;">
                <p style="font-size:13px;color:#666;margin-bottom:8px;">${escapeHtml(getConnectionMessage())}</p>
                <button onclick="loadSellerShops()" style="background:#1D9E75;color:#fff;border:none;padding:8px 20px;border-radius:8px;cursor:pointer;font-size:13px;">
                    Try again
                </button>
            </div>`;
    }
}

// ── LOAD SELLER PAYOUTS ──────────────────────────────────
async function loadSellerPayouts() {
    const list = document.getElementById('payouts-list');
    const totalEl = document.getElementById('total-payouts');
    list.innerHTML = '<div class="payout-skeleton"></div>'.repeat(3);

    try {
        const payouts = await apiRequest('/seller/payouts');

        const total = payouts.reduce((sum, p) => sum + (p.status === 'Completed' ? p.amount : 0), 0);
        totalEl.textContent = 'Total: ' + fmt(total);

        if (payouts.length === 0) {
            list.innerHTML = `
                <div class="empty-state" style="text-align:center;padding:3rem 1rem">
                    <div style="font-size:40px;margin-bottom:0.5rem">💰</div>
                    <h3 style="font-size:16px;color:#0a0a0a">No payouts yet</h3>
                    <p style="font-size:13px;color:#6b7280">Payouts will appear once orders are delivered.</p>
                </div>
            `;
            return;
        }

        list.innerHTML = payouts.map(p => `
            <div class="payout-item">
                <div>
                    <div class="payout-amount">${fmt(p.amount)}</div>
                    <div class="payout-meta">${escapeHtml(p.shopName)} · ${new Date(p.createdAt).toLocaleDateString('en-ZA')}</div>
                </div>
                <div>
                    <span class="payout-status ${p.status.toLowerCase()}">${escapeHtml(p.status)}</span>
                    ${p.status === 'Pending' ? '<div style="font-size:11px;color:#6b7280;margin-top:2px">Processing...</div>' : ''}
                </div>
            </div>
        `).join('');

    } catch (e) {
        console.error('Payouts error:', e);
        list.innerHTML = `
            <div style="text-align:center;padding:1.5rem;">
                <p style="font-size:13px;color:#666;margin-bottom:8px;">${escapeHtml(getConnectionMessage())}</p>
                <button onclick="loadSellerPayouts()" style="background:#1D9E75;color:#fff;border:none;padding:8px 20px;border-radius:8px;cursor:pointer;font-size:13px;">
                    Try again
                </button>
            </div>`;
    }
}

// ── ORDER STATUS MODAL ───────────────────────────────────
let currentOrderId = null;

function openStatusModal(orderId, orderNumber) {
    currentOrderId = orderId;
    document.getElementById('status-order-num').textContent = orderNumber;
    document.getElementById('status-select').value = 'Processing';
    document.getElementById('tracking-field').style.display = 'none';
    document.getElementById('status-err').classList.remove('show');
    showModal('status-modal');
}

document.getElementById('status-select').addEventListener('change', function () {
    document.getElementById('tracking-field').style.display = this.value === 'Shipped' ? 'block' : 'none';
});

async function updateOrderStatus() {
    const status = document.getElementById('status-select').value;
    const tracking = document.getElementById('tracking-number').value.trim();
    const errEl = document.getElementById('status-err');
    const btn = document.getElementById('status-save-btn');

    btn.disabled = true;
    btn.textContent = 'Updating...';
    errEl.classList.remove('show');

    try {
        await apiRequest(`/seller/orders/${currentOrderId}/status`, {
            method: 'PUT',
            body: JSON.stringify({
                status: status,
                trackingNumber: status === 'Shipped' ? tracking : null
            })
        });

        closeModal('status-modal');
        loadSellerOrders();
        loadStats();

    } catch (e) {
        errEl.textContent = e.message;
        errEl.classList.add('show');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Update Status';
    }
}

async function cancelOrder(orderId) {
    if (!confirm('Are you sure you want to cancel this order?')) return;

    try {
        await apiRequest(`/seller/orders/${orderId}/cancel`, { method: 'PUT' });
        loadSellerOrders();
        loadStats();
    } catch (e) {
        console.error('Cancel error:', e);
        alert(e.message || 'Could not cancel order.');
    }
}

// ── PRODUCT CRUD ──────────────────────────────────────────
let editingProductId = null;

async function showAddProductModal() {
    editingProductId = null;
    document.getElementById('prod-title').value = '';
    document.getElementById('prod-desc').value = '';
    document.getElementById('prod-original').value = '';
    document.getElementById('prod-sale').value = '';
    document.getElementById('prod-qty').value = '';
    document.getElementById('prod-urgent').checked = false;
    document.getElementById('prod-err').classList.remove('show');
    document.getElementById('prod-save-btn').textContent = 'Add Product';
    document.querySelector('#add-product-modal .modal-hdr h3').textContent = 'Add New Product';

    // Load shops and categories
    await populateShopSelect();
    await populateCategorySelect();
    showModal('add-product-modal');
}

async function editProduct(id) {
    try {
        const p = await apiRequest(`/seller/products/${id}`);

        document.getElementById('prod-title').value = p.title || '';
        document.getElementById('prod-desc').value = p.description || '';
        document.getElementById('prod-original').value = p.originalPrice || '';
        document.getElementById('prod-sale').value = p.salePrice || '';
        document.getElementById('prod-qty').value = p.quantity || '';
        document.getElementById('prod-condition').value = p.condition || 'New';
        document.getElementById('prod-urgent').checked = p.isUrgent || false;

        editingProductId = id;
        document.getElementById('prod-err').classList.remove('show');
        document.getElementById('prod-save-btn').textContent = 'Update Product';
        document.querySelector('#add-product-modal .modal-hdr h3').textContent = 'Edit Product';

        // Clear old images preview
        document.getElementById('image-preview').innerHTML = '';
        document.getElementById('prod-images').value = '';

        await populateShopSelect(p.shopId);
        await populateCategorySelect(p.categoryId);
        showModal('add-product-modal');

    } catch (e) {
        console.error('Edit error:', e);
        alert('Could not load product: ' + (e.message || 'Unknown error'));
    }
}

async function populateShopSelect(selectedId) {
    const select = document.getElementById('prod-shop');
    select.innerHTML = '<option value="">Select a shop</option>';

    try {
        const shops = await apiRequest('/seller/shops/verified');

        // Use a Map to prevent duplicates
        const uniqueShops = new Map();
        shops.forEach(s => {
            if (!uniqueShops.has(s.id)) {
                uniqueShops.set(s.id, s);
            }
        });

        if (uniqueShops.size === 0) {
            const opt = document.createElement('option');
            opt.value = '';
            opt.textContent = '⚠️ No verified shops - please contact admin';
            opt.disabled = true;
            select.appendChild(opt);
        }

        uniqueShops.forEach((s) => {
            const opt = document.createElement('option');
            opt.value = s.id;
            opt.textContent = s.shopName + ' ✅ Verified';
            if (s.id === selectedId) opt.selected = true;
            select.appendChild(opt);
        });
    } catch (e) {
        console.error('Shop select error:', e);
        const opt = document.createElement('option');
        opt.value = '';
        opt.textContent = '❌ Error loading shops';
        opt.disabled = true;
        select.appendChild(opt);
    }
}

async function populateCategorySelect(selectedId) {
    const select = document.getElementById('prod-category');
    select.innerHTML = '<option value="">Select category</option>';

    // Public endpoint - no auth header needed, matches original.
    try {
        const res = await fetch(`${API}/categories`);
        if (!res.ok) throw new Error(`Request failed (${res.status})`);
        const cats = await res.json();
        cats.forEach(c => {
            const opt = document.createElement('option');
            opt.value = c.id;
            opt.textContent = c.name;
            if (c.id === selectedId) opt.selected = true;
            select.appendChild(opt);
        });
    } catch (e) {
        console.error('Category select error:', e);
    }
}

async function saveProduct() {
    const errEl = document.getElementById('prod-err');
    const btn = document.getElementById('prod-save-btn');

    // ─── GET VALUES ─────────────────────────────────────
    const title = document.getElementById('prod-title').value.trim();
    const description = document.getElementById('prod-desc').value.trim();
    const originalPrice = parseFloat(document.getElementById('prod-original').value);
    const salePrice = parseFloat(document.getElementById('prod-sale').value);
    const quantity = parseInt(document.getElementById('prod-qty').value);
    const condition = document.getElementById('prod-condition').value;
    const shopId = parseInt(document.getElementById('prod-shop').value);
    const categoryId = document.getElementById('prod-category').value ? parseInt(document.getElementById('prod-category').value) : null;
    const isUrgent = document.getElementById('prod-urgent').checked;
    const imageInput = document.getElementById('prod-images');
    const images = imageInput.files;

    // ─── VALIDATION ─────────────────────────────────────
    if (!title || !originalPrice || !salePrice || !quantity || !shopId) {
        errEl.textContent = 'Please fill in all required fields.';
        errEl.classList.add('show');
        return;
    }

    if (salePrice > originalPrice) {
        errEl.textContent = 'Sale price cannot be higher than original price.';
        errEl.classList.add('show');
        return;
    }

    // ─── BUILD FORM DATA ───────────────────────────────
    errEl.classList.remove('show');
    btn.disabled = true;
    btn.textContent = 'Saving...';

    const formData = new FormData();
    formData.append('title', title);
    formData.append('description', description || '');
    formData.append('originalPrice', originalPrice.toString());
    formData.append('salePrice', salePrice.toString());
    formData.append('quantity', quantity.toString());
    formData.append('condition', condition);
    formData.append('shopId', shopId.toString());
    if (categoryId) formData.append('categoryId', categoryId.toString());
    formData.append('isUrgent', isUrgent ? 'true' : 'false');

    // Append images
    for (let i = 0; i < images.length; i++) {
        formData.append('images', images[i]);
    }

    const url = editingProductId ? `${API}/seller/products/${editingProductId}` : `${API}/seller/products`;
    const method = editingProductId ? 'PUT' : 'POST';

    try {
        // Not routed through apiRequest - this needs to omit Content-Type so
        // the browser sets its own multipart boundary. Forcing the JSON
        // default header onto a FormData body would break the upload.
        const res = await fetch(url, {
            method: method,
            headers: {
                'Authorization': 'Bearer ' + getToken()
            },
            body: formData
        });

        if (res.status === 401 || res.status === 403) {
            logout();
            return;
        }

        const responseText = await res.text();
        let data;
        try {
            data = JSON.parse(responseText);
        } catch (e) {
            data = { message: responseText || 'Unknown response' };
        }

        if (!res.ok) {
            // Previously this alerted here AND threw, which the outer catch
            // then alerted again for the same failure - two popups for one
            // error. Just throw; the catch block below handles the message.
            throw new Error(data.message || 'Failed to save');
        }

        alert('✅ Product saved successfully!');

        // ─── CLEANUP ──────────────────────────────────────
        document.getElementById('image-preview').innerHTML = '';
        document.getElementById('prod-images').value = '';
        selectedFiles = [];

        closeModal('add-product-modal');
        loadSellerProducts();
        loadStats();

    } catch (e) {
        console.error('Save error:', e);
        alert(`Error: ${e.message}`);
        errEl.textContent = e.message;
        errEl.classList.add('show');
    } finally {
        btn.disabled = false;
        btn.textContent = editingProductId ? 'Update Product' : 'Add Product';
    }
}

async function deleteProduct(id) {
    if (!confirm('Delete this product permanently?')) return;

    try {
        await apiRequest(`/seller/products/${id}`, { method: 'DELETE' });
        alert('Product deleted successfully!');
        loadSellerProducts();
        loadStats();
    } catch (e) {
        console.error('Delete error:', e);
        alert(e.message || 'Could not delete product.');
    }
}

// ── SHOP CRUD ─────────────────────────────────────────────
async function showAddShopModal() {
    document.getElementById('shop-name').value = '';
    document.getElementById('shop-desc').value = '';
    document.getElementById('shop-city').value = '';
    document.getElementById('shop-province').value = '';
    document.getElementById('shop-phone').value = '';
    document.getElementById('shop-err').classList.remove('show');
    showModal('add-shop-modal');
}

async function saveShop() {
    const errEl = document.getElementById('shop-err');
    const btn = document.getElementById('shop-save-btn');

    const shopName = document.getElementById('shop-name').value.trim();
    const shopDescription = document.getElementById('shop-desc').value.trim();
    const city = document.getElementById('shop-city').value.trim();
    const province = document.getElementById('shop-province').value;
    const phoneNumber = document.getElementById('shop-phone').value.trim();

    if (!shopName) {
        errEl.textContent = 'Shop name is required.';
        errEl.classList.add('show');
        return;
    }
    // Phone stays optional here too (CreateShopDto never requires it) - only
    // checked if the field wasn't left blank.
    if (phoneNumber && !isValidSaPhone(phoneNumber)) {
        errEl.textContent = 'Please enter a valid South African phone number, e.g. 082 123 4567.';
        errEl.classList.add('show');
        return;
    }

    errEl.classList.remove('show');
    btn.disabled = true;
    btn.textContent = 'Creating...';

    try {
        await apiRequest('/seller/shops', {
            method: 'POST',
            body: JSON.stringify({
                shopName,
                shopDescription,
                city,
                province,
                phoneNumber: phoneNumber.replace(/[\s-]/g, '')
            })
        });

        closeModal('add-shop-modal');
        loadSellerShops();
        loadStats();

    } catch (e) {
        errEl.textContent = e.message;
        errEl.classList.add('show');
    } finally {
        btn.disabled = false;
        btn.textContent = 'Create Shop';
    }
}

function renderDashboardSwitcher() {
    const user = getUser();
    if (!user || user.role === 'Admin') return;

    const container = document.getElementById('dashboard-switcher');
    if (!container) return;

    // Check which page we're on
    const isSellerPage = window.location.pathname.includes('seller-dashboard');
    const isBuyerPage = window.location.pathname.includes('buyer-dashboard');

    if (user.hasShop) {
        container.innerHTML = `
            <button class="btn-ghost ${isBuyerPage ? 'active' : ''}" 
                    onclick="window.location.href='/pages/buyer-dashboard.html'">
                🛒 Buyer
            </button>
            <button class="btn-ghost ${isSellerPage ? 'active' : ''}" 
                    onclick="window.location.href='/pages/seller-dashboard.html'">
                🏪 Seller
            </button>
        `;
    } else {
        container.innerHTML = `
            <button class="btn-ghost" onclick="window.location.href='/pages/create-shop.html'">
                🏪 Become a seller
            </button>
        `;
    }
}

// ── IMAGE PREVIEW ────────────────────────────────────────
// These src values are data: URIs generated locally by FileReader from a
// file the current user just picked from their own disk - never touch the
// server, and can't affect anyone but the person who selected the file, so
// there's no cross-user injection surface here the way there is with
// server-sourced image URLs elsewhere in this file.

let selectedFiles = [];

function previewImages(event) {
    const preview = document.getElementById('image-preview');
    preview.innerHTML = '';
    selectedFiles = [];

    const files = event.target.files;
    if (files.length > 5) {
        alert('Maximum 5 images allowed.');
        event.target.value = '';
        return;
    }

    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        if (!file.type.startsWith('image/')) continue;

        selectedFiles.push(file);

        const reader = new FileReader();
        reader.onload = function (e) {
            const div = document.createElement('div');
            div.style.cssText = `
                position: relative;
                width: 80px;
                height: 80px;
                border-radius: 8px;
                overflow: hidden;
                border: 1px solid #e8e8e8;
            `;
            div.innerHTML = `
                <img src="${e.target.result}" style="width:100%;height:100%;object-fit:cover;" />
                <span onclick="removeImage(${i})" style="
                    position:absolute;top:2px;right:2px;
                    background:rgba(0,0,0,0.6);color:#fff;
                    border-radius:50%;width:20px;height:20px;
                    display:flex;align-items:center;justify-content:center;
                    cursor:pointer;font-size:12px;
                ">×</span>
            `;
            preview.appendChild(div);
        };
        reader.readAsDataURL(file);
    }
}

function removeImage(index) {
    selectedFiles.splice(index, 1);
    // Rebuild preview
    const input = document.getElementById('prod-images');
    const dt = new DataTransfer();
    selectedFiles.forEach(f => dt.items.add(f));
    input.files = dt.files;
    previewImages({ target: input });
}


// ── INIT ─────────────────────────────────────────────────
// Gated behind shouldRedirect so a user about to be redirected away doesn't
// still fire off a batch of authenticated API calls in the meantime.
if (!shouldRedirect) {
    loadStats();
    loadSellerOrders();
    renderDashboardSwitcher();
}