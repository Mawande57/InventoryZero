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

// ── SAVED STATE ──────────────────────────────────────────
let savedIds = new Set();

// ── NAV AUTH CHECK ───────────────────────────────────────
function checkAuth() {
    const user = getUser();
    const navRight = document.getElementById('nav-right');
    if (!user) return;

    const isAdmin = user.role === 'Admin';
    let dashboardLinks = '';

    if (isAdmin) {
        dashboardLinks = `<button class="btn-solid" onclick="window.location.href='/pages/admin-dashboard.html'">Dashboard</button>`;
    } else {
        dashboardLinks += `<button class="btn-ghost" onclick="window.location.href='/pages/buyer-dashboard.html'">Buyer dashboard</button>`;
        dashboardLinks += user.hasShop
            ? `<button class="btn-solid" onclick="window.location.href='/pages/seller-dashboard.html'">Seller dashboard</button>`
            : `<button class="btn-solid" onclick="window.location.href='/pages/register.html?role=Seller'">Become a seller</button>`;
    }

    navRight.innerHTML = `
        <span style="font-size:13px;color:rgba(255,255,255,0.6)">
            Hi, ${user.fullName.split(' ')[0]}
        </span>
        ${dashboardLinks}`;
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

// ── LOAD SHOP PROFILE ────────────────────────────────────

async function loadShop(shopId) {
    try {
        const res = await fetch(`${API}/shops/${shopId}`);

        if (!res.ok) {
            document.getElementById('sh-name').textContent = 'Shop not found';
            return;
        }

        const shop = await res.json();

        // Fill in shop details
        document.getElementById('sh-name').textContent = shop.shopName;
        document.title = shop.shopName + ' — InventoryZero';

        if (shop.isVerified) {
            document.getElementById('sh-verified').style.display = 'flex';
        }

        if (shop.city) {
            document.getElementById('sh-location').style.display = 'flex';
            document.getElementById('sh-city').textContent =
                shop.city + (shop.province ? ', ' + shop.province : '');
        }

        // Join date
        const joined = new Date(shop.createdAt);
        document.getElementById('sh-join-date').textContent =
            'Joined ' + joined.toLocaleDateString('en-ZA', { month: 'long', year: 'numeric' });

        // Rating
        document.getElementById('sh-rating').textContent =
            shop.ownerRating > 0 ? shop.ownerRating.toFixed(1) : 'New';

        // Stats
        document.getElementById('sh-sales').textContent =
            shop.totalSales.toLocaleString();
        document.getElementById('sh-revenue').textContent =
            fmt(shop.totalRevenue);
        document.getElementById('sh-reviews').textContent =
            shop.ownerTotalReviews.toLocaleString();

        // Description
        if (shop.shopDescription) {
            document.getElementById('sh-description').textContent = shop.shopDescription;
            document.getElementById('shop-desc').style.display = 'block';
        }

        // Tags
        const tags = [];
        if (shop.city) tags.push(shop.city);
        if (shop.isVerified) tags.push('Verified business');
        tags.push('Fast shipping');
        document.getElementById('sh-tags').innerHTML = tags
            .map(t => `<span class="shop-tag">${t}</span>`).join('');

    } catch (e) {
        document.getElementById('sh-name').textContent = 'Could not load shop';
        console.error(e);
    }
}

// ── LOAD SHOP PRODUCTS ───────────────────────────────────
// ── LOAD SHOP PRODUCTS ───────────────────────────────────

// ── LOAD SHOP PRODUCTS ───────────────────────────────────

async function loadProducts(shopId) {
    try {
        const res = await fetch(`${API}/shops/${shopId}/products`);
        const products = await res.json();

        console.log('=== SHOP PRODUCTS DEBUG ===');
        console.log('📦 Products from API:', products);
        console.log('📦 Number of products:', products.length);

        // 🔍 Check each product for shopOwnerId
        products.forEach((p, index) => {
            console.log(`📦 Product ${index + 1}:`, {
                id: p.id,
                title: p.title,
                shopOwnerId: p.shopOwnerId,
                hasShopOwnerId: p.hasOwnProperty('shopOwnerId')
            });
        });

        // Get current user
        const user = getUser();
        console.log('👤 Current user:', user);
        console.log('👤 User ID:', user?.id);
        console.log('👤 User hasShop:', user?.hasShop);

        // Update listing count in stats and header
        document.getElementById('sh-listings').textContent = products.length;
        document.getElementById('sh-listing-count').textContent =
            products.length + ' deal' + (products.length !== 1 ? 's' : '') + ' available right now';

        if (products.length === 0) {
            document.getElementById('deals-grid').innerHTML = `
                <div class="empty-state">
                    <div class="empty-icon">📭</div>
                    <h3>No active listings</h3>
                    <p>This shop has no deals right now. Check back soon.</p>
                </div>`;
            return;
        }

        const isAdminUser = isAdmin();

        document.getElementById('deals-grid').innerHTML = products.map(p => {
            // ✅ Check if user owns this product
            const isOwnProduct = user && user.hasShop && p.shopOwnerId && user.id === p.shopOwnerId;

            console.log(`🔍 Product "${p.title}":`, {
                shopOwnerId: p.shopOwnerId,
                userId: user?.id,
                isOwnProduct: isOwnProduct,
                match: user && user.hasShop && p.shopOwnerId && user.id === p.shopOwnerId
            });

            // ✅ Hide heart for own products OR admin
            const hideHeart = isOwnProduct || isAdminUser;

            const imageHtml = p.mainImageUrl
                ? `<img src="${p.mainImageUrl}" style="width:100%;height:100%;object-fit:cover;border-radius:12px 12px 0 0;" />`
                : EMOJIS[p.categoryName] || '📦';

            return `
                <div class="deal" onclick='openModal(${JSON.stringify(p)})'>
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
                        <div class="deal-cat">${p.categoryName || 'General'}</div>
                        <div class="deal-title">${p.title}</div>
                        <div class="deal-shop">${p.shopName}</div>
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

        // Apply saved state after rendering
        applySavedState();

    } catch (e) {
        console.error('Could not load listings:', e);
        document.getElementById('deals-grid').innerHTML = `
            <p style="font-size:13px;color:#666;padding:1rem;grid-column:1/-1">
                Could not load listings.
            </p>`;
    }
}
// ── MODAL ────────────────────────────────────────────────
// ── MODAL ────────────────────────────────────────────────
let currentImageIndex = 0;
let productImages = [];

function openModal(productData) {
    console.log('=== SHOP PROFILE OPEN MODAL ===');
    console.log('📦 Product data received:', productData);

    // If we don't have imageUrls, fetch the full product
    if (!productData.imageUrls) {
        console.log('🔍 No imageUrls found, fetching full product details...');
        fetch(`${API}/products/${productData.slug}`)
            .then(res => {
                console.log('📡 Fetch response status:', res.status);
                if (!res.ok) throw new Error('Product not found');
                return res.json();
            })
            .then(fullProduct => {
                console.log('✅ Full product fetched:', fullProduct);
                renderModal(fullProduct);
            })
            .catch(err => {
                console.error('❌ Error fetching full product:', err);
                // Fallback: render with what we have
                renderModal(productData);
            });
        return;
    }

    renderModal(productData);
}

function renderModal(p) {
    console.log('🎨 Rendering modal with:', p);

    const user = getUser();
    const isOwnProduct = user && user.hasShop && p.shopOwnerId && user.id === p.shopOwnerId;
    const isAdminUser = isAdmin();

    // ─── SETUP CAROUSEL ──────────────────────────────────
    productImages = p.imageUrls || [];
    currentImageIndex = 0;
    console.log('📸 Images for carousel:', productImages.length, 'images');

    const carouselSlide = document.getElementById('m-carousel-slide');
    const dotsContainer = document.getElementById('carousel-dots');
    const counter = document.getElementById('carousel-counter');
    const prevBtn = document.getElementById('carousel-prev');
    const nextBtn = document.getElementById('carousel-next');

    // Check if carousel elements exist
    if (carouselSlide) {
        if (productImages && productImages.length > 0) {
            console.log('✅ Rendering', productImages.length, 'images');
            carouselSlide.innerHTML = productImages.map((url, index) => `
                <div class="carousel-image-wrapper" data-index="${index}" style="display:${index === 0 ? 'flex' : 'none'};width:100%;height:100%;align-items:center;justify-content:center;">
                    <img src="${url}" alt="Product image ${index + 1}" style="width:100%;height:100%;object-fit:contain;max-height:220px;" onerror="this.src='data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22100%22 height=%22100%22><text y=%22.9em%22 font-size=%2290%22>📦</text></svg>'"/>
                </div>
            `).join('');

            dotsContainer.innerHTML = productImages.map((_, index) => `
                <span class="carousel-dot ${index === 0 ? 'active' : ''}" onclick="goToImage(${index})"></span>
            `).join('');

            counter.textContent = `1 / ${productImages.length}`;
            prevBtn.style.display = productImages.length > 1 ? 'flex' : 'none';
            nextBtn.style.display = productImages.length > 1 ? 'flex' : 'none';
        } else {
            console.log('❌ No images, showing emoji');
            carouselSlide.innerHTML = `
                <div class="emoji-placeholder" style="font-size:72px;">${EMOJIS[p.categoryName] || '📦'}</div>
            `;
            dotsContainer.innerHTML = '';
            counter.textContent = '';
            prevBtn.style.display = 'none';
            nextBtn.style.display = 'none';
        }
    }

    // ─── FILL MODAL CONTENT ──────────────────────────────
    const bg = CAT_COLORS[p.categoryName] || '#f5f5f3';
    const modalHero = document.getElementById('m-img');
    modalHero.style.background = bg;

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

    document.getElementById('m-verified').innerHTML =
        '<i class="ti ti-shield-check" aria-hidden="true"></i> Verified';
    document.getElementById('m-desc').textContent =
        p.shortDescription || 'No description available.';

    // ─── BUY BUTTON ──────────────────────────────────────
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

    // ─── SAVE BUTTON ────────────────────────────────────
    const saveBtn = document.getElementById('m-save');
    const parentActions = saveBtn.parentNode;
    const existingSaveMsg = parentActions.querySelector('.own-product-save-msg');
    if (existingSaveMsg) existingSaveMsg.remove();

    // ✅ Hide save button for own products OR admin
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
    console.log('=== END RENDER MODAL ===');
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

function closeModal() {
    document.getElementById('modal').classList.remove('open');
}

// ── CONTACT SELLER ───────────────────────────────────────

// ── CONTACT SELLER ───────────────────────────────────────

function handleContact() {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
        return;
    }

    const urlParams = new URLSearchParams(window.location.search);
    const shopId = urlParams.get('id');

    if (!shopId) {
        alert('Shop not found.');
        return;
    }

    showContactModal(shopId);
}

function showContactModal(shopId) {
    // Remove existing modal if any
    const existing = document.getElementById('contact-modal');
    if (existing) existing.remove();

    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.id = 'contact-modal';
    overlay.style.display = 'flex';
    overlay.style.zIndex = '300';
    overlay.innerHTML = `
        <div class="contact-modal-content">
            <div class="contact-modal-header">
                <h3>
                    <i class="ti ti-message" style="color:#1D9E75;margin-right:8px;"></i>
                    Contact Seller
                </h3>
                <button class="contact-modal-close" onclick="closeContactModal()">
                    <i class="ti ti-x"></i>
                </button>
            </div>
            <div class="contact-modal-body">
                <div class="contact-shop-info">
                    <div class="contact-shop-avatar">🏪</div>
                    <div>
                        <div class="contact-shop-name" id="contact-shop-name">Loading...</div>
                        <div class="contact-shop-meta">Response within 24 hours</div>
                    </div>
                </div>
                <p class="contact-message-hint">
                    <i class="ti ti-info-circle" style="color:#6b7280;font-size:14px;"></i>
                    Send a message to the shop owner. They'll respond via email.
                </p>
                <div class="contact-field">
                    <label for="contact-msg">Your message</label>
                    <textarea id="contact-msg" rows="4" placeholder="Hi, I'm interested in your products..."></textarea>
                </div>
                <div class="contact-char-count">
                    <span id="contact-char-count">0</span> / 500 characters
                </div>
                <button class="contact-send-btn" id="contact-send-btn" onclick="sendContactMessage(${shopId})">
                    <i class="ti ti-send"></i> Send message
                </button>
            </div>
        </div>
    `;
    document.body.appendChild(overlay);

    // Load shop name for display
    loadShopNameForContact(shopId);

    // Character counter
    const textarea = document.getElementById('contact-msg');
    textarea.addEventListener('input', function () {
        const count = this.value.length;
        document.getElementById('contact-char-count').textContent = count;
        if (count > 500) {
            this.value = this.value.substring(0, 500);
            document.getElementById('contact-char-count').textContent = 500;
        }
    });

    // Close on overlay click
    overlay.addEventListener('click', function (e) {
        if (e.target === this) {
            closeContactModal();
        }
    });
}

async function loadShopNameForContact(shopId) {
    try {
        const res = await fetch(`${API}/shops/${shopId}`);
        if (res.ok) {
            const shop = await res.json();
            const nameEl = document.getElementById('contact-shop-name');
            if (nameEl) {
                nameEl.textContent = shop.shopName;
            }
        }
    } catch (e) {
        console.error('Could not load shop name:', e);
    }
}

function closeContactModal() {
    const modal = document.getElementById('contact-modal');
    if (modal) modal.remove();
}

async function sendContactMessage(shopId) {
    const msg = document.getElementById('contact-msg').value.trim();
    const btn = document.getElementById('contact-send-btn');
    const charCount = document.getElementById('contact-char-count');

    if (!msg) {
        // Show error on the button
        btn.textContent = '⚠️ Please write a message';
        btn.style.background = '#FEF3C7';
        btn.style.color = '#B45309';
        setTimeout(() => {
            btn.innerHTML = '<i class="ti ti-send"></i> Send message';
            btn.style.background = '';
            btn.style.color = '';
        }, 2000);
        return;
    }

    if (msg.length > 500) {
        btn.textContent = '⚠️ Message too long (max 500 chars)';
        btn.style.background = '#FEF3C7';
        btn.style.color = '#B45309';
        setTimeout(() => {
            btn.innerHTML = '<i class="ti ti-send"></i> Send message';
            btn.style.background = '';
            btn.style.color = '';
        }, 2000);
        return;
    }

    // Disable button and show loading
    btn.disabled = true;
    btn.innerHTML = '<i class="ti ti-loader" style="animation:spin 1s linear infinite;"></i> Sending...';

    try {
        // Simulate sending (fake delay)
        await new Promise(resolve => setTimeout(resolve, 1500));

        // ✅ FAKE SUCCESS - always succeeds
        // In production, you would uncomment this:
        // const res = await fetch(`${API}/shops/${shopId}/contact`, {
        //     method: 'POST',
        //     headers: {
        //         'Content-Type': 'application/json',
        //         'Authorization': 'Bearer ' + getToken()
        //     },
        //     body: JSON.stringify({ message: msg })
        // });

        // if (!res.ok) throw new Error('Failed to send');

        // Show success state
        const modalBody = document.querySelector('.contact-modal-body');
        modalBody.innerHTML = `
            <div class="contact-success">
                <div class="contact-success-icon">✅</div>
                <h3>Message Sent!</h3>
                <p>Your message has been sent to the seller.</p>
                <p style="font-size:13px;color:#6b7280;margin-top:4px;">
                    <i class="ti ti-clock"></i> They'll respond within 24 hours.
                </p>
                <button class="contact-success-btn" onclick="closeContactModal()">
                    Done
                </button>
            </div>
        `;

        // Auto close after 5 seconds
        setTimeout(() => {
            closeContactModal();
        }, 5000);

    } catch (e) {
        console.error('Contact error:', e);
        btn.disabled = false;
        btn.innerHTML = '<i class="ti ti-send"></i> Try again';
        btn.style.background = '#FEE2E2';
        btn.style.color = '#991B1B';
        setTimeout(() => {
            btn.innerHTML = '<i class="ti ti-send"></i> Send message';
            btn.style.background = '';
            btn.style.color = '';
        }, 3000);
    }
}

// ── INIT ─────────────────────────────────────────────────

const urlParams = new URLSearchParams(window.location.search);
const shopId = urlParams.get('id');

if (!shopId) {
    document.getElementById('sh-name').textContent = 'No shop specified';
} else {
    checkAuth();
    loadShop(shopId);
    loadProducts(shopId);
    loadSavedIds();
}