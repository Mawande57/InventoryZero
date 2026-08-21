const API = 'https://localhost:7237/api';

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

const DOT_COLORS = {
    'Clothing': '#1D9E75', 'Electronics': '#185FA5',
    'Food & Drinks': '#993C1D', 'Furniture': '#854F0B',
    'Hardware': '#534AB7', 'Sport & Fitness': '#3B6D11',
    'Beauty & Health': '#993556', 'Other': '#5F5E5A'
};

// ── STATE ────────────────────────────────────────────────
let state = {
    search: '', categorySlug: '', minPrice: '',
    maxPrice: '', condition: '', isUrgent: false,
    sortBy: 'newest', page: 1, pageSize: 12
};

let savedIds = new Set();

// ── HELPERS ──────────────────────────────────────────────
function fmt(n) { return 'R' + Number(n).toLocaleString('en-ZA'); }

function daysLeft(dateStr) {
    const diff = new Date(dateStr) - new Date();
    const d = Math.ceil(diff / 86400000);
    if (d <= 0) return 'Expired';
    if (d === 1) return '1d left';
    return d + 'd left';
}

function getToken() { return localStorage.getItem('token'); }

function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

function isAdmin() {
    const user = getUser();
    return user && user.role === 'Admin';
}

// ── ADMIN RESTRICTIONS ──────────────────────────────────
function showAdminBanner() {
    if (!isAdmin()) return;

    const existing = document.querySelector('.admin-banner');
    if (existing) existing.remove();

    const banner = document.createElement('div');
    banner.className = 'admin-banner';
    banner.style.cssText = `
        background: #1D9E75;
        color: #fff;
        padding: 10px 20px;
        border-radius: 8px;
        text-align: center;
        margin: 10px auto;
        max-width: 1140px;
        font-size: 14px;
    `;
    banner.innerHTML = `
        <i class="ti ti-shield-check" style="margin-right:8px;"></i> 
        You are viewing as <strong>Admin</strong>. You cannot make purchases.
    `;

    const nav = document.querySelector('.iz-nav');
    if (nav && nav.parentNode) {
        nav.parentNode.insertBefore(banner, nav.nextSibling);
    }
}

function hideBuyButtonsForAdmin() {
    if (!isAdmin()) return;

    document.querySelectorAll('.btn-search').forEach(btn => {
        btn.style.display = 'none';
    });
}

// ── NAV AUTH CHECK ───────────────────────────────────────
function checkAuth() {
    const user = getUser();
    const navRight = document.getElementById('nav-right');
    if (!user) return;

    const isAdmin = user.role === 'Admin';
    let dashboardLinks = '';

    if (isAdmin) {
        dashboardLinks = `<button class="btn-solid" onclick="window.location.href='/pages/admin-dashboard.html'">Dashboard</button>`;
        setTimeout(() => {
            showAdminBanner();
            hideBuyButtonsForAdmin();
        }, 100);
    } else {
        dashboardLinks += `<button class="btn-ghost" onclick="window.location.href='/pages/buyer-dashboard.html'">Buyer dashboard</button>`;
        if (user.hasShop) {
            dashboardLinks += `<button class="btn-solid" onclick="window.location.href='/pages/seller-dashboard.html'">Seller dashboard</button>`;
        } else {
            dashboardLinks += `<button class="btn-solid" onclick="window.location.href='/pages/create-shop.html'">Become a seller</button>`;

        }
    }

    navRight.innerHTML = `
        <span style="font-size:13px;color:rgba(255,255,255,0.6)">Hi, ${user.fullName.split(' ')[0]}</span>
        ${dashboardLinks}`;
}

// ── FILTER HELPERS ───────────────────────────────────────
function selectCat(el) {
    document.querySelectorAll('.filter-cat').forEach(c => c.classList.remove('active'));
    el.classList.add('active');
    state.categorySlug = el.dataset.slug || '';
}

function selectCond(el, val) {
    document.querySelectorAll('.cond-btn').forEach(b => b.classList.remove('active'));
    el.classList.add('active');
    state.condition = val;
}

function clearFilters() {
    state = { ...state, search: '', categorySlug: '', minPrice: '', maxPrice: '', condition: '', isUrgent: false, page: 1 };
    document.getElementById('search-input').value = '';
    document.getElementById('min-price').value = '';
    document.getElementById('max-price').value = '';
    document.getElementById('urgent-toggle').classList.remove('on');
    document.querySelectorAll('.filter-cat').forEach((c, i) => c.classList.toggle('active', i === 0));
    document.querySelectorAll('.cond-btn').forEach((b, i) => b.classList.toggle('active', i === 0));
    loadDeals();
    updateActiveTags();
}

