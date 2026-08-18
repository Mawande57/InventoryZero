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

// ── NAV AUTH CHECK ───────────────────────────────────────
function checkAuth() {
    const user = getUser();
    const navRight = document.getElementById('nav-right');
    if (user) {
        const dash = user.role === 'Seller'
            ? '/pages/seller-dashboard.html'
            : user.role === 'Admin'
                ? '/pages/admin-dashboard.html'
                : '/pages/buyer-dashboard.html';
        navRight.innerHTML = `
      <span style="font-size:13px;color:rgba(255,255,255,0.6)">
        Hi, ${user.fullName.split(' ')[0]}
      </span>
      <button class="btn-solid" onclick="window.location.href='${dash}'">Dashboard</button>`;
    }
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
async function loadProducts(shopId) {
    try {
        const res = await fetch(`${API}/shops/${shopId}/products`);
        const products = await res.json();

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

        document.getElementById('deals-grid').innerHTML = products.map(p => `
      <div class="deal" onclick='openModal(${JSON.stringify(p)})'>
        <div class="deal-img" style="background:${CAT_COLORS[p.categoryName] || '#f5f5f3'}">
          ${EMOJIS[p.categoryName] || '📦'}
          <div class="deal-badges">
            <div class="badge-off">-${Math.round(p.discountPercentage)}%</div>
            ${p.isUrgent
                ? `<div class="badge-urgent">
                   <i class="ti ti-clock" aria-hidden="true"></i>
                   ${daysLeft(p.listingEndDate)}
                 </div>`
                : ''}
          </div>
          <button class="deal-save-btn" onclick="event.stopPropagation();handleSave(${p.id})">
            <i class="ti ti-heart" aria-hidden="true"></i>
          </button>
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
          </div>
        </div>
      </div>`).join('');

    } catch (e) {
        document.getElementById('deals-grid').innerHTML = `
      <p style="font-size:13px;color:#666;padding:1rem;grid-column:1/-1">
        Could not load listings.
      </p>`;
    }
}

// ── MODAL ────────────────────────────────────────────────
function openModal(p) {
    const modalHero = document.getElementById('m-img');
    modalHero.style.background = CAT_COLORS[p.categoryName] || '#f5f5f3';
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
    document.getElementById('m-verified').innerHTML =
        '<i class="ti ti-shield-check" aria-hidden="true"></i> Verified';
    document.getElementById('m-desc').textContent =
        p.shortDescription || 'No description available.';

    const buyBtn = document.getElementById('m-buy');
    buyBtn.textContent = 'Buy now — ' + fmt(p.salePrice);
    buyBtn.onclick = () => {
        if (!getToken()) window.location.href = '/pages/login.html';
        else window.location.href = '/pages/checkout.html?slug=' + p.slug;
    };

    document.getElementById('m-save').onclick = () => {
        if (!getToken()) window.location.href = '/pages/login.html';
        else handleSave(p.id);
    };

    document.getElementById('modal').classList.add('open');
}

function closeModal() {
    document.getElementById('modal').classList.remove('open');
}

function handleSave(productId) {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
        return;
    }
    console.log('Save product', productId);
}

function handleContact() {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
    } else {
        alert('Messaging coming in Phase 6.');
    }
}

// ── INIT ───────────────────────────────────t ──────────────

// Get shopId from URL — shop-profile.html?id=1
const urlParams = new URLSearchParams(window.location.search);
const shopId = urlParams.get('id');
// ── SAVE/UNSAVE PRODUCT ────────────────────────────────
async function handleSave(productId) {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
        return;
    }

    try {
        const res = await fetch(`${API}/saved-products`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + getToken()
            },
            body: JSON.stringify(productId)
        });

        if (res.ok) {
            // Toggle heart icon
            const hearts = document.querySelectorAll(`.deal-save-btn[data-product-id="${productId}"] i, .btn-save-p i`);
            hearts.forEach(heart => {
                heart.classList.toggle('ti-heart');
                heart.classList.toggle('ti-heart-filled');
                heart.style.color = heart.classList.contains('ti-heart-filled') ? '#E24B4A' : '#6b7280';
            });

            // Show feedback
            showToast('Product saved! ❤️');
        } else if (res.status === 409) {
            // Already saved - unsave it
            await handleUnsave(productId);
        }
    } catch (e) {
        console.error('Save error:', e);
    }
}

