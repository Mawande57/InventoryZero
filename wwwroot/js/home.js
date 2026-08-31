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

const STEPS = {
    buyer: [
        { n: '1', h: 'Browse deals', p: 'Filter by category, location, discount or time left.' },
        { n: '2', h: 'Buy securely', p: 'Pay by card. Money held safely until delivery confirmed.' },
        { n: '3', h: 'Receive or collect', p: 'Delivery in 3–7 days or collect from the shop.' },
        { n: '4', h: 'Rate the seller', p: 'Your review builds trust for the whole community.' }
    ],
    seller: [
        { n: '1', h: 'Create your shop', p: 'Register your business and verify your bank account.' },
        { n: '2', h: 'List your stock', p: 'Upload photos, set liquidation price, pick expiry date.' },
        { n: '3', h: 'Get orders', p: 'Buyers purchase instantly. You get notified immediately.' },
        { n: '4', h: 'Get paid in 24h', p: '85% of the sale lands in your bank within 24 hours.' }
    ]
};

// ── HELPERS ──────────────────────────────────────────────
function daysLeft(dateStr) {
    const diff = new Date(dateStr) - new Date();
    const d = Math.ceil(diff / 86400000);
    if (d <= 0) return 'Expired';
    if (d === 1) return '1d left';
    if (d <= 3) return d + 'd left';
    return d + ' days';
}

function fmt(num) {
    return 'R' + Number(num).toLocaleString('en-ZA');
}

function getUser() {
    const u = localStorage.getItem('user');
    return u ? JSON.parse(u) : null;
}

function getToken() {
    return localStorage.getItem('token');
}

function isAdmin() {
    const user = getUser();
    return user && user.role === 'Admin';
}

// Anything rendered via innerHTML has to go through this first. Product
// titles, shop names, category names etc. all come from data other users
// typed in (a seller sets their own shop/product name), so treating them as
// trusted HTML would let a malicious value execute script in the browser of
// anyone visiting the homepage - this page needs no login at all, so it's
// the single most exposed surface on the whole site.
function escapeHtml(value) {
    if (value === null || value === undefined) return '';
    return String(value)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}

// Distinguishes "your connection looks offline" from "our server didn't
// respond" so a visitor gets something more useful than a generic failure
// message - and a hint about whether it's worth checking their own network
// versus just trying again in a moment.
function getConnectionMessage() {
    if (typeof navigator !== 'undefined' && navigator.onLine === false) {
        return "You appear to be offline. Check your internet connection and try again.";
    }
    return "We're having trouble reaching the server right now. This is usually temporary — please try again in a moment.";
}

// ── SAVED STATE ──────────────────────────────────────────
let savedIds = new Set();

// Holds the currently-rendered deals so the card click handler can look a
// product up by id instead of carrying the whole product as JSON inside an
// HTML attribute (see loadDeals/openModalById below for why).
let currentDeals = [];

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

    // user.fullName is only ever the current session's own name reflected
    // back to them, but it's still user-editable data, so it's escaped the
    // same as everything else rather than trusted just because it's "theirs."
    navRight.innerHTML = `
        <span style="font-size:13px;color:rgba(255,255,255,0.6)">Hi, ${escapeHtml(user.fullName.split(' ')[0])}</span>
        ${dashboardLinks}`;
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

    document.querySelectorAll('.btn-hero-p, .btn-hero-s').forEach(btn => {
        btn.style.display = 'none';
    });
}

// ── HOW IT WORKS TABS ────────────────────────────────────
function renderSteps(tab) {
    document.getElementById('how-steps').innerHTML = STEPS[tab]
        .map(s => `
            <div class="how-step">
                <div class="how-num">${s.n}</div>
                <h3>${s.h}</h3>
                <p>${s.p}</p>
            </div>`).join('');
}