function applyFilters() {
    state.search = document.getElementById('search-input').value.trim();
    state.minPrice = document.getElementById('min-price').value;
    state.maxPrice = document.getElementById('max-price').value;
    state.isUrgent = document.getElementById('urgent-toggle').classList.contains('on');
    state.sortBy = document.getElementById('sort-select').value;
    state.page = 1;
    loadDeals();
    updateActiveTags();
}

function updateActiveTags() {
    const tags = [];
    if (state.search) tags.push({ label: 'Search: ' + state.search, key: 'search' });
    if (state.categorySlug) tags.push({ label: state.categorySlug, key: 'categorySlug' });
    if (state.condition) tags.push({ label: state.condition, key: 'condition' });
    if (state.isUrgent) tags.push({ label: 'Urgent only', key: 'isUrgent' });
    if (state.minPrice) tags.push({ label: 'Min R' + state.minPrice, key: 'minPrice' });
    if (state.maxPrice) tags.push({ label: 'Max R' + state.maxPrice, key: 'maxPrice' });

    document.getElementById('active-filters').innerHTML = tags.map(t => `
        <div class="filter-tag" onclick="removeTag('${t.key}')">
            ${t.label} <i class="ti ti-x" aria-hidden="true"></i>
        </div>`).join('');
}

function removeTag(key) {
    if (key === 'isUrgent') {
        state.isUrgent = false;
        document.getElementById('urgent-toggle').classList.remove('on');
    } else {
        state[key] = '';
        if (key === 'search') document.getElementById('search-input').value = '';
        if (key === 'minPrice') document.getElementById('min-price').value = '';
        if (key === 'maxPrice') document.getElementById('max-price').value = '';
        if (key === 'categorySlug') {
            document.querySelectorAll('.filter-cat').forEach((c, i) => c.classList.toggle('active', i === 0));
        }
        if (key === 'condition') {
            document.querySelectorAll('.cond-btn').forEach((b, i) => b.classList.toggle('active', i === 0));
        }
    }
    state.page = 1;
    loadDeals();
    updateActiveTags();
}

// ── LOAD CATEGORIES ──────────────────────────────────────
async function loadCategories() {
    try {
        const res = await fetch(`${API}/categories`);
        const data = await res.json();
        const grid = document.getElementById('filter-cats');

        data.forEach(c => {
            const el = document.createElement('div');
            el.className = 'filter-cat';
            el.dataset.slug = c.slug;

            if (state.categorySlug === c.slug) el.classList.add('active');

            el.innerHTML = `
                <div class="filter-cat-dot" style="background:${DOT_COLORS[c.name] || '#6b7280'}"></div>
                <span>${c.name}</span>`;
            el.onclick = () => selectCat(el);
            grid.appendChild(el);
        });
    } catch (e) {
        console.error('Could not load categories', e);
    }
}