async function handleUnsave(productId) {
    try {
        const res = await fetch(`${API}/saved-products/${productId}`, {
            method: 'DELETE',
            headers: {
                'Authorization': 'Bearer ' + getToken()
            }
        });

        if (res.ok) {
            const hearts = document.querySelectorAll(`.deal-save-btn[data-product-id="${productId}"] i, .btn-save-p i`);
            hearts.forEach(heart => {
                heart.classList.remove('ti-heart-filled');
                heart.classList.add('ti-heart');
                heart.style.color = '#6b7280';
            });
            showToast('Removed from saved 🤍');
        }
    } catch (e) {
        console.error('Unsave error:', e);
    }
}

// ── TOAST NOTIFICATION ───────────────────────────────────
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

// ── CHECK IF PRODUCT IS SAVED ───────────────────────────
async function checkSavedStatus(productId) {
    if (!getToken()) return;
    try {
        const res = await fetch(`${API}/saved-products`, {
            headers: { 'Authorization': 'Bearer ' + getToken() }
        });
        const saved = await res.json();
        const isSaved = saved.some(s => s.productId === productId);
        if (isSaved) {
            const hearts = document.querySelectorAll(`.deal-save-btn[data-product-id="${productId}"] i, .btn-save-p i`);
            hearts.forEach(heart => {
                heart.classList.remove('ti-heart');
                heart.classList.add('ti-heart-filled');
                heart.style.color = '#E24B4A';
            });
        }
    } catch (e) {
        console.error('Check saved error:', e);
    }
}
function handleContact() {
    if (!getToken()) {
        window.location.href = '/pages/login.html';
        return;
    }

    // Get shop ID from URL
    const urlParams = new URLSearchParams(window.location.search);
    const shopId = urlParams.get('id');

    // Show contact modal with shop info
    showContactModal(shopId);
}

function showContactModal(shopId) {
    // Simple contact modal
    const overlay = document.createElement('div');
    overlay.className = 'modal-overlay';
    overlay.id = 'contact-modal';
    overlay.innerHTML = `
        <div class="modal" style="max-width:400px">
            <div class="modal-hdr">
                <h3>Contact Seller</h3>
                <button class="modal-x" onclick="this.closest('.modal-overlay').remove()">
                    <i class="ti ti-x"></i>
                </button>
            </div>
            <div class="modal-body">
                <p style="color:#6b7280;font-size:14px;margin-bottom:1rem">
                    Send a message to the shop owner. They'll respond via email.
                </p>
                <div class="field">
                    <label>Your message</label>
                    <textarea id="contact-msg" rows="4" style="width:100%;padding:10px;border:0.5px solid #e8e8e8;border-radius:10px;font-family:inherit;resize:vertical"></textarea>
                </div>
                <button class="btn-save" onclick="sendContactMessage(${shopId})" style="width:100%">
                    Send message
                </button>
            </div>
        </div>
    `;
    document.body.appendChild(overlay);
}

async function sendContactMessage(shopId) {
    const msg = document.getElementById('contact-msg').value.trim();
    if (!msg) {
        alert('Please write a message.');
        return;
    }

    try {
        const res = await fetch(`${API}/shops/${shopId}/contact`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer ' + getToken()
            },
            body: JSON.stringify({ message: msg })
        });

        if (res.ok) {
            alert('Message sent! The seller will get back to you.');
            document.getElementById('contact-modal').remove();
        } else {
            alert('Could not send message. Try again.');
        }
    } catch (e) {
        console.error('Contact error:', e);
        alert('Something went wrong.');
    }
}
if (!shopId) {
    document.getElementById('sh-name').textContent = 'No shop specified';
} else {
    checkAuth();
    loadShop(shopId);
    loadProducts(shopId);
}