function switchTab(tab, el) {
    document.querySelectorAll('.how-tab')
        .forEach(t => t.classList.remove('active'));
    el.classList.add('active');
    renderSteps(tab);
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

// ── LOAD CATEGORIES ──────────────────────────────────────
async function loadCategories() {
    try {
        const res = await fetch(`${API}/categories`);
        if (!res.ok) throw new Error(`Request failed (${res.status})`);
        const data = await res.json();

        document.getElementById('cats-grid').innerHTML = data.map(c => `
            <div class="cat" onclick="window.location.href='/pages/browse.html?category=${encodeURIComponent(c.slug)}'">
                <div class="cat-icon" style="background:${CAT_COLORS[c.name] || '#F1EFE8'}">
                    ${EMOJIS[c.name] || '📦'}
                </div>
                <span>${escapeHtml(c.name)}</span>
            </div>`).join('');

    } catch (e) {
        console.error('Could not load categories:', e);
        document.getElementById('cats-grid').innerHTML = `
            <div style="grid-column:1/-1;text-align:center;padding:1rem;">
                <p style="font-size:13px;color:#666;margin-bottom:8px;">${escapeHtml(getConnectionMessage())}</p>
                <button onclick="loadCategories()" style="background:#1D9E75;color:#fff;border:none;padding:8px 20px;border-radius:8px;cursor:pointer;font-size:13px;">
                    Try again
                </button>
            </div>`;
    }
}

// ── LOAD DEALS ───────────────────────────────────────────
async function loadDeals() {
    try {
        const res = await fetch(`${API}/products?sortBy=ending-soon&pageSize=8`);
        if (!res.ok) throw new Error(`Request failed (${res.status})`);
        const data = await res.json();

        // Keep the current set of deals around so cards can look themselves
        // up by id when clicked (see openModalById).
        currentDeals = data.items;

        document.getElementById('s-deals').textContent = data.totalCount.toLocaleString() + '+';

        const user = getUser();
        const isAdminUser = isAdmin();

        document.getElementById('deals-grid').innerHTML = data.items.map(p => {
            const isOwnProduct = user && user.hasShop && p.shopOwnerId && user.id === p.shopOwnerId;
            const hideHeart = isOwnProduct || isAdminUser;

            const imageHtml = p.mainImageUrl
                ? `<img src="${escapeHtml(p.mainImageUrl)}" style="width:100%;height:100%;object-fit:cover;border-radius:12px 12px 0 0;" />`
                : EMOJIS[p.categoryName] || '📦';

            return `
                <div class="deal" onclick="openModalById(${p.id})">
                    <div class="deal-img" style="background:${CAT_COLORS[p.categoryName] || '#f5f5f3'};position:relative;overflow:hidden;">
                        ${imageHtml}
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
                        <div class="deal-cat">${escapeHtml(p.categoryName || 'General')}</div>
                        <div class="deal-title">${escapeHtml(p.title)}</div>
                        <div class="deal-shop">
                            <i class="ti ti-building-store" style="font-size:11px" aria-hidden="true"></i>
                            ${escapeHtml(p.shopName)}${p.shopCity ? ', ' + escapeHtml(p.shopCity) : ''}
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
                            <span style="font-size:11px;color:#bbb">${daysLeft(p.listingEndDate)} left</span>
                            ${isOwnProduct ? `<span style="font-size:10px;color:#B45309;background:#FEF3C7;padding:2px 8px;border-radius:999px;margin-left:4px;">Your item</span>` : ''}
                        </div>
                    </div>
                </div>
            `;
        }).join('');

        applySavedState();

    } catch (e) {
        console.error('Could not load deals:', e);
        document.getElementById('deals-grid').innerHTML = `
            <div style="grid-column:1/-1;text-align:center;padding:1.5rem;">
                <p style="font-size:13px;color:#666;margin-bottom:8px;">${escapeHtml(getConnectionMessage())}</p>
                <button onclick="loadDeals()" style="background:#1D9E75;color:#fff;border:none;padding:8px 20px;border-radius:8px;cursor:pointer;font-size:13px;">
                    Try again
                </button>
            </div>`;
    }
}

// ── MODAL ────────────────────────────────────────────────
let currentImageIndex = 0;
let productImages = [];

// Looks the clicked card's product up from the current set of deals instead
// of the card carrying the whole product as JSON inside its onclick
// attribute. Embedding JSON.stringify(p) directly into a single-quoted HTML
// attribute meant any field containing a single quote - a product title, a
// shop name, anything a seller typed - could break out of the attribute and
// inject arbitrary markup/script. This page needs no login, so it's the most
// exposed surface on the whole site for that kind of attack.
function openModalById(id) {
    const product = currentDeals.find(p => p.id === id);
    if (!product) {
        console.error('Could not find product', id, 'in the current deals.');
        return;
    }
    openModal(product);
}

function openModal(productData) {
    if (!productData.imageUrls) {
        // List cards (ProductCardDto) only carry mainImageUrl, not the full
        // imageUrls array - that only comes back from the single-product
        // endpoint (ProductDetailDto), so fetch the full record first.
        fetch(`${API}/products/${productData.slug}`)
            .then(res => {
                if (!res.ok) throw new Error('Product not found');
                return res.json();
            })
            .then(fullProduct => renderModal(fullProduct))
            .catch(err => {
                console.error('Error fetching full product:', err);
                renderModal(productData);
            });
        return;
    }

    renderModal(productData);
}

function renderModal(p) {
    const user = getUser();
    const isOwnProduct = user && user.hasShop && p.shopOwnerId && user.id === p.shopOwnerId;
    const isAdminUser = isAdmin();

    productImages = p.imageUrls || [];
    currentImageIndex = 0;

    const carouselSlide = document.getElementById('m-carousel-slide');
    const dotsContainer = document.getElementById('carousel-dots');
    const counter = document.getElementById('carousel-counter');
    const prevBtn = document.getElementById('carousel-prev');
    const nextBtn = document.getElementById('carousel-next');

    if (productImages && productImages.length > 0) {
        // Escaped as an attribute value - uploaded image filenames incorporate
        // the original filename the uploader chose, so this isn't guaranteed
        // to be free of characters that could break out of the src attribute.
        carouselSlide.innerHTML = productImages.map((url, index) => `
            <div class="carousel-image-wrapper" data-index="${index}" style="display:${index === 0 ? 'flex' : 'none'};width:100%;height:100%;align-items:center;justify-content:center;">
                <img src="${escapeHtml(url)}" alt="Product image ${index + 1}" style="width:100%;height:100%;object-fit:contain;max-height:220px;" onerror="this.src='data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22100%22 height=%22100%22><text y=%22.9em%22 font-size=%2290%22>📦</text></svg>'"/>
            </div>
        `).join('');

        dotsContainer.innerHTML = productImages.map((_, index) => `
            <span class="carousel-dot ${index === 0 ? 'active' : ''}" onclick="goToImage(${index})"></span>
        `).join('');

        counter.textContent = `1 / ${productImages.length}`;
        prevBtn.style.display = productImages.length > 1 ? 'flex' : 'none';
        nextBtn.style.display = productImages.length > 1 ? 'flex' : 'none';
    } else {
        carouselSlide.innerHTML = `
            <div class="emoji-placeholder" style="font-size:72px;">${EMOJIS[p.categoryName] || '📦'}</div>
        `;
        dotsContainer.innerHTML = '';
        counter.textContent = '';
        prevBtn.style.display = 'none';
        nextBtn.style.display = 'none';
    }

    const bg = CAT_COLORS[p.categoryName] || '#f5f5f3';
    const modalHero = document.getElementById('m-img');
    modalHero.style.background = bg;

    // All of these use textContent, not innerHTML - the browser treats the
    // value as plain text rather than parsing it as markup, so there's
    // nothing to escape here.
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

    const existingMsg = parent.querySelector('.own-product-msg, .admin-msg');
    if (existingMsg) existingMsg.remove();

    buyBtn.style.display = 'block';
    buyBtn.textContent = 'Buy now — ' + fmt(p.salePrice);

    if (isAdminUser) {
        buyBtn.style.display = 'none';
        const msg = document.createElement('div');
        msg.className = 'admin-msg';
        msg.style.cssText = `flex:1;padding:13px;background:#e8e8e8;color:#6b7280;border-radius:12px;text-align:center;font-size:14px;`;
        msg.textContent = '🔒 Admin view only';
        parent.insertBefore(msg, buyBtn);
    } else if (isOwnProduct) {
        buyBtn.style.display = 'none';
        const msg = document.createElement('div');
        msg.className = 'own-product-msg';
        msg.style.cssText = `flex:1;padding:13px;background:#FEF3C7;color:#B45309;border-radius:12px;text-align:center;font-size:14px;font-weight:500;`;
        msg.textContent = '🚫 You cannot buy your own products';
        parent.insertBefore(msg, buyBtn);
    } else {
        buyBtn.onclick = () => {
            if (!getToken()) window.location.href = '/pages/login.html';
            else window.location.href = '/pages/checkout.html?slug=' + p.slug;
        };
    }

    const saveBtn = document.getElementById('m-save');
    const parentActions = saveBtn.parentNode;
    const existingSaveMsg = parentActions.querySelector('.own-product-save-msg');
    if (existingSaveMsg) existingSaveMsg.remove();

    if (isOwnProduct || isAdminUser) {
        saveBtn.style.display = 'none';
        const note = document.createElement('span');
        note.className = 'own-product-save-msg';
        note.style.cssText = `font-size:11px;color:#6b7280;padding:8px 12px;background:#f8f8f6;border-radius:8px;text-align:center;`;
        note.textContent = isAdminUser ? '🔒 Admin' : '📦 Your item';
        parentActions.insertBefore(note, saveBtn);
    } else {
        saveBtn.style.display = 'flex';
        saveBtn.dataset.productId = p.id;
        saveBtn.onclick = () => toggleSave(p.id);
        setHeartVisual(saveBtn, savedIds.has(p.id));
    }

    document.getElementById('modal').classList.add('open');
}

// ─── CAROUSEL FUNCTIONS ──────────────────────────────────

function changeImage(direction) {
    if (productImages.length === 0) return;
    currentImageIndex = (currentImageIndex + direction + productImages.length) % productImages.length;
    updateCarousel();
}

function goToImage(index) {
    if (index < 0 || index >= productImages.length) return;
    currentImageIndex = index;
    updateCarousel();
}

function updateCarousel() {
    const wrappers = document.querySelectorAll('.carousel-image-wrapper');
    const dots = document.querySelectorAll('.carousel-dot');
    const counter = document.getElementById('carousel-counter');

    wrappers.forEach((wrapper, index) => {
        wrapper.style.display = index === currentImageIndex ? 'flex' : 'none';
    });

    dots.forEach((dot, index) => {
        dot.classList.toggle('active', index === currentImageIndex);
    });

    if (counter) {
        counter.textContent = `${currentImageIndex + 1} / ${productImages.length}`;
    }
}

// Close modal when clicking the overlay (background)
function closeModalOverlay(event) {
    if (event && event.target && event.target.id === 'modal') {
        document.getElementById('modal').classList.remove('open');
    }
}

// Close modal from the X button
function closeModal() {
    document.getElementById('modal').classList.remove('open');
}

// ── INIT ─────────────────────────────────────────────────
// NOTE: this used to also call loadStats(), which fetched GET /api/shops to
// count verified shops for the s-shops stat. ShopsController has no such
// endpoint (only GET /api/shops/{id} and /{id}/products exist) - that call
// was silently 404ing on every homepage load. It also duplicated loadDeals's
// job of setting s-deals, with a different number format, so whichever
// request finished last would win and the display would flicker between
// "1234" and "1,234+". Removed rather than left half-broken; s-shops needs a
// real backend endpoint (e.g. a public shops list or a small stats route)
// before this can be wired back up correctly.
renderSteps('buyer');
checkAuth();
loadCategories();
loadDeals();
loadSavedIds();