// ── LOAD DEALS ───────────────────────────────────────────
async function loadDeals() {
    document.getElementById('deals-grid').innerHTML = '<div class="deal-skeleton"></div>'.repeat(6);

    const params = new URLSearchParams();
    if (state.search) params.set('search', state.search);
    if (state.categorySlug) params.set('categorySlug', state.categorySlug);
    if (state.minPrice) params.set('minPrice', state.minPrice);
    if (state.maxPrice) params.set('maxPrice', state.maxPrice);
    if (state.condition) params.set('condition', state.condition);
    if (state.isUrgent) params.set('isUrgent', 'true');
    params.set('sortBy', state.sortBy);
    params.set('page', state.page);
    params.set('pageSize', state.pageSize);

    try {
        const res = await fetch(`${API}/products?${params}`);
        const data = await res.json();

        document.getElementById('result-count').textContent = data.totalCount.toLocaleString() + ' deals';
        document.getElementById('showing-count').textContent = data.items.length + ' of ' + data.totalCount;

        if (data.items.length === 0) {
            document.getElementById('deals-grid').innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">🔍</div>
                    <h3>No deals found</h3>
                    <p>Try adjusting your filters or search term</p>
                </div>`;
            document.getElementById('pagination').innerHTML = '';
            return;
        }

        // ✅ Get user for ownership check
        const user = getUser();
        const isAdminUser = isAdmin();

        document.getElementById('deals-grid').innerHTML = data.items.map(p => {
            const isOwnProduct = user && user.hasShop && p.shopOwnerId && user.id === p.shopOwnerId;
            const hideHeart = isOwnProduct || isAdminUser;

            return `
                <div class="deal" onclick='openModal(${JSON.stringify(p)})'>
                    <div class="deal-img" style="background:${CAT_COLORS[p.categoryName] || '#f5f5f3'}">
                        ${EMOJIS[p.categoryName] || '📦'}
                        <div class="deal-badges">
                            <div class="badge-off">-${Math.round(p.discountPercentage)}%</div>
                            ${p.isUrgent ? `<div class="badge-urgent">
                                <i class="ti ti-clock" aria-hidden="true"></i>
                                ${daysLeft(p.listingEndDate)}
                            </div>` : ''}
                            ${isOwnProduct ? `<div class="badge-own">📦 Your item</div>` : ''}
                        </div>
                        ${!hideHeart ? `
                            <button class="deal-save-btn" data-product-id="${p.id}" onclick="event.stopPropagation();toggleSave(${p.id})">
                                <i class="ti ti-heart" aria-hidden="true"></i>
                            </button>
                        ` : `
                            <span class="deal-save-disabled" style="position:absolute;top:8px;right:8px;font-size:11px;color:#6b7280;background:rgba(255,255,255,0.9);padding:2px 8px;border-radius:999px;">
                                ${isAdminUser ? '🔒' : '📦'}
                            </span>
                        `}
                    </div>
                    <div class="deal-body">
                        <div class="deal-cat">${p.categoryName || 'General'}</div>
                        <div class="deal-title">${p.title}</div>
                        <div class="deal-shop">
                            <i class="ti ti-building-store" style="font-size:11px" aria-hidden="true"></i>
                            ${p.shopName}${p.shopCity ? ', ' + p.shopCity : ''}
                        </div>
                        <div class="deal-pricing">
                            <span class="deal-price">${fmt(p.salePrice)}</span>
                            <span class="deal-orig">${fmt(p.originalPrice)}</span>
                        </div>
                        <div class="deal-foot">
                            <span class="deal-stock">
                                <span class="stock-dot"></span>
                                ${p.remainingQuantity} left
                            </span>
                            <span style="font-size:11px;color:#bbb">${daysLeft(p.listingEndDate)}</span>
                            ${isOwnProduct ? `<span style="font-size:10px;color:#B45309;background:#FEF3C7;padding:2px 8px;border-radius:999px;margin-left:4px;">Your item</span>` : ''}
                        </div>
                    </div>
                </div>
            `;
        }).join('');

        renderPagination(data);
        applySavedState();

    } catch (e) {
        document.getElementById('deals-grid').innerHTML = `
            <p style="font-size:13px;color:#666;padding:1rem;grid-column:1/-1">
                Could not load deals. Make sure the API is running.
            </p>`;
    }
}

// ── PAGINATION ───────────────────────────────────────────
function renderPagination(data) {
    if (data.totalPages <= 1) {
        document.getElementById('pagination').innerHTML = '';
        return;
    }

    let html = `
        <button class="page-btn" onclick="goPage(${data.page - 1})" ${data.page === 1 ? 'disabled' : ''}>
            <i class="ti ti-arrow-left" aria-hidden="true"></i>
        </button>`;

    for (let i = 1; i <= data.totalPages; i++) {
        if (i === 1 || i === data.totalPages || Math.abs(i - data.page) <= 1) {
            html += `<button class="page-btn ${i === data.page ? 'active' : ''}" onclick="goPage(${i})">${i}</button>`;
        } else if (Math.abs(i - data.page) === 2) {
            html += `<span class="page-info">...</span>`;
        }
    }

    html += `
        <button class="page-btn" onclick="goPage(${data.page + 1})" ${data.page === data.totalPages ? 'disabled' : ''}>
            <i class="ti ti-arrow-right" aria-hidden="true"></i>
        </button>`;

    document.getElementById('pagination').innerHTML = html;
}

function goPage(p) {
    state.page = p;
    loadDeals();
    window.scrollTo(0, 0);
}

// ── MODAL ────────────────────────────────────────────────
function openModal(p) {
    console.log('=== OPEN MODAL DEBUG ===');
    console.log('📦 Product object:', p);
    console.log('🔑 Product ID:', p.id);
    console.log('👤 Shop Owner ID from product:', p.shopOwnerId);

    const user = getUser();
    console.log('👤 Current user:', user);
    console.log('👤 User ID:', user?.id);
    console.log('👤 User hasShop:', user?.hasShop);

    const isOwnProduct = user && user.hasShop && p.shopOwnerId && user.id === p.shopOwnerId;
    console.log('✅ isOwnProduct result:', isOwnProduct);

    const bg = CAT_COLORS[p.categoryName] || '#f5f5f3';
    const modalHero = document.getElementById('m-img');
    modalHero.style.background = bg;
    const closeBtn = modalHero.querySelector('.modal-close');
    modalHero.innerHTML = '';
    modalHero.appendChild(closeBtn);
    modalHero.insertAdjacentText('beforeend', EMOJIS[p.categoryName] || '📦');

    document.getElementById('m-cat').textContent = p.categoryName || 'General';
    document.getElementById('m-title').textContent = p.title;
    document.getElementById('m-price').textContent = fmt(p.salePrice);
    document.getElementById('m-orig').textContent = fmt(p.originalPrice);
    document.getElementById('m-disc').textContent = '-' + Math.round(p.discountPercentage) + '% off';
    document.getElementById('m-cond').textContent = p.condition;
    document.getElementById('m-stock').textContent = p.remainingQuantity + ' units';
    document.getElementById('m-city').textContent = p.shopCity || 'South Africa';
    document.getElementById('m-ends').textContent = daysLeft(p.listingEndDate);
    document.getElementById('m-shop').textContent = p.shopName;
    document.getElementById('m-shoploc').textContent = p.shopCity || '';
    document.getElementById('m-shop-row').onclick = () => {
        window.location.href = '/pages/shop-profile.html?id=' + p.shopId;
    };
    document.getElementById('m-verified').innerHTML = '<i class="ti ti-shield-check" aria-hidden="true"></i> Verified';
    document.getElementById('m-desc').textContent = p.shortDescription || 'No description available.';

    const buyBtn = document.getElementById('m-buy');
    const parent = buyBtn.parentNode;

    // ✅ Remove any existing message before adding a new one
    const existingMsg = parent.querySelector('.own-product-msg, .admin-msg');
    if (existingMsg) {
        existingMsg.remove();
    }

    // Reset buy button
    buyBtn.style.display = 'block';
    buyBtn.textContent = 'Buy now — ' + fmt(p.salePrice);

    const isAdminUser = isAdmin();

    if (isAdminUser) {
        console.log('🔒 Admin user - hiding buy button');
        buyBtn.style.display = 'none';
        const msg = document.createElement('div');
        msg.className = 'admin-msg';
        msg.style.cssText = `
            flex: 1;
            padding: 13px;
            background: #e8e8e8;
            color: #6b7280;
            border-radius: 12px;
            text-align: center;
            font-size: 14px;
        `;
        msg.textContent = '🔒 Admin view only';
        parent.insertBefore(msg, buyBtn);
    } else if (isOwnProduct) {
        console.log('🚫 User owns this product - hiding buy button');
        buyBtn.style.display = 'none';
        const msg = document.createElement('div');
        msg.className = 'own-product-msg';
        msg.style.cssText = `
            flex: 1;
            padding: 13px;
            background: #FEF3C7;
            color: #B45309;
            border-radius: 12px;
            text-align: center;
            font-size: 14px;
            font-weight: 500;
        `;
        msg.textContent = '🚫 You cannot buy your own products';
        parent.insertBefore(msg, buyBtn);
    } else {
        console.log('✅ User can buy this product - showing buy button');
        buyBtn.onclick = () => {
            if (!getToken()) window.location.href = '/pages/login.html';
            else window.location.href = '/pages/checkout.html?slug=' + p.slug;
        };
    }

    // ─── SAVE BUTTON - Hide for own products ───
    const saveBtn = document.getElementById('m-save');

    // ✅ Remove existing save button message if any
    const parentActions = saveBtn.parentNode;
    const existingSaveMsg = parentActions.querySelector('.own-product-save-msg');
    if (existingSaveMsg) {
        existingSaveMsg.remove();
    }

    if (isOwnProduct || isAdminUser) {
        // Hide save button for own products or admin
        saveBtn.style.display = 'none';

        // Optional: Add a small note that saving isn't available
        const note = document.createElement('span');
        note.className = 'own-product-save-msg';
        note.style.cssText = `
            font-size: 11px;
            color: #6b7280;
            padding: 8px 12px;
            background: #f8f8f6;
            border-radius: 8px;
            text-align: center;
        `;
        note.textContent = isAdminUser ? '🔒 Admin' : '📦 Your item';
        parentActions.insertBefore(note, saveBtn);
    } else {
        saveBtn.style.display = 'flex';
        saveBtn.dataset.productId = p.id;
        saveBtn.onclick = () => toggleSave(p.id);
        setHeartVisual(saveBtn, savedIds.has(p.id));
    }

    document.getElementById('modal').classList.add('open');
    console.log('=== END OPEN MODAL ===');
}

function closeModal() {
    document.getElementById('modal').classList.remove('open');
}

// ── HEART VISUAL HELPERS ─────────────────────────────────
function setHeartVisual(btn, saved) {
    const icon = btn.querySelector('i');
    if (!icon) return;
    icon.classList.toggle('ti-heart', !saved);
    icon.classList.toggle('ti-heart-filled', saved);
    icon.style.color = saved ? '#E24B4A' : '#6b7280';
}

function applySavedState() {
    document.querySelectorAll('[data-product-id]').forEach(btn => {
        const id = Number(btn.dataset.productId);
        setHeartVisual(btn, savedIds.has(id));
    });
}

// ── LOAD SAVED IDs ───────────────────────────────────────
async function loadSavedIds() {
    if (!getToken()) return;
    try {
        const res = await fetch(`${API}/saved-products`, {
            headers: { 'Authorization': 'Bearer ' + getToken() }
        });
        if (res.ok) {
            const data = await res.json();
            savedIds = new Set(data.map(s => s.productId));
            applySavedState();
        }
    } catch (e) {
        console.error('Could not load saved products', e);
    }
}

// ── TOGGLE SAVE ──────────────────────────────────────────
async function toggleSave(productId) {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
        return;
    }

    const currentlySaved = savedIds.has(productId);

    try {
        if (currentlySaved) {
            const res = await fetch(`${API}/saved-products/${productId}`, {
                method: 'DELETE',
                headers: { 'Authorization': 'Bearer ' + getToken() }
            });
            if (res.ok) {
                savedIds.delete(productId);
                document.querySelectorAll(`[data-product-id="${productId}"]`).forEach(btn => setHeartVisual(btn, false));
                showToast('Removed from saved 🤍');
            }
        } else {
            const res = await fetch(`${API}/saved-products`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': 'Bearer ' + getToken()
                },
                body: JSON.stringify(productId)
            });
            if (res.ok) {
                savedIds.add(productId);
                document.querySelectorAll(`[data-product-id="${productId}"]`).forEach(btn => setHeartVisual(btn, true));
                showToast('Product saved! ❤️');
            }
        }
    } catch (e) {
        console.error('Save toggle error:', e);
    }
}

// ── TOAST ─────────────────────────────────────────────────
function showToast(message) {
    let toast = document.getElementById('toast');
    if (!toast) {
        toast = document.createElement('div');
        toast.id = 'toast';
        toast.style.cssText = `
            position: fixed; bottom: 2rem; left: 50%; transform: translateX(-50%);
            background: #0a0a0a; color: #fff; padding: 10px 24px;
            border-radius: 12px; font-size: 14px; z-index: 999;
            transition: opacity 0.3s; opacity: 0;
            pointer-events: none;
        `;
        document.body.appendChild(toast);
    }
    toast.textContent = message;
    toast.style.opacity = '1';
    setTimeout(() => { toast.style.opacity = '0'; }, 2500);
}

// ── LOAD STATS ───────────────────────────────────────────
async function loadStats() {
    try {
        const res = await fetch(`${API}/products?pageSize=1`);
        const data = await res.json();
        document.getElementById('result-count').textContent = data.totalCount + ' deals';
    } catch (e) {
        console.log('Could not load stats:', e);
    }
}

// ── INIT ─────────────────────────────────────────────────
const urlParams = new URLSearchParams(window.location.search);
if (urlParams.get('category')) state.categorySlug = urlParams.get('category');
if (urlParams.get('sort')) state.sortBy = urlParams.get('sort');
if (urlParams.get('search')) {
    state.search = urlParams.get('search');
    document.getElementById('search-input').value = state.search;
}

checkAuth();
loadCategories();
loadDeals();
loadSavedIds();
loadStats();