const API = 'https://localhost:7237/api';

// ── AUTH ──────────────────────────────────────────────────
function getToken() {
    const token = localStorage.getItem('token');
    console.log('🔑 Token:', token ? 'exists' : 'null');
    return token;
}

function getUser() {
    const u = localStorage.getItem('user');
    console.log('👤 User from localStorage:', u);
    return u ? JSON.parse(u) : null;
}

// 🔍 DEBUG: Log everything
console.log('=== SELLER DASHBOARD LOADED ===');
const user = getUser();
console.log('📦 User object:', user);
console.log('📦 hasShop:', user?.hasShop);

// ✅ Check if user exists
if (!user) {
    console.log('❌ No user found, redirecting to login');
    window.location.href = '/pages/login.html';
}

// If user has no shops, redirect to create shop page
if (!user.hasShop) {
    console.log('❌ User has no shops, redirecting to create-shop');
    window.location.href = '/pages/create-shop.html';
}

// If user is Admin, redirect to admin dashboard
if (user.role === 'Admin') {
    console.log('❌ User is Admin, redirecting to admin dashboard');
    window.location.href = '/pages/admin-dashboard.html';
}

console.log('✅ User is a seller, loading dashboard!');

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

// ── HELPERS ──────────────────────────────────────────────
function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

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
        const res = await fetch(`${API}/seller/stats`, { headers: authHeaders() });
        const stats = await res.json();

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
        const url = statusFilter ? `${API}/seller/orders?status=${statusFilter}` : `${API}/seller/orders`;
        const res = await fetch(url, { headers: authHeaders() });
        const orders = await res.json();

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

        list.innerHTML = orders.map(o => `
            <div class="order-item">
                <div class="order-img" style="background:#f8f8f6">
                    ${o.productImage ? `<img src="${o.productImage}" style="width:40px;height:40px;object-fit:cover;border-radius:8px" />` : '📦'}
                </div>
                <div class="order-info">
                    <div class="order-title">${o.productTitle}</div>
                    <div class="order-meta">
                        <span>${o.shopName}</span>
                        <span>·</span>
                        <span class="order-num">#${o.orderNumber}</span>
                        <span>·</span>
                        <span>${new Date(o.createdAt).toLocaleDateString('en-ZA')}</span>
                    </div>
                </div>
                <div class="order-right">
                    <div class="order-amount">${fmt(o.totalAmount)}</div>
                    <div class="status-badge ${statusClass(o.orderStatus)}">${o.orderStatus}</div>
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
        list.innerHTML = '<p style="color:#6b7280">Could not load orders.</p>';
    }
}

// ── LOAD SELLER PRODUCTS ─────────────────────────────────
async function loadSellerProducts() {
    const grid = document.getElementById('products-grid');
    grid.innerHTML = '<div class="product-skeleton"></div>'.repeat(4);

    try {
        const res = await fetch(`${API}/seller/products`, { headers: authHeaders() });
        const products = await res.json();

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
                    ${p.mainImageUrl ? `<img src="${p.mainImageUrl}" style="width:60px;height:60px;object-fit:cover;border-radius:8px" />` : '📦'}
                </div>
                <div class="product-card-body">
                    <div class="product-card-title">${p.title}</div>
                    <div class="product-card-price">${fmt(p.salePrice)}</div>
                    <div style="font-size:11px;color:#6b7280;margin-top:2px">
                        ${p.remainingQuantity} left · ${p.shopName}
                    </div>
                    <span class="product-card-status ${p.status.toLowerCase()}">${p.status}</span>
                </div>
                <div class="product-card-actions">
                    <button class="btn-small primary" onclick="editProduct(${p.id})">Edit</button>
                    <button class="btn-small danger" onclick="deleteProduct(${p.id})">Delete</button>
                </div>
            </div>
        `).join('');

    } catch (e) {
        console.error('Products error:', e);
        grid.innerHTML = '<p style="color:#6b7280">Could not load products.</p>';
    }
}

// ── LOAD SELLER SHOPS ────────────────────────────────────
async function loadSellerShops() {
    const list = document.getElementById('shops-list');
    list.innerHTML = '<div class="shop-skeleton"></div>';

    try {
        // ✅ Use /shops (ALL shops - for display)
        const res = await fetch(`${API}/seller/shops`, { headers: authHeaders() });
        const shops = await res.json();

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

        list.innerHTML = shops.map(s => `
            <div class="shop-card">
                <div class="shop-card-avatar">🏪</div>
                <div class="shop-card-info">
                    <div class="shop-card-name">${s.shopName}</div>
                    <div class="shop-card-meta">
                        ${s.city || 'No location'} · ${s.isVerified ? '✅ Verified' : '⏳ Pending approval'}
                    </div>
                </div>
                <span class="shop-card-status ${s.status?.toLowerCase() || 'pending'}">${s.status || 'Pending'}</span>
                <button class="btn-small primary" onclick="window.location.href='/pages/shop-profile.html?id=${s.id}'">
                    View Shop
                </button>
            </div>
        `).join('');

    } catch (e) {
        console.error('Shops error:', e);
        list.innerHTML = '<p style="color:#6b7280">Could not load shops.</p>';
    }
}

