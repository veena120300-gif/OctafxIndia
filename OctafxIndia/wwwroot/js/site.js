// wwwroot/js/site.js
(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {

        /* -----------------------
           Utility helpers
           ----------------------- */

        function $(sel, root = document) { return root.querySelector(sel); }
        function $all(sel, root = document) { return Array.from(root.querySelectorAll(sel)); }

        function getAntiForgeryToken() {
            // Try common locations for an antiforgery token:
            // - hidden input named __RequestVerificationToken inside forms
            // - meta tag <meta name="csrf-token" content="...">
            const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            if (tokenInput && tokenInput.value) return tokenInput.value;
            const meta = document.querySelector('meta[name="csrf-token"]');
            if (meta && meta.content) return meta.content;
            return null;
        }

        function safeFetch(url, options = {}) {
            // wrapper to centralize error handling/logging
            return fetch(url, options).then(async (res) => {
                const ct = res.headers.get("content-type") || "";
                let body = null;
                try {
                    if (ct.indexOf("application/json") !== -1) body = await res.json();
                    else body = await res.text();
                } catch (e) {
                    body = null;
                }
                return { ok: res.ok, status: res.status, body, raw: res };
            }).catch(err => {
                console.error("Fetch error", err);
                return { ok: false, status: 0, body: null, error: err };
            });
        }

        function showInlineError(container, message) {
            if (!container) return alert(message);
            let el = container.querySelector('.panel-error');
            if (!el) {
                el = document.createElement('div');
                el.className = 'panel-error';
                el.style.color = '#b00020';
                el.style.marginTop = '8px';
                container.prepend(el);
            }
            el.textContent = message;
        }

        function clearInlineError(container) {
            if (!container) return;
            const el = container.querySelector('.panel-error');
            if (el) el.remove();
        }

        /* -----------------------
           DOM elements
           ----------------------- */
        const openBtn = document.querySelector('a[href="#login"]');
        const panel = document.getElementById('loginPanel');
        const overlay = document.getElementById('loginOverlay');
        const closeBtn = document.getElementById('closeLoginBtn');

        if (!panel || !overlay) {
            // required DOM not present — nothing to do
            console.warn("login panel / overlay not found — aborting login panel script");
            return;
        }

        const panelInner = panel.querySelector('.login-panel-inner');
        const originalInnerHTML = panelInner ? panelInner.innerHTML : '';
        const forgotTemplate = document.getElementById('forgotTemplate');

        /* -----------------------
           Open/close helpers
           ----------------------- */
        function openPanel() {
            panel.classList.add('open');
            overlay.classList.add('open');
            panel.setAttribute('aria-hidden', 'false');
            overlay.setAttribute('aria-hidden', 'false');
            document.body.style.overflow = 'hidden';

            setTimeout(() => {
                const firstInput = panel.querySelector('input');
                if (firstInput) firstInput.focus();
            }, 180);

            // attach interactive handlers for the visible content
            attachLoginEnhancements();
            attachForgotLinkListener();
            attachSocialHandlers();
        }

        function closePanel() {
            panel.classList.remove('open');
            overlay.classList.remove('open');
            panel.setAttribute('aria-hidden', 'true');
            overlay.setAttribute('aria-hidden', 'true');
            document.body.style.overflow = '';
            restoreLogin();
        }

        if (openBtn) {
            openBtn.addEventListener('click', function (e) {
                e.preventDefault();
                openPanel();
            });
        }

        if (closeBtn) {
            closeBtn.addEventListener('click', function (e) {
                e.preventDefault();
                closePanel();
            });
        }

        overlay.addEventListener('click', closePanel);

        document.addEventListener('keydown', function (e) {
            if (e.key === "Escape" && panel.classList.contains('open')) {
                closePanel();
            }
        });

        /* -----------------------
           Password toggle (if any)
           ----------------------- */
        function attachPasswordToggle() {
            const toggle = panel.querySelector('#togglePassword');
            const pwd = panel.querySelector('#passwordInput');

            if (!toggle || !pwd) return;

            const newToggle = toggle.cloneNode(true);
            toggle.parentNode.replaceChild(newToggle, toggle);

            newToggle.addEventListener('click', function (e) {
                e.preventDefault();
                const isPass = pwd.type === 'password';
                pwd.type = isPass ? 'text' : 'password';
                newToggle.setAttribute('aria-label', isPass ? 'Hide password' : 'Show password');
            });
        }

        /* -----------------------
           Clear buttons for inputs
           ----------------------- */
        function attachClearButtons() {
            function setupClear(inputSelector, clearSelector) {
                const input = panel.querySelector(inputSelector);
                const clear = panel.querySelector(clearSelector);
                if (!input || !clear) return;
                const newClear = clear.cloneNode(true);
                clear.parentNode.replaceChild(newClear, clear);

                newClear.style.display = input.value && input.value.length ? 'block' : 'none';

                input.addEventListener('input', function () {
                    newClear.style.display = input.value && input.value.length ? 'block' : 'none';
                });

                newClear.addEventListener('click', function (ev) {
                    ev.preventDefault();
                    input.value = '';
                    newClear.style.display = 'none';
                    input.focus();
                    input.dispatchEvent(new Event('input', { bubbles: true }));
                });
            }

            setupClear('#emailInput', '#clearEmail');
            setupClear('#passwordInput', '#clearPassword');
        }

        /* -----------------------
           Social login handlers (redirect to provider)
           ----------------------- */
        function attachSocialHandlers() {
            const fb = panel.querySelector('.social-fb');
            const apple = panel.querySelector('.social-apple');
            const google = panel.querySelector('.social-google');

            if (fb) {
                fb.addEventListener('click', function () {
                    window.location.href = "https://www.facebook.com/login.php";
                });
            }

            if (apple) {
                apple.addEventListener('click', function () {
                    window.location.href = "https://appleid.apple.com/";
                });
            }

            if (google) {
                google.addEventListener('click', function () {
                    window.location.href = "https://accounts.google.com/";
                });
            }
        }

        /* -----------------------
           Forgot password handling
           ----------------------- */
        function attachForgotLinkListener() {
            const forgotLink = panel.querySelector('.forgot-link');
            if (!forgotLink) return;
            const newLink = forgotLink.cloneNode(true);
            forgotLink.parentNode.replaceChild(newLink, forgotLink);

            newLink.addEventListener('click', function (e) {
                e.preventDefault();
                showForgot();
            });
        }

        function showForgot() {
            if (!panelInner) return;
            if (forgotTemplate && forgotTemplate.innerHTML.trim().length) {
                panelInner.innerHTML = forgotTemplate.innerHTML;
            } else {
                panelInner.innerHTML = `
                    <div class="panel-top">
                        <button type="button" class="back-btn" id="fpBackBtn" aria-label="Back">←</button>
                        <button type="button" class="login-close" id="fpCloseBtn" aria-label="Close">×</button>
                    </div>
                    <div class="login-panel-inner fp-inner">
                        <h2 class="fp-title">Reset password</h2>
                        <p class="fp-desc">Enter the email address you used to register. We’ll send a link to reset your password.</p>
                        <form id="forgotForm" method="post" action="/Account/ForgotPassword">
                            <div class="input-wrap">
                                <span class="icon-left">
                                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#999"><rect x="2" y="5" width="20" height="14" rx="2"></rect><path d="M2 7l10 7 10-7"></path></svg>
                                </span>
                                <input type="email" name="Email" placeholder="example@gmail.com" required autocomplete="email" class="text-input" />
                            </div>
                            <button type="submit" class="btn btn-primary fp-send">Send link</button>
                        </form>
                    </div>
                `;
            }
            attachForgotHandlers();
        }

        function attachForgotHandlers() {
            const fpBack = panel.querySelector('#fpBackBtn');
            const fpClose = panel.querySelector('#fpCloseBtn');
            const fpForm = panel.querySelector('#forgotForm');

            if (fpBack) fpBack.addEventListener('click', function (e) { e.preventDefault(); restoreLogin(); });
            if (fpClose) fpClose.addEventListener('click', function (e) { e.preventDefault(); closePanel(); });

            if (fpForm) {
                fpForm.addEventListener('submit', function (ev) {
                    // let the form post normally, or you can implement AJAX here
                });
            }
        }

        function restoreLogin() {
            if (!panelInner) return;
            if (panelInner.innerHTML === originalInnerHTML) return;
            panelInner.innerHTML = originalInnerHTML;

            // re-attach login-specific behavior
            attachLoginEnhancements();
            attachForgotLinkListener();
            attachSocialHandlers();
        }

        /* -----------------------
           OTP / Passwordless login flow
           ----------------------- */

        // Find login form if present
        function attachLoginFormHandler() {
            const form = panel.querySelector('#loginForm');
            if (!form) return;

            // remove any existing listener by cloning
            const newForm = form.cloneNode(true);
            form.parentNode.replaceChild(newForm, form);

            newForm.addEventListener('submit', function (e) {
                e.preventDefault();
                clearInlineError(panelInner);
                handleLoginStart(newForm);
            });
        }

        async function handleLoginStart(formEl) {
            // Read email only (user asked to remove password)
            const emailInput = formEl.querySelector('input[type="email"], input[name="Email"], #emailInput');
            if (!emailInput || !emailInput.value || emailInput.value.trim().length === 0) {
                showInlineError(panelInner, 'Please enter your email address.');
                return;
            }
            const email = emailInput.value.trim();

            // Determine endpoint: prefer form action if set, otherwise fallbacks
            const possible = [];
            if (formEl.action) possible.push(formEl.action);
            possible.push('/Account/LoginStart');
            possible.push('/api/account/loginstart');
            possible.push('/api/auth/start-login');
            possible.push('/Account/Login'); // last fallback

            const token = getAntiForgeryToken();

            // Payload
            const payload = { Email: email };

            // try endpoints sequentially until one responds OK-ish
            let response = null;
            for (const url of possible) {
                try {
                    const headers = { 'Content-Type': 'application/json' };
                    if (token) headers['RequestVerificationToken'] = token;

                    response = await safeFetch(url, {
                        method: 'POST',
                        headers,
                        body: JSON.stringify(payload),
                        credentials: 'same-origin'
                    });

                    // If server responded with 404/500, try next; otherwise break
                    if (!response || (response.status >= 500 && response.status !== 400)) {
                        // try next fallback
                        console.info('LoginStart try next endpoint, got', response && response.status, url);
                        continue;
                    }

                    // If we got any JSON or text back, break and handle it
                    break;
                } catch (err) {
                    console.warn('Error trying login start at', url, err);
                }
            }

            if (!response) {
                showInlineError(panelInner, 'Login request failed — please try again later.');
                return;
            }

            if (!response.ok) {
                // Try to show helpful message from server if available
                const msg = (response.body && response.body.message) || (response.body && response.body.error) || 'Login request failed';
                showInlineError(panelInner, msg);
                return;
            }

            // Response OK: server should indicate next step. Expected JSON shapes:
            // { next: "otp", otpToken: "abc123", message: "OTP sent" }   => show OTP UI
            // or { next: "redirect", redirectUrl: "/somewhere" }         => direct login success
            // or { next: "error", message: "User not found" }            => show error
            const data = response.body;

            if (!data) {
                showInlineError(panelInner, 'Unexpected server response. Please try again.');
                return;
            }

            if (data.next === 'redirect') {
                // immediate success (server says already logged in)
                const go = data.redirectUrl || '/';
                window.location.href = go;
                return;
            }

            if (data.next === 'otp') {
                // Show OTP UI inside panel
                showOtpUI(data.otpToken, email, formEl);
                return;
            }

            // fallback success: server may return redirectUrl
            if (data.redirectUrl) {
                window.location.href = data.redirectUrl;
                return;
            }

            // unknown response: show message if present
            if (data.message) {
                showInlineError(panelInner, data.message);
                return;
            }

            showInlineError(panelInner, 'Login request failed — server did not return expected data.');
        }

        function showOtpUI(otpToken, email, originalForm) {
            if (!panelInner) return;
            // Build OTP markup. Keep minimal and accessible.
            panelInner.innerHTML = `
                <div class="panel-top">
                    <button type="button" class="back-btn" id="otpBackBtn" aria-label="Back">←</button>
                    <button type="button" class="login-close" id="otpCloseBtn" aria-label="Close">×</button>
                </div>
                <div class="login-panel-inner otp-inner">
                    <h2 id="otpTitle">Confirm it's you</h2>
                    <p id="otpDesc">We sent a confirmation code to <strong>${escapeHtml(email)}</strong>. Enter it below. The code is valid for 10 minutes.</p>
                    <form id="otpForm">
                        <label for="otpInput" class="sr-only">Confirmation code</label>
                        <input id="otpInput" name="code" type="text" inputmode="numeric" pattern="[0-9]*" placeholder="Enter code" class="text-input" required autocomplete="one-time-code" />
                        <button type="submit" class="btn btn-primary otp-submit">Confirm login</button>
                        <button type="button" class="btn btn-light otp-resend" id="otpResendBtn">Resend code</button>
                        <div class="panel-error" style="display:none; color:#b00020; margin-top:8px"></div>
                    </form>
                </div>
            `;

            // attach handlers
            const otpForm = panelInner.querySelector('#otpForm');
            const otpBack = panelInner.querySelector('#otpBackBtn');
            const otpClose = panelInner.querySelector('#otpCloseBtn');
            const otpResend = panelInner.querySelector('#otpResendBtn');
            const errorContainer = panelInner.querySelector('.panel-error');

            function showError(msg) { errorContainer.style.display = 'block'; errorContainer.textContent = msg; }
            function clearError() { errorContainer.style.display = 'none'; errorContainer.textContent = ''; }

            if (otpBack) otpBack.addEventListener('click', function (e) { e.preventDefault(); restoreLogin(); });
            if (otpClose) otpClose.addEventListener('click', function (e) { e.preventDefault(); closePanel(); });

            otpForm.addEventListener('submit', async function (e) {
                e.preventDefault();
                clearError();
                const code = otpForm.querySelector('#otpInput').value.trim();
                if (!code) { showError('Please enter the confirmation code.'); return; }

                // Try verify endpoints
                const verifyPaths = [
                    '/Account/VerifyOtp',
                    '/api/account/verify-otp',
                    '/api/auth/verify-otp',
                    '/Account/Login' // fallback if server accepts token+code here
                ];

                const token = getAntiForgeryToken();
                const body = { otpToken: otpToken, code: code, email: email };

                let res = null;
                for (const url of verifyPaths) {
                    res = await safeFetch(url, {
                        method: 'POST',
                        headers: Object.assign({ 'Content-Type': 'application/json' }, token ? { 'RequestVerificationToken': token } : {}),
                        body: JSON.stringify(body),
                        credentials: 'same-origin'
                    });
                    if (!res) continue;
                    // if server responded with 404/500 try next; break otherwise
                    if (res.status === 404 || (res.status >= 500 && res.status !== 400)) continue;
                    break;
                }

                if (!res) {
                    showError('Verification request failed — please try again later.');
                    return;
                }

                if (!res.ok) {
                    const msg = (res.body && (res.body.message || res.body.error)) || 'Verification failed — incorrect code or expired.';
                    showError(msg);
                    return;
                }

                const data = res.body || {};
                if (data.next === 'redirect' || data.success) {
                    const redirectUrl = data.redirectUrl || '/';
                    window.location.href = redirectUrl;
                    return;
                }

                if (data.message) {
                    showError(data.message);
                    return;
                }

                // fallback: success assumed
                window.location.reload();
            });

            otpResend.addEventListener('click', async function (e) {
                e.preventDefault();
                clearError();
                otpResend.disabled = true;
                otpResend.textContent = 'Resending...';
                try {
                    const token = getAntiForgeryToken();
                    const resp = await safeFetch('/Account/ResendOtp', {
                        method: 'POST',
                        headers: Object.assign({ 'Content-Type': 'application/json' }, token ? { 'RequestVerificationToken': token } : {}),
                        body: JSON.stringify({ email, otpToken }),
                        credentials: 'same-origin'
                    });
                    if (!resp || !resp.ok) {
                        const msg = (resp && resp.body && (resp.body.message || resp.body.error)) || 'Resend failed';
                        showError(msg);
                    } else {
                        // server confirmed resent
                        otpResend.textContent = 'Resent — check your email';
                        setTimeout(() => { otpResend.textContent = 'Resend code'; otpResend.disabled = false; }, 4000);
                    }
                } catch (err) {
                    console.error(err);
                    showError('Resend failed — try again later.');
                    otpResend.disabled = false;
                    otpResend.textContent = 'Resend code';
                }
            });
        }

        /* -----------------------
           Helper: escape HTML for safe insertion
           ----------------------- */
        function escapeHtml(unsafe) {
            return (unsafe + '').replace(/[&<"'>]/g, function (m) {
                return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;' })[m];
            });
        }

        /* -----------------------
           Quick links (scroll) helpers
           ----------------------- */
        function attachQuickLinks() {
            const links = document.querySelectorAll('.quick-link');
            if (!links || links.length === 0) return;

            links.forEach(btn => {
                btn.addEventListener('click', function (e) {
                    e.preventDefault();
                    const target = btn.dataset.target || btn.getAttribute('data-scroll') || btn.getAttribute('href');
                    if (!target) return;
                    const selector = target.startsWith('#') ? target : ('#' + target);
                    const el = document.querySelector(selector);
                    if (!el) {
                        console.warn('quick link target not found', selector);
                        return;
                    }
                    // compute offset for fixed header if any
                    const headerOffset = document.querySelector('header') ? document.querySelector('header').offsetHeight + 10 : 0;
                    const top = el.getBoundingClientRect().top + window.pageYOffset - headerOffset;
                    window.scrollTo({ top, behavior: 'smooth' });

                    // If the target is a hidden panel, open it after scroll
                    // (example for trading-conditions quick panels)
                    // Optionally you can expand accordion items here if present.
                });
            });
        }

        /* -----------------------
           Attach top-level login behaviours
           ----------------------- */
        function attachLoginEnhancements() {
            attachPasswordToggle();
            attachClearButtons();
            attachForgotLinkListener();
            attachSocialHandlers();
            attachLoginFormHandler();
        }

        // initial attach (in case panel already contains login HTML on page load)
        attachLoginEnhancements();

        // quick links
        attachQuickLinks();

        /* -----------------------
           Expose helpers globally (optional)
           ----------------------- */
        window.openLoginPanel = openPanel;
        window.closeLoginPanel = closePanel;
        window.restoreLoginPanel = restoreLogin;
        window.attachLoginEnhancements = attachLoginEnhancements;
    });
})();