// ── LOAD SELLER PAYOUTS ──────────────────────────────────
async function loadSellerPayouts() {
    const list = document.getElementById('payouts-list');
    const totalEl = document.getElementById('total-payouts');
    list.innerHTML = '<div class="payout-skeleton"></div>'.repeat(3);

    try {
        const res = await fetch(`${API}/seller/payouts`, { headers: authHeaders() });
        const payouts = await res.json();

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
                    <div class="payout-meta">${p.shopName} · ${new Date(p.createdAt).toLocaleDateString('en-ZA')}</div>
                </div>
                <div>
                    <span class="payout-status ${p.status.toLowerCase()}">${p.status}</span>
                    ${p.status === 'Pending' ? '<div style="font-size:11px;color:#6b7280;margin-top:2px">Processing...</div>' : ''}
                </div>
            </div>
        `).join('');

    } catch (e) {
        console.error('Payouts error:', e);
        list.innerHTML = '<p style="color:#6b7280">Could not load payouts.</p>';
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
        const res = await fetch(`${API}/seller/orders/${currentOrderId}/status`, {
            method: 'PUT',
            headers: authHeaders(),
            body: JSON.stringify({
                status: status,
                trackingNumber: status === 'Shipped' ? tracking : null
            })
        });

        if (!res.ok) {
            const data = await res.json();
            throw new Error(data.message || 'Failed to update');
        }

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
        const res = await fetch(`${API}/seller/orders/${orderId}/cancel`, {
            method: 'PUT',
            headers: authHeaders()
        });

        if (res.ok) {
            loadSellerOrders();
            loadStats();
        } else {
            alert('Could not cancel order.');
        }
    } catch (e) {
        console.error('Cancel error:', e);
        alert('Something went wrong.');
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
    console.log('📝 Editing product ID:', id);

    try {
        const res = await fetch(`${API}/seller/products/${id}`, {
            headers: authHeaders()
        });

        console.log('📡 Response status:', res.status);

        if (!res.ok) {
            const error = await res.json();
            console.error('❌ Error response:', error);
            alert('Could not load product: ' + (error.message || 'Unknown error'));
            return;
        }

        const p = await res.json();
        console.log('✅ Product loaded:', p);

        // ✅ Populate form fields
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
        console.error('❌ Edit error:', e);
        alert('Could not load product: ' + e.message);
    }
}

async function populateShopSelect(selectedId) {
    const select = document.getElementById('prod-shop');
    select.innerHTML = '<option value="">Select a shop</option>';

    try {
        const res = await fetch(`${API}/seller/shops/verified`, { headers: authHeaders() });
        const shops = await res.json();

        // ✅ Use a Set to prevent duplicates
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

    try {
        const res = await fetch(`${API}/categories`);
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

    console.log('🔍 Form data being sent:', {
        title,
        description,
        originalPrice,
        salePrice,
        quantity,
        condition,
        shopId,
        categoryId,
        isUrgent,
        imageCount: images.length,
        editingProductId
    });

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
        console.log(`📷 Appending image ${i + 1}:`, images[i].name, images[i].size);
        formData.append('images', images[i]);
    }

    // ─── LOG FORM DATA ENTRIES ─────────────────────────
    console.log('📦 FormData entries:');
    for (let pair of formData.entries()) {
        const value = pair[1] instanceof File ? `[File: ${pair[1].name}]` : pair[1];
        console.log('  ', pair[0], '=', value);
    }

    const url = editingProductId ? `${API}/seller/products/${editingProductId}` : `${API}/seller/products`;
    const method = editingProductId ? 'PUT' : 'POST';

    console.log(`📡 Sending ${method} request to:`, url);

    try {
        const res = await fetch(url, {
            method: method,
            headers: {
                'Authorization': 'Bearer ' + getToken()
            },
            body: formData
        });

        console.log('📡 Response status:', res.status);
        console.log('📡 Response headers:', [...res.headers.entries()]);

        // ✅ Try to get response text first
        const responseText = await res.text();
        console.log('📡 Raw response:', responseText);

        let data;
        try {
            data = JSON.parse(responseText);
        } catch (e) {
            console.error('❌ Failed to parse JSON:', e);
            data = { message: responseText || 'Unknown response' };
        }

        if (!res.ok) {
            console.error('❌ Error response:', data);
            alert(`Error ${res.status}: ${data.message || 'Failed to save'}`);
            throw new Error(data.message || 'Failed to save');
        }

        console.log('✅ Success:', data);
        alert('✅ Product saved successfully!');

        // ─── CLEANUP ──────────────────────────────────────
        document.getElementById('image-preview').innerHTML = '';
        document.getElementById('prod-images').value = '';
        selectedFiles = [];

        closeModal('add-product-modal');
        loadSellerProducts();
        loadStats();

    } catch (e) {
        console.error('❌ Save error:', e);
        alert(`❌ Error: ${e.message}`);
        errEl.textContent = e.message;
        errEl.classList.add('show');
    } finally {
        btn.disabled = false;
        btn.textContent = editingProductId ? 'Update Product' : 'Add Product';
    }
}
async function deleteProduct(id) {
    if (!confirm('Delete this product permanently?')) return;

    console.log(`🗑️ Deleting product: ${id}`);

    try {
        const res = await fetch(`${API}/seller/products/${id}`, {
            method: 'DELETE',
            headers: authHeaders()
        });

        console.log(`📡 DELETE response status: ${res.status}`);

        if (!res.ok) {
            const error = await res.json();
            console.log('❌ Error:', error);
            alert(error.message || 'Could not delete product.');
            return;
        }

        const data = await res.json();
        console.log('✅ Delete success:', data);
        alert('Product deleted successfully!');

        loadSellerProducts();
        loadStats();

    } catch (e) {
        console.error('❌ Delete error:', e);
        alert('Something went wrong.');
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

    errEl.classList.remove('show');
    btn.disabled = true;
    btn.textContent = 'Creating...';

    try {
        const res = await fetch(`${API}/seller/shops`, {
            method: 'POST',
            headers: authHeaders(),
            body: JSON.stringify({ shopName, shopDescription, city, province, phoneNumber })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.message || 'Failed to create shop');
        }

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


// ── INIT ──────────────────────────────────────────────────
loadStats();
loadSellerOrders();
renderDashboardSwitcher();