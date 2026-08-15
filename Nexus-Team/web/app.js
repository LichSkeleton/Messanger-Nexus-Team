/* ============================================================================
   NexusTeam Web Client
   Vanilla-JS single-page client for the NexusTeam API. Same-origin through
   Nginx (/api + /ws proxied to the .NET server). Feature set mirrors the
   desktop client: chats, groups, attachments, voice messages,
   emoji, AI image generation and settings.
   ============================================================================ */
(function () {
    "use strict";

    // ---- Config ---------------------------------------------------------------
    var API = "/api";
    var WS_PATH = "/ws";
    var USER_KEY = "nexus_user";
    var DEVICE_KEY = "nexus_device_id";
    var THEME_KEY = "nexus_theme";
    var POLLINATIONS = "https://image.pollinations.ai/prompt/";

    var WS = {
        NewMessage: "newMessage", EditMessage: "editMessage", DeleteMessage: "deleteMessage",
        StatusUpdate: "statusUpdate", Heartbeat: "heartbeat", Error: "error",
        Authenticate: "authenticate", MessageReaction: "messageReaction",
        AvatarUpdate: "avatarUpdate", ChatDeleted: "chatDeleted", ChatCreated: "chatCreated",
        ChatUpdated: "chatUpdated"
    };

    // Numeric enum values the server may emit if string-enum conversion is missing.
    var WS_BY_NUMBER = {
        0: "newMessage", 1: "editMessage", 2: "deleteMessage", 3: "messageDelivered",
        4: "messageRead", 5: "typing", 6: "statusUpdate", 7: "heartbeat", 8: "error",
        9: "authenticate", 10: "resume", 11: "messageReaction", 12: "avatarUpdate", 13: "chatDeleted",
        22: "chatCreated", 23: "chatUpdated"
    };

    var EMOJIS = ("😀 😃 😄 😁 😆 😅 🤣 😂 🙂 🙃 😉 😊 😇 🥰 😍 🤩 😘 😗 😚 😙 " +
        "😋 😛 😜 🤪 😝 🤑 🤗 🤭 🤫 🤔 🤐 🤨 😐 😑 😶 😏 😒 🙄 😬 🤥 " +
        "😌 😔 😪 🤤 😴 😷 🤒 🤕 🤢 🤮 🤧 🥵 🥶 🥴 😵 🤯 🤠 🥳 😎 🤓 " +
        "🧐 😕 😟 🙁 ☹️ 😮 😯 😲 😳 🥺 😦 😧 😨 😰 😥 😢 😭 😱 😖 😣 " +
        "😞 😓 😩 😫 🥱 😤 😡 😠 🤬 😈 👿 💀 ☠️ 💩 🤡 👹 👺 👻 👽 👾 " +
        "🤖 😺 😸 😹 😻 😼 😽 🙀 😿 😾 👋 🤚 🖐️ ✋ 🖖 👌 🤌 🤏 ✌️ 🤞 " +
        "🤟 🤘 🤙 👈 👉 👆 🖕 👇 ☝️ 👍 👎 ✊ 👊 🤛 🤜 👏 🙌 👐 🤲 🤝 " +
        "🙏 ✍️ 💅 🤳 💪 🦾 🦿 🦵 🦶 👂 🦻 👃 🧠 👀 👁️ 👅 👄 💋 🩸 " +
        "❤️ 🧡 💛 💚 💙 💜 🖤 🤍 🤎 💔 ❣️ 💕 💞 💓 💗 💖 💘 💝 💟 ☮️ " +
        "🔥 ⭐ 🌟 ✨ 💫 💥 💢 💦 💨 🕳️ 💣 💬 👁️‍🗨️ 🗨️ 🗯️ 💭 💤 " +
        "🎉 🎊 🎈 🎁 🏆 🥇 🥈 🥉 ⚽ 🏀 🏈 ⚾ 🎾 🏐 🏉 🎱 🏓 🏸 🥅 🏒 " +
        "🐶 🐱 🐭 🐹 🐰 🦊 🐻 🐼 🐨 🐯 🦁 🐮 🐷 🐸 🐵 🙈 🙉 🙊 🐒 🐔 " +
        "🐧 🐦 🐤 🐣 🐥 🦆 🦅 🦉 🦇 🐺 🐗 🐴 🦄 🐝 🐛 🦋 🐌 🐞 🐜 🦟 " +
        "🍎 🍐 🍊 🍋 🍌 🍉 🍇 🍓 🫐 🍈 🍒 🍑 🥭 🍍 🥥 🥝 🍅 🍆 🥑 🥦 " +
        "🍕 🍔 🍟 🌭 🥪 🌮 🌯 🥙 🧆 🥚 🍳 🥘 🍲 🥣 🥗 🍿 🧈 🧂 🥨 🥖 " +
        "🍦 🍧 🍨 🍩 🍪 🎂 🍰 🧁 🥧 🍫 🍬 🍭 🍮 🍯 🍼 🥛 ☕ 🍵 🧃 🥤 " +
        "🍺 🍻 🥂 🍷 🥃 🍸 🍹 🧉 🍾 🧊 🚗 🚕 🚙 🚌 🚎 🏎️ 🚓 🚑 🚒 🚐 " +
        "✈️ 🚀 🛸 🚁 ⛵ 🚢 🏠 🏡 🏢 🏣 🏥 🏦 🏨 🏫 🏬 🏭 🏯 🏰 💒 🗼 " +
        "⌚ 📱 💻 ⌨️ 🖥️ 🖨️ 🖱️ 🕹️ 📷 📸 📹 🎥 📞 ☎️ 📺 📻 🎙️ 🔋 🔌 💡 " +
        "🔔 🔕 📣 📢 ✉️ 📧 📩 📨 📦 📫 📪 📬 📭 📮 ✏️ ✒️ 🖊️ 🖋️ 📝 💼 " +
        "✅ ❌ ❓ ❗ ❕ ❔ ‼️ ⁉️ 💯 🔴 🟠 🟡 🟢 🔵 🟣 ⚫ ⚪ 🟤 🔶 🔷").split(" ");

    // ---- State ----------------------------------------------------------------
    function getOrCreateDeviceId() {
        var existing = localStorage.getItem(DEVICE_KEY);
        if (existing) return existing;
        var id = window.crypto && window.crypto.randomUUID ? window.crypto.randomUUID() :
            "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
                var r = Math.random() * 16 | 0; return (c === "x" ? r : (r & 3 | 8)).toString(16);
            });
        localStorage.setItem(DEVICE_KEY, id);
        return id;
    }

    function deviceName() {
        var platform = navigator.userAgentData && navigator.userAgentData.platform || navigator.platform || "Unknown OS";
        var browser = /Edg\//.test(navigator.userAgent) ? "Edge" : /Firefox\//.test(navigator.userAgent) ? "Firefox" : /Chrome\//.test(navigator.userAgent) ? "Chrome" : /Safari\//.test(navigator.userAgent) ? "Safari" : "Browser";
        return browser + " on " + platform;
    }

    var state = {
        token: null,
        me: JSON.parse(localStorage.getItem(USER_KEY) || "null"),
        deviceId: getOrCreateDeviceId(), locked: false, lockStatus: null,
        tabId: window.crypto && window.crypto.randomUUID ? window.crypto.randomUUID() : String(Date.now()) + Math.random(),
        securityMode: "enable", lockTimer: null,
        chats: [], activeChatId: null, socket: null, heartbeat: null,
        seenMessageIds: {}, messagesById: {}, peersById: {},
        lastPreviews: {}, // chatId -> truncated preview text
        unread: {},       // chatId -> true when there are unseen messages
        prefs: { notificationsEnabled: true, soundEnabled: true, theme: "dark" },
        avatarBust: {}, // userId -> version for cache busting
        myStatus: "online", // online | invisible
        folders: [],
        activeFolderId: "all",
        editingFolderId: null,
        editGroupChatId: null,
        editGroupAvatarFile: null,
        optionsChatId: null,
        // new-chat modal
        chatMode: "direct", selectedUsers: {}, users: [],
        // image gen
        lastGenUrl: null,
        // editing
        editingMessageId: null,
        // recording
        recorder: null, recChunks: [], recStream: null, recording: false, recDiscard: false,
        previewObjectUrl: null,
        previewDownloadUrl: null,
        previewFileName: null,
        authMediaCache: {},
        authMediaPending: {}
    };

    // ---- DOM helpers ----------------------------------------------------------
    function $(id) { return document.getElementById(id); }
    function show(el) { el.classList.remove("hidden"); }
    function hide(el) { el.classList.add("hidden"); }

    var toastTimer = null;
    function toast(msg, ms) {
        var t = $("toast"); t.textContent = msg; show(t);
        clearTimeout(toastTimer); toastTimer = setTimeout(function () { hide(t); }, ms || 3200);
    }

    function escapeHtml(s) {
        return String(s == null ? "" : s)
            .replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;").replace(/'/g, "&#39;");
    }

    function avatarUrl(userId) {
        var v = state.avatarBust[userId] ? ("?t=" + state.avatarBust[userId]) : "";
        return API + "/users/avatar/" + encodeURIComponent(userId || "default") + v;
    }

    function resolveAvatarSrc(idOrUrl) {
        if (!idOrUrl) return avatarUrl("default");
        if (idOrUrl.indexOf("http") === 0 || idOrUrl.indexOf("/") === 0) {
            var key = idOrUrl;
            var v = state.avatarBust[key] ? ((idOrUrl.indexOf("?") >= 0 ? "&" : "?") + "t=" + state.avatarBust[key]) : "";
            return idOrUrl + v;
        }
        return avatarUrl(idOrUrl);
    }

    function attachAvatar(img, idOrUrl, name) {
        img.src = resolveAvatarSrc(idOrUrl);
        img.alt = name || "";
        img.onerror = function () {
            img.onerror = null;
            var initial = (name || "?").trim().charAt(0).toUpperCase();
            var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='96' height='96'>" +
                "<rect width='100%' height='100%' fill='%234a3a1e'/>" +
                "<text x='50%' y='54%' font-size='44' fill='%23ff8c00' text-anchor='middle' " +
                "dominant-baseline='middle' font-family='sans-serif'>" + initial + "</text></svg>";
            img.src = "data:image/svg+xml;utf8," + svg.replace(/#/g, "%23");
        };
    }

    function formatTime(iso) {
        if (!iso) return "";
        var d = new Date(iso);
        if (isNaN(d.getTime())) return "";
        return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    }

    function formatSize(bytes) {
        if (!bytes) return "";
        var u = ["B", "KB", "MB", "GB"], i = 0, n = bytes;
        while (n >= 1024 && i < u.length - 1) { n /= 1024; i++; }
        return (Math.round(n * 10) / 10) + " " + u[i];
    }

    // ---- HTTP -----------------------------------------------------------------
    function formatApiError(data, status) {
        if (!data) return "Request failed (" + status + ")";
        if (typeof data === "string") {
            try { data = JSON.parse(data); } catch (e) { return data; }
        }
        // ASP.NET ProblemDetails validation errors: { errors: { Field: ["msg"] } }
        if (data.errors && typeof data.errors === "object") {
            var parts = [];
            Object.keys(data.errors).forEach(function (key) {
                var vals = data.errors[key];
                if (Array.isArray(vals)) parts = parts.concat(vals);
                else if (vals) parts.push(String(vals));
            });
            if (parts.length) {
                var joined = parts.join("; ");
                if (/too large|max request body/i.test(joined)) {
                    return "This file is too large. Maximum size is 100 MB.";
                }
                return joined;
            }
        }
        var fallback = data.error || data.errorMessage || data.title || data.detail ||
            ("Request failed (" + status + ")");
        if (/too large|max request body/i.test(String(fallback))) {
            return "This file is too large. Maximum size is 100 MB.";
        }
        return fallback;
    }

    function request(method, path, body, isForm) {
        var opts = { method: method, headers: {} };
        if (state.token) opts.headers["Authorization"] = "Bearer " + state.token;
        if (body !== undefined) {
            if (isForm) { opts.body = body; }
            else { opts.headers["Content-Type"] = "application/json"; opts.body = JSON.stringify(body); }
        }
        return fetch(API + path, opts).then(function (res) {
            if (res.status === 204 || res.status === 205) {
                if (!res.ok) throw new Error("Request failed (" + res.status + ")");
                return null;
            }
            var ct = (res.headers.get("content-type") || "").toLowerCase();
            var parse = ct.indexOf("json") !== -1 ? res.json() : res.text();
            return parse.then(function (data) {
                if (res.status === 423 || data && data.code === "DEVICE_LOCKED") {
                    showLocked();
                } else if (data && data.code === "DEVICE_SECURITY_UNAVAILABLE") {
                    showSecurityUnavailable();
                } else if (res.status === 401 && path !== "/device-lock/unlock" && path !== "/device-lock/enable" && path !== "/device-lock/disable" && path !== "/device-lock") {
                    logout();
                }
                if (!res.ok) {
                    var error = new Error(formatApiError(data, res.status));
                    error.status = res.status; error.data = data;
                    throw error;
                }
                return data;
            });
        });
    }
    function api(m, p, b) { return request(m, p, b, false); }
    function apiForm(m, p, form) { return request(m, p, form, true); }

    // ---- Auth -----------------------------------------------------------------
    function saveSession(token, user) {
        state.token = token; state.me = user;
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        localStorage.removeItem("nexus_token");
    }

    function login(identifier, password) {
        return api("POST", "/auth/login", {
            usernameOrEmail: identifier, password: password,
            deviceId: state.deviceId, deviceName: deviceName()
        }).then(function (res) {
            saveSession(res.accessToken, res.user);
            return bootstrapDevice();
        });
    }

    function register(displayName, username, email, password) {
        return api("POST", "/auth/register", { displayName: displayName, username: username, email: email, password: password })
            .then(function (res) {
                if (res && res.success === false) throw new Error(res.errorMessage || "Registration failed");
                return login(username, password);
            });
    }

    function logout() {
        if (state.token) {
            fetch(API + "/auth/logout", { method: "POST", headers: { Authorization: "Bearer " + state.token } }).catch(function () { });
        }
        if (state.socket) { try { state.socket.close(); } catch (e) { /* ignore */ } }
        clearInterval(state.heartbeat);
        state.token = null; state.me = null; state.chats = []; state.activeChatId = null;
        localStorage.removeItem("nexus_token"); localStorage.removeItem(USER_KEY);
        document.body.classList.remove("chat-open");
        hide($("lockView")); hide($("chatView")); show($("authView")); document.body.classList.add("auth-mode");
    }

    function restoreSession() {
        return fetch(API + "/auth/refresh", { method: "POST" }).then(function (res) {
            if (!res.ok) throw new Error("No session");
            return res.json();
        }).then(function (res) {
            saveSession(res.accessToken, res.user);
            return bootstrapDevice().catch(function (error) {
                showSecurityUnavailable();
                error.deviceBootstrapFailed = true;
                throw error;
            });
        }).catch(function (error) {
            if (error && error.deviceBootstrapFailed) return;
            state.token = null;
            localStorage.removeItem(USER_KEY);
            hide($("chatView")); hide($("lockView")); show($("authView"));
        });
    }

    function bootstrapDevice() {
        return api("GET", "/device-lock/status").then(function (status) {
            state.lockStatus = status;
            if (status.requiresPinReset) {
                showLocked();
                openSecurityModal("enable");
                $("securityModalTitle").textContent = "Set a new PIN";
            } else if (status.isLocked) showLocked();
            else enterApp();
        });
    }

    // ---- Device lock ---------------------------------------------------------
    var lockChannel = "BroadcastChannel" in window ? new BroadcastChannel("nexus-device-lock") : null;

    function clearSensitiveState() {
        state.chats = []; state.activeChatId = null; state.messagesById = {}; state.peersById = {};
        state.lastPreviews = {}; state.unread = {}; state.users = [];
        ["chatList", "messages"].forEach(function (id) { var el = $(id); if (el) el.replaceChildren(); });
        hide($("activeChat"));
    }

    function showLocked() {
        if (!state.me) { logout(); return; }
        state.locked = true;
        clearTimeout(state.lockTimer);
        if (state.socket) { try { state.socket.close(); } catch (e) { /* ignore */ } }
        clearInterval(state.heartbeat);
        clearSensitiveState();
        hide($("settingsModal")); hide($("securityModal")); hide($("authView")); hide($("chatView"));
        $("lockUsername").textContent = state.me.username || state.me.displayName || "";
        attachAvatar($("lockAvatar"), state.me.id || "default", state.me.displayName || state.me.username);
        $("unlockPin").disabled = false; $("forgotPinBtn").disabled = false;
        $("unlockPin").value = ""; $("unlockError").textContent = "";
        show($("lockView"));
        setTimeout(function () { $("unlockPin").focus(); }, 0);
        if (lockChannel) lockChannel.postMessage({ type: "locked" });
    }

    function showSecurityUnavailable() {
        showLocked();
        $("unlockPin").disabled = true;
        $("forgotPinBtn").disabled = true;
        $("unlockError").textContent = "Security service is unavailable. Your messages remain protected. Try again when the connection returns.";
    }

    function unlockDevice(pin) {
        $("unlockError").textContent = "";
        return api("POST", "/device-lock/unlock", { pin: pin }).then(function () {
            state.locked = false;
            hide($("lockView"));
            if (lockChannel) lockChannel.postMessage({ type: "unlocked" });
            enterApp();
        }).catch(function (err) {
            $("unlockPin").value = "";
            var remaining = err.data && err.data.remainingAttempts;
            $("unlockError").textContent = err.message + (remaining > 0 ? " (" + remaining + " attempts left)" : "");
            if (err.data && err.data.code === "PIN_RESET_REQUIRED") logout();
        });
    }

    function reportActivity() {
        if (!state.token || !state.lockStatus || !state.lockStatus.enabled || state.locked) return Promise.resolve();
        clearTimeout(state.lockTimer);
        if (document.hidden && !state.callActive && state.socket) {
            try { state.socket.close(); } catch (e) { /* ignore */ }
        }
        return api("POST", "/device-lock/activity", {
            tabId: state.tabId,
            isVisible: !document.hidden,
            hasActiveCall: !!state.callActive
        }).then(function () {
            if (document.hidden && !state.callActive) {
                state.lockTimer = setTimeout(checkLockStatus, state.lockStatus.timeoutSeconds * 1000);
            }
        }).catch(function () { });
    }

    function checkLockStatus() {
        if (!state.token || !state.lockStatus || !state.lockStatus.enabled) return Promise.resolve();
        return api("GET", "/device-lock/status").then(function (status) {
            state.lockStatus = status;
            if (status.isLocked) showLocked();
        }).catch(function () { });
    }

    if (lockChannel) {
        lockChannel.onmessage = function (event) {
            if (event.data && event.data.type === "locked" && !state.locked) showLocked();
            if (event.data && event.data.type === "unlocked" && state.locked) {
                state.locked = false; hide($("lockView")); enterApp();
            }
        };
    }

    // ---- Bootstrap ------------------------------------------------------------
    function enterApp() {
        state.locked = false;
        document.body.classList.remove("auth-mode");
        hide($("authView")); show($("chatView"));
        $("myName").textContent = state.me ? state.me.displayName : "";
        attachAvatar($("myAvatar"), state.me ? state.me.id : "default", state.me ? state.me.displayName : "");
        applyTheme(localStorage.getItem(THEME_KEY) || "dark");
        unlockAudio();
        connectSocket();
        loadChats();
        loadFolders();
        loadPreferences();
        loadMyStatus();
        reportActivity();
    }

    // ---- Presence / status ----------------------------------------------------
    function normalizePresence(status) {
        if (status === 1 || status === "1" || status === "online" || status === "Online") return "online";
        if (status === 4 || status === "4" || status === "invisible" || status === "Invisible") return "invisible";
        return "offline";
    }

    function isOnlinePresence(status) {
        return normalizePresence(status) === "online";
    }

    function setDotClass(el, online) {
        if (!el) return;
        el.classList.toggle("online", !!online);
        el.classList.toggle("offline", !online);
    }

    function applyMyStatusUi() {
        var online = state.myStatus === "online";
        var label = online ? "Online" : "Invisible";
        setDotClass($("myStatusBadgeDot"), online);
        var badge = $("myStatusBadge");
        if (badge) {
            badge.title = label + " — tap to change";
            badge.setAttribute("aria-label", label + ", change status");
            badge.classList.toggle("is-online", online);
            badge.classList.toggle("is-invisible", !online);
        }
    }

    function loadMyStatus() {
        return api("GET", "/users/status").then(function (dto) {
            state.myStatus = normalizePresence(dto && dto.status) === "invisible" ? "invisible" : "online";
            applyMyStatusUi();
        }).catch(function () {
            state.myStatus = "online";
            applyMyStatusUi();
        });
    }

    function setMyStatus(mode) {
        var next = mode === "invisible" ? "invisible" : "online";
        var apiStatus = next === "invisible" ? 4 : 1;
        return api("PUT", "/users/status", { status: apiStatus }).then(function () {
            state.myStatus = next;
            applyMyStatusUi();
            closeStatusMenu();
            toast(next === "invisible" ? "You appear offline" : "You are online");
        }).catch(function (e) { toast(e.message); });
    }

    function openStatusMenu() {
        show($("statusMenuMobile"));
        var badge = $("myStatusBadge");
        if (badge) badge.setAttribute("aria-expanded", "true");
    }

    function closeStatusMenu() {
        hide($("statusMenuMobile"));
        var badge = $("myStatusBadge");
        if (badge) badge.setAttribute("aria-expanded", "false");
    }

    function toggleStatusMenu(e) {
        if (e) { e.preventDefault(); e.stopPropagation(); }
        if ($("statusMenuMobile").classList.contains("hidden")) openStatusMenu();
        else closeStatusMenu();
    }
    function isGroup(chat) { return chat.type === "group" || chat.type === 1 || chat.type === "channel" || chat.type === 2; }
    function isDirect(chat) { return chat.type === "directMessage" || chat.type === 0; }

    function isPersonalChat(chat) {
        if (isDirect(chat)) return true;
        var n = (chat.participants && chat.participants.length)
            || (chat.participantIds && chat.participantIds.length)
            || 0;
        return n === 2;
    }

    function otherParticipant(chat) {
        if (!chat.participants || !chat.participants.length) return null;
        for (var i = 0; i < chat.participants.length; i++) {
            if (chat.participants[i].id !== (state.me && state.me.id)) return chat.participants[i];
        }
        return chat.participants[0];
    }

    function truncatePreview(text) {
        var s = String(text == null ? "" : text).trim().replace(/\s+/g, " ");
        if (!s) return "";
        if (s.length <= 15) return s;
        return s.slice(0, 15) + "...";
    }

    function previewFromMessage(m) {
        if (!m) return "";
        if (m.isDeleted) return truncatePreview("Message was deleted");
        var content = (m.content || "").trim();
        var atts = m.attachments || [];
        var hasAudio = atts.some(function (a) {
            var t = a.attachmentType;
            return t === "audio" || t === 2 || (a.contentType || "").indexOf("audio/") === 0;
        });
        var hasImage = atts.some(function (a) { return isImageAttachment(a); });
        var hasVideo = atts.some(function (a) {
            var t = a.attachmentType;
            return t === "video" || t === 1 || (a.contentType || "").indexOf("video/") === 0;
        });
        if (hasAudio || content.indexOf("🎤") === 0) return truncatePreview("🎤 Voice message");
        if (hasImage || isImageUrl(content)) return truncatePreview("🖼 Photo");
        if (hasVideo) return truncatePreview("🎬 Video");
        if (atts.length) return truncatePreview("📎 " + (atts[0].fileName || "File"));
        return truncatePreview(content || "Message");
    }

    function setChatPreview(chatId, msg, markUnread) {
        if (!chatId) return;
        state.lastPreviews[chatId] = previewFromMessage(msg);
        if (markUnread && chatId !== state.activeChatId) state.unread[chatId] = true;
        if (chatId === state.activeChatId) delete state.unread[chatId];
    }

    function chatDisplay(chat) {
        if (isPersonalChat(chat)) {
            var peer = otherParticipant(chat);
            return {
                name: peer ? (peer.displayName || peer.username) : (chat.name || "Direct message"),
                avatarId: peer ? peer.id : "default",
                peer: peer,
                group: false,
                personal: true,
                isOwner: false
            };
        }
        return {
            name: chat.name || "Group chat",
            avatarId: chat.avatarUrl || chat.id,
            peer: null,
            group: true,
            personal: false,
            isOwner: !!(state.me && chat.createdBy === state.me.id)
        };
    }

    function peerPresenceOnline(peerRef) {
        if (!peerRef) return false;
        var peer = state.peersById[peerRef.id] || peerRef;
        return isOnlinePresence(peer.status);
    }

    function loadChats() {
        return api("GET", "/chats").then(function (chats) {
            state.chats = chats || [];
            state.chats.forEach(function (c) {
                (c.participants || []).forEach(function (p) { state.peersById[p.id] = p; });
            });
            renderChatList();
            return loadChatPreviews();
        }).catch(function (e) { toast(e.message); });
    }

    // Fetch the newest message for each chat so the sidebar shows a real preview on load.
    function loadChatPreviews() {
        var chats = state.chats.slice();
        if (!chats.length) return Promise.resolve();

        return Promise.all(chats.map(function (chat) {
            // No activity yet — show the empty-state hint immediately.
            if (!chat.lastMessageAt) {
                state.lastPreviews[chat.id] = "Start chat";
                return Promise.resolve();
            }
            return api("GET", "/chats/" + encodeURIComponent(chat.id) + "/messages?limit=1&offset=0")
                .then(function (messages) {
                    var list = messages || [];
                    if (!list.length) {
                        state.lastPreviews[chat.id] = "Start chat";
                        return;
                    }
                    // API may return newest-first or oldest-first; pick the latest by date.
                    var latest = list.slice().sort(function (a, b) {
                        return new Date(b.createdAt || b.CreatedAt) - new Date(a.createdAt || a.CreatedAt);
                    })[0];
                    state.lastPreviews[chat.id] = previewFromMessage(normalizeMessage(latest));
                })
                .catch(function () {
                    state.lastPreviews[chat.id] = "Start chat";
                });
        })).then(function () {
            renderChatList($("chatSearch").value);
        });
    }

    function renderChatList(filter) {
        var list = $("chatList"); list.innerHTML = "";
        var f = (filter || "").toLowerCase();
        var folder = state.activeFolderId !== "all"
            ? state.folders.find(function (x) { return x.id === state.activeFolderId; })
            : null;
        var folderIds = folder && folder.chatIds ? folder.chatIds : null;

        var visible = state.chats.slice().filter(function (chat) {
            if (folderIds && folderIds.indexOf(chat.id) === -1) return false;
            return true;
        }).sort(function (a, b) {
            return new Date(b.lastMessageAt || b.createdAt) - new Date(a.lastMessageAt || a.createdAt);
        });

        visible.forEach(function (chat) {
            var d = chatDisplay(chat);
            if (f && d.name.toLowerCase().indexOf(f) === -1) return;

            var li = document.createElement("li");
            var hasUnread = !!state.unread[chat.id];
            li.className = "chat-item" + (chat.id === state.activeChatId ? " active" : "") + (hasUnread ? " unread" : "");
            li.dataset.chatId = chat.id;

            var img = document.createElement("img");
            img.className = "avatar";
            attachAvatar(img, d.avatarId, d.name);

            var avatarWrap = document.createElement("span");
            avatarWrap.className = "avatar-wrap" + (d.group ? " is-group" : "");
            avatarWrap.appendChild(img);
            if (d.personal) {
                var online = peerPresenceOnline(d.peer);
                var dot = document.createElement("span");
                dot.className = "status-dot " + (online ? "online" : "offline");
                dot.setAttribute("aria-hidden", "true");
                avatarWrap.appendChild(dot);
            }

            var avatarCol = document.createElement("div");
            avatarCol.className = "chat-item-avatar-col";
            avatarCol.appendChild(avatarWrap);
            if (d.group) {
                var glabel = document.createElement("span");
                glabel.className = "chat-item-group-label";
                glabel.textContent = "Group";
                avatarCol.appendChild(glabel);
            }

            var preview = truncatePreview(state.lastPreviews[chat.id] || "Start chat");
            var statusLine = "";
            if (d.personal) {
                var isOn = peerPresenceOnline(d.peer);
                statusLine = '<div class="chat-item-status ' + (isOn ? "online" : "offline") + '">' +
                    (isOn ? "Online" : "Offline") + "</div>";
            }

            var body = document.createElement("div");
            body.className = "chat-item-body";
            var unreadDot = hasUnread ? '<span class="unread-dot" title="New messages"></span>' : "";
            body.innerHTML =
                '<div class="chat-item-top">' +
                '<span class="chat-item-name">' + escapeHtml(d.name) + '</span>' +
                '<span class="chat-item-meta">' + unreadDot +
                '<span class="chat-item-time">' + formatTime(chat.lastMessageAt) + '</span></span></div>' +
                statusLine +
                '<div class="chat-item-preview">' + escapeHtml(preview) + '</div>';

            li.appendChild(avatarCol); li.appendChild(body);
            li.addEventListener("click", function () { openChat(chat.id); });
            li.addEventListener("contextmenu", function (e) {
                e.preventDefault();
                openChatOptions(chat.id);
            });
            list.appendChild(li);
        });

        var shown = list.children.length;
        $("chatListEmpty").classList.toggle("hidden", shown !== 0 || state.chats.length === 0);
        if (shown === 0 && state.chats.length > 0 && folderIds) {
            $("chatListEmpty").textContent = "No chats in this folder.";
            $("chatListEmpty").classList.remove("hidden");
        } else {
            $("chatListEmpty").textContent = "No chats yet. Tap + to start one.";
        }
    }

    // ---- Conversation ---------------------------------------------------------
    function openChat(chatId) {
        state.activeChatId = chatId;
        state.seenMessageIds = {}; state.messagesById = {};
        delete state.unread[chatId];
        var chat = state.chats.find(function (c) { return c.id === chatId; });
        if (!chat) return;
        var d = chatDisplay(chat);

        hide($("noChatPlaceholder")); show($("activeChat"));
        document.body.classList.add("chat-open");
        hidePicker();
        if (state.recording) cancelRecording();
        if (state.editingMessageId) cancelEdit();

        $("peerName").textContent = d.name;
        attachAvatar($("peerAvatar"), d.avatarId, d.name);
        var peerWrap = $("peerAvatar") && $("peerAvatar").parentElement;
        if (peerWrap && peerWrap.classList.contains("avatar-wrap")) {
            peerWrap.classList.toggle("is-group", !!d.group);
        }
        updatePeerStatus(chat);

        Array.prototype.forEach.call(document.querySelectorAll(".chat-item"), function (el) {
            el.classList.toggle("active", el.dataset.chatId === chatId);
            if (el.dataset.chatId === chatId) el.classList.remove("unread");
        });
        renderChatList($("chatSearch").value);

        $("messages").innerHTML = '<p class="day-divider">Loading…</p>';
        api("GET", "/chats/" + encodeURIComponent(chatId) + "/messages?limit=50&offset=0")
            .then(function (messages) {
                if (state.activeChatId !== chatId) return;
                $("messages").innerHTML = "";
                var sorted = (messages || []).slice().sort(function (a, b) {
                    return new Date(a.createdAt) - new Date(b.createdAt);
                });
                sorted.forEach(function (m) { appendMessage(normalizeMessage(m)); });
                if (sorted.length) setChatPreview(chatId, normalizeMessage(sorted[sorted.length - 1]), false);
                renderChatList($("chatSearch").value);
                scrollToBottom();
            })
            .catch(function (e) { $("messages").innerHTML = '<p class="day-divider">' + escapeHtml(e.message) + '</p>'; });

        $("messageInput").focus();
    }

    function updatePeerStatus(chat) {
        var el = $("peerStatus");
        var dot = $("peerPresenceDot");
        if (isPersonalChat(chat)) {
            var peerRef = otherParticipant(chat);
            var online = peerPresenceOnline(peerRef);
            el.textContent = online ? "Online" : "Offline";
            el.classList.toggle("online", !!online);
            el.classList.remove("is-group");
            if (dot) {
                dot.classList.remove("hidden");
                setDotClass(dot, online);
            }
        } else {
            el.textContent = "Group · " + (chat.participantIds || chat.participants || []).length + " members";
            el.classList.remove("online");
            el.classList.add("is-group");
            if (dot) dot.classList.add("hidden");
        }
    }

    function senderName(m) {
        if (state.me && m.senderId === state.me.id) return "You";
        var p = state.peersById[m.senderId];
        return p ? p.displayName : "Unknown";
    }

    // ----- URL / image detection -----
    function isImageUrl(str) {
        if (!str) return false;
        var s = str.trim();
        if (/\s/.test(s)) return false;
        if (s.indexOf("image.pollinations.ai") !== -1) return true;
        return /^https?:\/\/\S+\.(png|jpe?g|gif|webp|bmp|svg)(\?\S*)?$/i.test(s);
    }

    var MAX_PREVIEW_BYTES = 5 * 1024 * 1024;
    var MAX_FILE_BYTES = 100 * 1024 * 1024;
    var MAX_IMAGE_BYTES = 10 * 1024 * 1024;
    var MSG_MAX_LEN = 480;
    var MSG_MAX_LINES = 10;
    var CODE_EXTS = {
        cs: 1, js: 1, ts: 1, py: 1, java: 1, cpp: 1, c: 1, h: 1, hpp: 1, go: 1, rs: 1,
        php: 1, rb: 1, swift: 1, kt: 1, scala: 1, html: 1, css: 1, json: 1, xml: 1,
        yaml: 1, yml: 1, sql: 1, sh: 1, bat: 1, ps1: 1, jsx: 1, tsx: 1
    };
    var DOC_EXTS = { pdf: 1, docx: 1, txt: 1, md: 1 };
    var IMAGE_EXTS = { jpg: 1, jpeg: 1, png: 1, gif: 1, webp: 1, bmp: 1, svg: 1 };

    function fileExt(name) {
        var i = String(name || "").lastIndexOf(".");
        return i >= 0 ? String(name).slice(i + 1).toLowerCase() : "";
    }

    function isCodeFile(name) {
        return !!CODE_EXTS[fileExt(name)];
    }

    function canPreviewFile(name, type) {
        if (type === "code" || type === 5) return true;
        var ext = fileExt(name);
        return !!(CODE_EXTS[ext] || DOC_EXTS[ext]);
    }

    function needsReadMore(text) {
        if (!text) return false;
        if (text.length > MSG_MAX_LEN) return true;
        return text.split(/\n/).length > MSG_MAX_LINES;
    }

    function collapseText(text) {
        var normalized = String(text).replace(/\r\n/g, "\n");
        var lines = normalized.split("\n");
        var limited = lines.length > MSG_MAX_LINES ? lines.slice(0, MSG_MAX_LINES).join("\n") : normalized;
        if (limited.length <= MSG_MAX_LEN) return limited.replace(/\s+$/, "") + "...";
        var cut = limited.slice(0, MSG_MAX_LEN);
        var lastSpace = cut.lastIndexOf(" ");
        if (lastSpace >= MSG_MAX_LEN / 2) cut = cut.slice(0, lastSpace);
        return cut.replace(/\s+$/, "") + "...";
    }

    function renderMessageText(content, cls) {
        if (!needsReadMore(content)) {
            return '<span class="' + cls + '">' + escapeHtml(content) + '</span>';
        }
        return '<span class="' + cls + ' msg-body">' + escapeHtml(collapseText(content)) + '</span>' +
            '<button type="button" class="read-more-btn" data-full="' + escapeHtml(content) + '">Read more</button>';
    }

    function fetchAuthBlob(url) {
        var opts = { headers: {} };
        if (state.token) opts.headers["Authorization"] = "Bearer " + state.token;
        return fetch(url, opts).then(function (res) {
            if (res.status === 401) { logout(); throw new Error("Session expired. Please sign in again."); }
            if (!res.ok) throw new Error("Failed to load file (" + res.status + ")");
            return res.blob();
        });
    }

    function loadAuthUrl(url) {
        if (!url) return Promise.reject(new Error("Missing file URL"));
        if (url.indexOf("blob:") === 0 || url.indexOf("data:") === 0) return Promise.resolve(url);
        if (state.authMediaCache[url]) return Promise.resolve(state.authMediaCache[url]);
        if (state.authMediaPending[url]) return state.authMediaPending[url];
        var pending = fetchAuthBlob(url).then(function (blob) {
            var obj = URL.createObjectURL(blob);
            state.authMediaCache[url] = obj;
            delete state.authMediaPending[url];
            return obj;
        }).catch(function (err) {
            delete state.authMediaPending[url];
            throw err;
        });
        state.authMediaPending[url] = pending;
        return pending;
    }

    function hydrateAuthMedia(root) {
        var scope = root || document;
        var nodes = scope.querySelectorAll ? scope.querySelectorAll("[data-auth-src]") : [];
        Array.prototype.forEach.call(nodes, function (el) {
            var url = el.getAttribute("data-auth-src");
            if (!url || el.getAttribute("data-hydrated") === "1") return;
            el.setAttribute("data-hydrated", "1");
            loadAuthUrl(url).then(function (objUrl) {
                el.src = objUrl;
                if (el.tagName === "AUDIO" || el.tagName === "VIDEO") {
                    try { el.load(); } catch (e) { /* ignore */ }
                }
            }).catch(function () {
                el.classList.add("media-failed");
            });
        });
    }

    function isImageAttachment(a) {
        var type = a.attachmentType;
        if (type === "image" || type === 0 || type === "Image") return true;
        if ((a.contentType || "").toLowerCase().indexOf("image/") === 0) return true;
        return !!IMAGE_EXTS[fileExt(a.fileName)];
    }

    function triggerBlobDownload(blob, fileName) {
        var obj = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = obj;
        a.download = fileName || "file";
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(function () { URL.revokeObjectURL(obj); }, 1500);
    }

    function downloadAttachment(url, fileName) {
        toast("Downloading…");
        fetchAuthBlob(url)
            .then(function (blob) { triggerBlobDownload(blob, fileName); })
            .catch(function (err) { toast(err.message || "Download failed"); });
    }

    function xmlToPlainText(xml) {
        return String(xml)
            .replace(/<w:tab\/>/g, "\t")
            .replace(/<w:br[^/]*\/>/g, "\n")
            .replace(/<\/w:p>/g, "\n")
            .replace(/<[^>]+>/g, "")
            .replace(/&amp;/g, "&")
            .replace(/&lt;/g, "<")
            .replace(/&gt;/g, ">")
            .replace(/&quot;/g, '"')
            .replace(/&#39;/g, "'")
            .replace(/\n{3,}/g, "\n\n")
            .trim();
    }

    function findZipEntry(buffer, entryName) {
        var view = new DataView(buffer);
        var bytes = new Uint8Array(buffer);
        var offset = 0;
        while (offset + 30 < bytes.length) {
            if (view.getUint32(offset, true) !== 0x04034b50) break;
            var method = view.getUint16(offset + 8, true);
            var compSize = view.getUint32(offset + 18, true);
            var nameLen = view.getUint16(offset + 26, true);
            var extraLen = view.getUint16(offset + 28, true);
            var name = new TextDecoder("utf-8").decode(bytes.subarray(offset + 30, offset + 30 + nameLen));
            var dataStart = offset + 30 + nameLen + extraLen;
            if (name === entryName && compSize > 0) {
                return { method: method, data: bytes.subarray(dataStart, dataStart + compSize) };
            }
            offset = dataStart + compSize;
        }
        return null;
    }

    function inflateRaw(uint8) {
        if (typeof DecompressionStream === "undefined") {
            return Promise.reject(new Error("This browser cannot unpack Word files."));
        }
        var stream = new Blob([uint8]).stream().pipeThrough(new DecompressionStream("deflate-raw"));
        return new Response(stream).arrayBuffer();
    }

    function extractDocxText(arrayBuffer) {
        var entry = findZipEntry(arrayBuffer, "word/document.xml");
        if (!entry) return Promise.reject(new Error("Could not read this Word file."));
        var ready = entry.method === 0
            ? Promise.resolve(entry.data.buffer.slice(entry.data.byteOffset, entry.data.byteOffset + entry.data.byteLength))
            : inflateRaw(entry.data);
        return ready.then(function (buf) {
            var xml = new TextDecoder("utf-8").decode(buf);
            var text = xmlToPlainText(xml);
            return text || "(Empty document)";
        });
    }

    function revokePreviewUrl() {
        if (state.previewObjectUrl) {
            URL.revokeObjectURL(state.previewObjectUrl);
            state.previewObjectUrl = null;
        }
    }

    function closeFilePreview() {
        hide($("filePreviewModal"));
        revokePreviewUrl();
        $("filePreviewBody").innerHTML = "";
        hide($("filePreviewBody"));
        hide($("filePreviewNotice"));
        $("filePreviewNotice").textContent = "";
        $("filePreviewModal").querySelector(".file-preview-modal").classList.remove("compact");
        state.previewDownloadUrl = null;
        state.previewFileName = null;
    }

    function showPreviewNotice(text) {
        var el = $("filePreviewNotice");
        el.textContent = text;
        show(el);
        hide($("filePreviewBody"));
        $("filePreviewBody").innerHTML = "";
        $("filePreviewModal").querySelector(".file-preview-modal").classList.add("compact");
    }

    function setPreviewBodyHtml(html) {
        $("filePreviewBody").innerHTML = html;
        if (html) {
            show($("filePreviewBody"));
            $("filePreviewModal").querySelector(".file-preview-modal").classList.remove("compact");
        } else {
            hide($("filePreviewBody"));
        }
    }

    function openFilePreview(url, fileName, fileSize) {
        state.previewDownloadUrl = url;
        state.previewFileName = fileName;
        $("filePreviewTitle").textContent = fileName || "Preview";
        hide($("filePreviewNotice"));
        $("filePreviewNotice").textContent = "";
        show($("filePreviewModal"));

        if (Number(fileSize) > MAX_PREVIEW_BYTES) {
            showPreviewNotice(
                "This file is too large to preview (" + formatSize(fileSize) +
                "). Preview is limited to " + formatSize(MAX_PREVIEW_BYTES) +
                ". Please download the file instead."
            );
            return;
        }

        setPreviewBodyHtml('<div class="file-preview-loading"><div class="spinner"></div></div>');

        var ext = fileExt(fileName);
        fetchAuthBlob(url).then(function (blob) {
            if (ext === "pdf") {
                revokePreviewUrl();
                var pdfBlob = blob.type && blob.type.indexOf("pdf") !== -1
                    ? blob
                    : new Blob([blob], { type: "application/pdf" });
                state.previewObjectUrl = URL.createObjectURL(pdfBlob);
                setPreviewBodyHtml('<iframe class="file-preview-frame" title="PDF preview" src="' +
                    state.previewObjectUrl + '"></iframe>');
                return;
            }
            if (ext === "docx") {
                return blob.arrayBuffer().then(extractDocxText).then(function (text) {
                    setPreviewBodyHtml('<pre class="file-preview-code"></pre>');
                    $("filePreviewBody").querySelector("pre").textContent = text;
                });
            }
            return blob.text().then(function (text) {
                setPreviewBodyHtml('<pre class="file-preview-code"></pre>');
                $("filePreviewBody").querySelector("pre").textContent = text || "(Empty file)";
            });
        }).catch(function (err) {
            showPreviewNotice(err.message || "Failed to preview this file.");
        });
    }

    function toggleReadMore(btn) {
        var full = btn.getAttribute("data-full") || "";
        var textEl = btn.previousElementSibling;
        if (!textEl) return;
        if (btn.getAttribute("data-expanded") === "1") {
            textEl.textContent = collapseText(full);
            btn.textContent = "Read more";
            btn.setAttribute("data-expanded", "0");
        } else {
            textEl.textContent = full;
            btn.textContent = "Show less";
            btn.setAttribute("data-expanded", "1");
        }
    }

    function attachmentHtml(a) {
        var type = a.attachmentType;
        var isImg = isImageAttachment(a);
        var isAud = type === "audio" || type === 2 || type === "Audio" || (a.contentType || "").indexOf("audio/") === 0;
        var isVid = type === "video" || type === 1 || type === "Video" || (a.contentType || "").indexOf("video/") === 0;
        var url = a.downloadUrl || (API + "/attachments/download/" + a.id);
        var thumb = a.thumbnailUrl || url;
        if (isImg) {
            return '<img class="att att-image" data-auth-src="' + escapeHtml(thumb) +
                '" data-full="' + escapeHtml(url) + '" alt="' + escapeHtml(a.fileName || "Image") + '" />';
        }
        if (isAud) {
            return '<audio class="att att-audio" controls preload="none" data-auth-src="' + escapeHtml(url) + '"></audio>';
        }
        if (isVid) {
            return '<video class="att att-video" controls preload="metadata" data-auth-src="' + escapeHtml(url) + '"></video>';
        }
        var ico = type === "archive" || type === 4 ? "🗜️" : (isCodeFile(a.fileName) || type === "code" || type === 5 ? "💻" : "📄");
        var html = '<div class="att att-file">' +
            '<span class="file-ico">' + ico + '</span>' +
            '<span class="file-meta">' +
            '<span class="file-name">' + escapeHtml(a.fileName || "File") + '</span>' +
            '<span class="file-size">' + escapeHtml(formatSize(a.fileSize)) + '</span>' +
            '</span><span class="file-actions">';
        if (canPreviewFile(a.fileName, type)) {
            html += '<button type="button" class="att-btn att-preview-btn"' +
                ' data-att-url="' + escapeHtml(url) + '"' +
                ' data-att-name="' + escapeHtml(a.fileName || "File") + '"' +
                ' data-att-size="' + Number(a.fileSize || 0) + '">Preview</button>';
        }
        html += '<button type="button" class="att-btn att-download-btn"' +
            ' data-att-url="' + escapeHtml(url) + '"' +
            ' data-att-name="' + escapeHtml(a.fileName || "File") + '">Download</button>';
        html += '</span></div>';
        return html;
    }

    function messageHasMedia(m) {
        var content = (m.content || "").trim();
        if (isImageUrl(content)) return true;
        var atts = m.attachments || [];
        return atts.some(function (a) {
            var t = a.attachmentType;
            var ct = a.contentType || "";
            return isImageAttachment(a) || t === "audio" || t === 2 || t === "video" || t === 1 ||
                ct.indexOf("audio/") === 0 || ct.indexOf("video/") === 0 ||
                !!a.fileName; // any file attachment → delete-only
        });
    }

    function isOwnMessage(m) {
        return !!(state.me && m && m.senderId === state.me.id);
    }

    function canEditMessage(m) {
        return isOwnMessage(m) && !m.isDeleted && !messageHasMedia(m);
    }

    function canDeleteMessage(m) {
        return isOwnMessage(m) && !m.isDeleted;
    }

    function buildBubbleInner(m, chat) {
        var html = "";
        var mine = isOwnMessage(m);

        if (m.isDeleted) {
            html += '<span class="deleted-text">Message was deleted</span>';
            html += '<span class="meta">' + formatTime(m.createdAt) + '</span>';
            return html;
        }

        var showSender = chat && !isDirect(chat) && !mine;
        // Sender name is rendered under the avatar in appendMessage; keep empty here.
        if (showSender) html += '<span class="sender">' + escapeHtml(senderName(m)) + '</span>';

        var content = (m.content || "").trim();
        var hasAtt = m.attachments && m.attachments.length;
        var hasAudio = hasAtt && m.attachments.some(function (a) {
            var t = a.attachmentType;
            return t === "audio" || t === 2 || (a.contentType || "").indexOf("audio/") === 0;
        });

        // Attachments first so voice caption / text sits below the player
        if (hasAtt) { m.attachments.forEach(function (a) { html += attachmentHtml(a); }); }

        if (content && isImageUrl(content) && !hasAtt) {
            html += '<img class="att att-image" src="' + escapeHtml(content) + '" data-full="' + escapeHtml(content) + '" alt="" />';
        } else if (content) {
            var cls = hasAudio ? "text voice-caption" : "text";
            html += renderMessageText(content, cls);
        }

        if (m.editedAt) html += '<span class="edited-label">Edited</span>';
        html += '<span class="meta">' + formatTime(m.createdAt) + '</span>';

        if (mine) {
            html += '<div class="msg-actions">';
            if (canEditMessage(m)) {
                html += '<button type="button" class="msg-action" data-action="edit" title="Edit">Edit</button>';
            }
            if (canDeleteMessage(m)) {
                html += '<button type="button" class="msg-action danger" data-action="delete" title="Delete">Delete</button>';
            }
            html += '</div>';
        }

        return html;
    }

    function appendMessage(m) {
        if (!m || !m.id) return;
        if (m.chatId && state.activeChatId && m.chatId !== state.activeChatId) return;
        if (state.seenMessageIds[m.id]) { updateMessage(m); return; }
        state.seenMessageIds[m.id] = true;
        state.messagesById[m.id] = m;

        var chat = state.chats.find(function (c) { return c.id === state.activeChatId; });
        var mine = isOwnMessage(m);

        var row = document.createElement("div");
        row.className = "msg-row" + (mine ? " me" : "") + (m.isDeleted ? " deleted" : "");
        row.dataset.msgId = m.id;

        if (!mine && chat && !isDirect(chat)) {
            var col = document.createElement("div");
            col.className = "msg-avatar-col";
            var av = document.createElement("img");
            av.className = "msg-avatar";
            attachAvatar(av, m.senderId, senderName(m));
            col.appendChild(av);
            var under = document.createElement("span");
            under.className = "msg-sender-under";
            under.textContent = senderName(m);
            col.appendChild(under);
            row.appendChild(col);
        }

        var bubble = document.createElement("div");
        bubble.className = "bubble";
        bubble.innerHTML = buildBubbleInner(m, chat);
        row.appendChild(bubble);
        $("messages").appendChild(row);
        hydrateAuthMedia(row);
    }

    function updateMessage(m) {
        if (!m || !m.id) return;
        var prev = state.messagesById[m.id];
        if (prev && (!m.attachments || !m.attachments.length) && prev.attachments && prev.attachments.length) {
            m.attachments = prev.attachments;
        }
        if (prev && m.isDeleted === undefined) m.isDeleted = prev.isDeleted;
        state.messagesById[m.id] = m;
        var row = document.querySelector('.msg-row[data-msg-id="' + m.id + '"]');
        if (!row) { appendMessage(m); return; }
        var chat = state.chats.find(function (c) { return c.id === state.activeChatId; });
        row.classList.toggle("deleted", !!m.isDeleted);
        var bubble = row.querySelector(".bubble");
        if (bubble) {
            bubble.innerHTML = buildBubbleInner(m, chat);
            hydrateAuthMedia(bubble);
        }
        if (m.chatId === state.activeChatId || (prev && prev.chatId === state.activeChatId)) {
            bumpChat(m.chatId || (prev && prev.chatId), m, false);
        }
    }

    function markMessageDeleted(messageId) {
        var m = state.messagesById[messageId];
        if (!m) {
            // Message may not be in the open chat — still update local cache if present later
            return;
        }
        m.isDeleted = true;
        m.content = "";
        m.attachments = [];
        m.editedAt = null;
        updateMessage(m);
        if (state.editingMessageId === messageId) cancelEdit();
        bumpChat(m.chatId, { content: "Message was deleted", createdAt: m.createdAt, chatId: m.chatId }, false);
    }

    function removeMessage(messageId) {
        markMessageDeleted(messageId);
    }

    function scrollToBottom() { var m = $("messages"); m.scrollTop = m.scrollHeight; }

    // ---- Personal folders ----------------------------------------------------
    function loadFolders() {
        return api("GET", "/folders").then(function (folders) {
            state.folders = folders || [];
            if (state.activeFolderId !== "all" &&
                !state.folders.some(function (f) { return f.id === state.activeFolderId; })) {
                state.activeFolderId = "all";
            }
            renderFolderBar();
            renderChatList($("chatSearch").value);
        }).catch(function () {
            state.folders = [];
            renderFolderBar();
        });
    }

    function renderFolderBar() {
        var host = $("folderChips");
        if (!host) return;
        host.innerHTML = "";
        state.folders.forEach(function (folder) {
            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "folder-chip" + (folder.id === state.activeFolderId ? " active" : "");
            btn.dataset.folderId = folder.id;
            btn.textContent = folder.name;
            btn.title = "Personal folder — right-click to edit/delete";
            btn.addEventListener("click", function () {
                state.activeFolderId = folder.id;
                renderFolderBar();
                renderChatList($("chatSearch").value);
            });
            btn.addEventListener("contextmenu", function (e) {
                e.preventDefault();
                openFolderModal(folder);
            });
            host.appendChild(btn);
        });
        var allBtn = document.querySelector('.folder-chip[data-folder-id="all"]');
        if (allBtn) allBtn.classList.toggle("active", state.activeFolderId === "all");
    }

    function openFolderModal(folder) {
        state.editingFolderId = folder ? folder.id : null;
        $("folderModalTitle").textContent = folder ? "Edit folder" : "New folder";
        $("folderNameInput").value = folder ? folder.name : "";
        $("deleteFolderBtn").classList.toggle("hidden", !folder);
        var pick = $("folderChatPickList");
        pick.innerHTML = "";
        var selected = {};
        (folder && folder.chatIds ? folder.chatIds : []).forEach(function (id) { selected[id] = true; });
        state.chats.forEach(function (chat) {
            var d = chatDisplay(chat);
            var li = document.createElement("li");
            li.className = "user-item";
            li.innerHTML = '<label style="display:flex;align-items:center;gap:10px;width:100%;cursor:pointer;">' +
                '<input type="checkbox" data-chat-id="' + escapeHtml(chat.id) + '"' +
                (selected[chat.id] ? " checked" : "") + ' />' +
                '<span>' + escapeHtml(d.name) + (d.group ? " · Group" : "") + "</span></label>";
            pick.appendChild(li);
        });
        show($("folderModal"));
        $("folderNameInput").focus();
    }

    function saveFolder() {
        var name = ($("folderNameInput").value || "").trim();
        if (!name) { toast("Folder name is required"); return; }
        var chatIds = [];
        Array.prototype.forEach.call($("folderChatPickList").querySelectorAll("input[type=checkbox]"), function (cb) {
            if (cb.checked) chatIds.push(cb.getAttribute("data-chat-id"));
        });
        var body = { name: name, chatIds: chatIds };
        var req = state.editingFolderId
            ? api("PUT", "/folders/" + encodeURIComponent(state.editingFolderId), body)
            : api("POST", "/folders", body);
        req.then(function () {
            hide($("folderModal"));
            toast(state.editingFolderId ? "Folder updated" : "Folder created");
            state.editingFolderId = null;
            return loadFolders();
        }).catch(function (e) { toast(e.message); });
    }

    // ---- Group leave / edit / chat options -----------------------------------
    function openChatOptions(chatId) {
        var chat = state.chats.find(function (c) { return c.id === chatId; });
        if (!chat) return;
        state.optionsChatId = chatId;
        var d = chatDisplay(chat);
        $("optEditGroup").classList.toggle("hidden", !(d.group && d.isOwner));
        $("optLeaveGroup").classList.toggle("hidden", !d.group);
        show($("chatOptionsModal"));
    }

    function leaveGroup(chatId) {
        var chat = state.chats.find(function (c) { return c.id === chatId; });
        if (!chat) return;
        var members = (chat.participantIds || chat.ParticipantIds || chat.participants || []).length;
        var isLast = members <= 1;
        openConfirm({
            title: isLast ? "Delete group?" : "Leave group?",
            text: isLast
                ? "You are the last member. Leaving will permanently delete \"" + (chat.name || "group") + "\" and all its messages."
                : "Leave \"" + (chat.name || "group") + "\"? You can be added again later.",
            okText: isLast ? "Delete" : "Leave",
            onConfirm: function () {
                api("POST", "/chats/" + encodeURIComponent(chatId) + "/leave")
                    .then(function () {
                        state.chats = state.chats.filter(function (c) { return c.id !== chatId; });
                        if (state.activeChatId === chatId) {
                            state.activeChatId = null;
                            document.body.classList.remove("chat-open");
                            hide($("activeChat")); show($("noChatPlaceholder"));
                        }
                        toast(isLast ? "Group deleted" : "You left the group");
                        renderChatList($("chatSearch").value);
                        return loadFolders();
                    })
                    .catch(function (e) { toast(e.message); });
            }
        });
    }

    function openEditGroup(chatId) {
        var chat = state.chats.find(function (c) { return c.id === chatId; });
        if (!chat) return;
        var d = chatDisplay(chat);
        if (!d.isOwner) { toast("Only the group owner can edit"); return; }
        state.editGroupChatId = chatId;
        state.editGroupAvatarFile = null;
        $("editGroupName").value = chat.name || "";
        attachAvatar($("editGroupAvatar"), d.avatarId, d.name);
        show($("editGroupModal"));
    }

    function saveGroupEdits() {
        var chatId = state.editGroupChatId;
        if (!chatId) return;
        var name = ($("editGroupName").value || "").trim();
        if (!name) { toast("Group name is required"); return; }

        api("PUT", "/chats/" + encodeURIComponent(chatId), { name: name })
            .then(function (updated) {
                var file = state.editGroupAvatarFile;
                if (!file) return updated;
                var form = new FormData();
                form.append("file", file);
                return apiForm("POST", "/chats/" + encodeURIComponent(chatId) + "/avatar", form);
            })
            .then(function (updated) {
                var idx = state.chats.findIndex(function (c) { return c.id === chatId; });
                if (idx >= 0 && updated) {
                    state.chats[idx] = updated;
                    if (updated.avatarUrl) state.avatarBust[updated.avatarUrl] = Date.now();
                }
                hide($("editGroupModal"));
                state.editGroupChatId = null;
                state.editGroupAvatarFile = null;
                toast("Group updated");
                if (state.activeChatId === chatId) openChat(chatId);
                else renderChatList($("chatSearch").value);
            })
            .catch(function (e) { toast(e.message); });
    }

    function deleteChat(chatId) {
        var chat = state.chats.find(function (c) { return c.id === chatId; });
        if (!chat) return;
        openConfirm({
            title: "Delete chat?",
            text: "Permanently delete \"" + (chatDisplay(chat).name) + "\" and all messages for everyone?",
            okText: "Delete",
            onConfirm: function () {
                api("DELETE", "/chats/" + encodeURIComponent(chatId))
                    .then(function () {
                        state.chats = state.chats.filter(function (c) { return c.id !== chatId; });
                        if (state.activeChatId === chatId) {
                            state.activeChatId = null;
                            document.body.classList.remove("chat-open");
                            hide($("activeChat")); show($("noChatPlaceholder"));
                        }
                        toast("Chat deleted");
                        renderChatList($("chatSearch").value);
                        return loadFolders();
                    })
                    .catch(function (e) { toast(e.message); });
            }
        });
    }

    function openAddToFolder(chatId) {
        var list = $("addToFolderList");
        list.innerHTML = "";
        if (!state.folders.length) {
            show($("addToFolderEmpty"));
            hide(list);
        } else {
            hide($("addToFolderEmpty"));
            show(list);
            state.folders.forEach(function (folder) {
                var inFolder = (folder.chatIds || []).indexOf(chatId) >= 0;
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "chat-option";
                btn.textContent = (inFolder ? "✓ " : "") + folder.name + (inFolder ? " (remove)" : "");
                btn.addEventListener("click", function () {
                    var ids = (folder.chatIds || []).slice();
                    if (inFolder) ids = ids.filter(function (id) { return id !== chatId; });
                    else ids.push(chatId);
                    api("PUT", "/folders/" + encodeURIComponent(folder.id), { name: folder.name, chatIds: ids })
                        .then(function () {
                            hide($("addToFolderModal"));
                            toast(inFolder ? "Removed from folder" : "Added to folder");
                            return loadFolders();
                        })
                        .catch(function (e) { toast(e.message); });
                });
                list.appendChild(btn);
            });
        }
        show($("addToFolderModal"));
    }

    // ---- Edit / delete via WebSocket ----------------------------------------
    function wsSend(envelope) {
        if (!state.socket || state.socket.readyState !== WebSocket.OPEN) {
            toast("Not connected. Try again in a moment.");
            return false;
        }
        state.socket.send(JSON.stringify(envelope));
        return true;
    }

    function startEdit(messageId) {
        var m = state.messagesById[messageId];
        if (!m || !canEditMessage(m)) return;
        if (state.recording) cancelRecording();
        state.editingMessageId = messageId;
        $("messageInput").value = m.content || "";
        $("messageInput").disabled = false;
        $("messageInput").placeholder = "Edit message…";
        $("messageInput").focus();
        showStrip("✏️", "Editing message — tap ➤ to save", false);
        document.querySelector(".send-btn").title = "Save edit";
        Array.prototype.forEach.call(document.querySelectorAll(".msg-row.editing"), function (el) {
            el.classList.remove("editing");
        });
        var row = document.querySelector('.msg-row[data-msg-id="' + messageId + '"]');
        if (row) row.classList.add("editing");
    }

    function cancelEdit() {
        state.editingMessageId = null;
        $("messageInput").value = "";
        $("messageInput").placeholder = "Message";
        document.querySelector(".send-btn").title = "Send";
        hideStrip();
        Array.prototype.forEach.call(document.querySelectorAll(".msg-row.editing"), function (el) {
            el.classList.remove("editing");
        });
    }

    function submitEdit(content) {
        var id = state.editingMessageId;
        content = (content || "").trim();
        if (!id || !content) { toast("Message cannot be empty"); return; }
        var ok = wsSend({
            type: WS.EditMessage,
            messageId: id,
            payload: { messageId: id, content: content }
        });
        if (!ok) return;
        // Optimistic UI update
        var m = state.messagesById[id];
        if (m) {
            m.content = content;
            m.editedAt = new Date().toISOString();
            updateMessage(m);
        }
        cancelEdit();
        toast("Message updated");
    }

    function deleteOwnMessage(messageId) {
        var m = state.messagesById[messageId];
        if (!m || !canDeleteMessage(m)) return;
        openConfirm({
            title: "Delete message?",
            text: "This message will be removed for everyone in the chat.",
            confirmLabel: "Delete",
            onConfirm: function () {
                var ok = wsSend({
                    type: WS.DeleteMessage,
                    messageId: messageId,
                    payload: { messageId: messageId }
                });
                if (!ok) return;
                markMessageDeleted(messageId);
                toast("Message deleted");
            }
        });
    }

    var confirmCallback = null;
    function openConfirm(opts) {
        opts = opts || {};
        confirmCallback = typeof opts.onConfirm === "function" ? opts.onConfirm : null;
        $("confirmTitle").textContent = opts.title || "Are you sure?";
        $("confirmText").textContent = opts.text || "";
        $("confirmOk").textContent = opts.confirmLabel || "Confirm";
        show($("confirmModal"));
        $("confirmOk").focus();
    }
    function closeConfirm() {
        hide($("confirmModal"));
        confirmCallback = null;
    }
    function acceptConfirm() {
        var cb = confirmCallback;
        closeConfirm();
        if (cb) cb();
    }

    function bumpChat(chatId, msg, markUnread) {
        if (!chatId) return;
        var chat = state.chats.find(function (c) { return c.id === chatId; });
        if (!chat) {
            // New chat we don't have yet — refresh the list.
            loadChats();
            return;
        }
        chat.lastMessageAt = (msg && (msg.createdAt || msg.CreatedAt)) || new Date().toISOString();
        if (msg) setChatPreview(chatId, msg, !!markUnread);
        renderChatList($("chatSearch").value);
    }

    // ---- Sending --------------------------------------------------------------
    // FluentValidation requires chatId in the body BEFORE the controller copies
    // it from the route, so both must be present.
    function postMessage(content, chatId) {
        var id = chatId || state.activeChatId;
        if (!id) return Promise.reject(new Error("Open a chat first"));
        return api("POST", "/chats/" + encodeURIComponent(id) + "/messages", {
            chatId: id,
            content: content == null ? "" : String(content)
        });
    }

    function sendText(content) {
        content = content.trim();
        if (!content || !state.activeChatId) return;
        var chatId = state.activeChatId;
        postMessage(content, chatId).then(function (msg) {
            appendMessage(msg); scrollToBottom(); bumpChat(msg.chatId || chatId, msg);
        }).catch(function (e) { toast(e.message); });
    }

    function isImageFile(file) {
        if (!file) return false;
        var type = (file.type || "").toLowerCase();
        if (type.indexOf("image/") === 0) return true;
        return !!IMAGE_EXTS[fileExt(file.name)];
    }

    function fileSizeLimit(file) {
        return isImageFile(file) ? MAX_IMAGE_BYTES : MAX_FILE_BYTES;
    }

    function fileTooLargeMessage(file) {
        var limit = fileSizeLimit(file);
        var label = isImageFile(file) ? "Images" : "Files";
        return label + " cannot be larger than " + formatSize(limit) +
            ". For bigger files, send a link to external storage (Google Drive, OneDrive, etc.).";
    }

    function discardPlaceholderMessage(msg) {
        if (!msg || !msg.id) return;
        wsSend({
            type: WS.DeleteMessage,
            messageId: msg.id,
            payload: { messageId: msg.id }
        });
        delete state.seenMessageIds[msg.id];
        delete state.messagesById[msg.id];
        var row = document.querySelector('.msg-row[data-msg-id="' + msg.id + '"]');
        if (row) row.remove();
    }

    // Attachment/voice: create a message then upload the file to it.
    function sendFile(file, caption) {
        if (!state.activeChatId) { toast("Open a chat first"); return; }
        if (!file) return;
        if (file.size > fileSizeLimit(file)) {
            toast(fileTooLargeMessage(file), 7000);
            return;
        }

        var chatId = state.activeChatId;
        toast("Uploading " + file.name + "…");
        postMessage(caption || " ", chatId).then(function (msg) {
            appendMessage(msg); scrollToBottom(); bumpChat(chatId, msg, false);
            var form = new FormData();
            form.append("file", file, file.name);
            form.append("messageId", msg.id);
            return apiForm("POST", "/attachments/upload", form).then(function (att) {
                msg.attachments = (msg.attachments || []).concat([att]);
                updateMessage(msg); scrollToBottom();
                bumpChat(chatId, msg, false);
                toast("Sent");
            }).catch(function (err) {
                discardPlaceholderMessage(msg);
                throw err;
            });
        }).catch(function (e) {
            toast(e.message || "Upload failed", 7000);
        });
    }

    // ---- WebSocket ------------------------------------------------------------
    function normalizeType(raw) {
        if (raw == null) return "";
        if (typeof raw === "number") return WS_BY_NUMBER[raw] || String(raw);
        var s = String(raw);
        // Accept PascalCase enum names too ("NewMessage")
        if (s.length && s[0] === s[0].toUpperCase() && s.indexOf(" ") === -1) {
            return s.charAt(0).toLowerCase() + s.slice(1);
        }
        return s;
    }

    function pick(obj) {
        var keys = Array.prototype.slice.call(arguments, 1);
        if (!obj) return undefined;
        for (var i = 0; i < keys.length; i++) {
            if (obj[keys[i]] !== undefined && obj[keys[i]] !== null) return obj[keys[i]];
        }
        return undefined;
    }

    function normalizeAttachment(a) {
        if (!a || typeof a !== "object") return a;
        return {
            id: pick(a, "id", "Id"),
            messageId: pick(a, "messageId", "MessageId"),
            fileName: pick(a, "fileName", "FileName"),
            fileSize: pick(a, "fileSize", "FileSize"),
            contentType: pick(a, "contentType", "ContentType"),
            attachmentType: pick(a, "attachmentType", "AttachmentType"),
            downloadUrl: pick(a, "downloadUrl", "DownloadUrl"),
            thumbnailUrl: pick(a, "thumbnailUrl", "ThumbnailUrl"),
            uploadedAt: pick(a, "uploadedAt", "UploadedAt")
        };
    }

    function normalizeMessage(raw) {
        if (!raw || typeof raw !== "object") return raw;
        var atts = pick(raw, "attachments", "Attachments") || [];
        return {
            id: pick(raw, "id", "Id"),
            chatId: pick(raw, "chatId", "ChatId"),
            senderId: pick(raw, "senderId", "SenderId"),
            content: pick(raw, "content", "Content") || "",
            status: pick(raw, "status", "Status"),
            createdAt: pick(raw, "createdAt", "CreatedAt"),
            editedAt: pick(raw, "editedAt", "EditedAt"),
            replyToId: pick(raw, "replyToId", "ReplyToId"),
            isDeleted: !!pick(raw, "isDeleted", "IsDeleted"),
            attachments: atts.map(normalizeAttachment),
            reactions: pick(raw, "reactions", "Reactions") || {}
        };
    }

    function connectSocket() {
        if (!state.token) return;
        // Avoid stacking reconnect timers / sockets
        if (state.socket && (state.socket.readyState === WebSocket.OPEN || state.socket.readyState === WebSocket.CONNECTING)) {
            return;
        }
        var proto = location.protocol === "https:" ? "wss:" : "ws:";
        var socket = new WebSocket(proto + "//" + location.host + WS_PATH);
        state.socket = socket;
        socket.onopen = function () {
            socket.send(JSON.stringify({ type: WS.Authenticate, payload: { token: state.token } }));
        };
        socket.onmessage = function (evt) {
            var env;
            try { env = JSON.parse(evt.data); } catch (e) { return; }
            handleSocketMessage(env);
        };
        socket.onclose = function () {
            clearInterval(state.heartbeat);
            state.socket = null;
            if (state.token && !state.locked && !document.hidden) setTimeout(connectSocket, 2000);
        };
        socket.onerror = function () { try { socket.close(); } catch (e) { /* ignore */ } };
    }

    function handleSocketMessage(env) {
        if (!env) return;
        var type = normalizeType(pick(env, "type", "Type"));
        var payload = pick(env, "payload", "Payload");
        var messageId = pick(env, "messageId", "MessageId");
        var error = pick(env, "error", "Error");

        switch (type) {
            case WS.Authenticate:
                startHeartbeat();
                break;
            case WS.NewMessage:
                if (payload) {
                    var m = normalizeMessage(payload);
                    var mine = state.me && m.senderId === state.me.id;
                    if (m.chatId === state.activeChatId) {
                        appendMessage(m);
                        scrollToBottom();
                    }
                    if (!mine) notifyIncoming(m);
                    bumpChat(m.chatId, m, !mine);
                }
                break;
            case WS.EditMessage:
                if (payload) {
                    var edited = normalizeMessage(payload);
                    updateMessage(edited);
                    if (edited.chatId) bumpChat(edited.chatId, edited, false);
                }
                break;
            case WS.MessageReaction:
                if (payload) updateMessage(normalizeMessage(payload));
                break;
            case WS.DeleteMessage:
                if (messageId) markMessageDeleted(messageId);
                break;
            case WS.StatusUpdate:
                if (payload) {
                    var userId = pick(payload, "userId", "UserId");
                    var status = pick(payload, "status", "Status");
                    if (userId) {
                        var p = state.peersById[userId];
                        if (p) p.status = status;
                        else state.peersById[userId] = { id: userId, status: status };
                        // Keep participant objects on chats in sync
                        state.chats.forEach(function (c) {
                            (c.participants || []).forEach(function (part) {
                                if (part.id === userId) part.status = status;
                            });
                        });
                        var chat = state.chats.find(function (c) { return c.id === state.activeChatId; });
                        if (chat) updatePeerStatus(chat);
                        renderChatList($("chatSearch").value);
                    }
                }
                break;
            case WS.AvatarUpdate:
                if (payload) {
                    var avatarUserId = pick(payload, "userId", "UserId");
                    if (avatarUserId) {
                        state.avatarBust[avatarUserId] = Date.now();
                        renderChatList($("chatSearch").value);
                    }
                }
                break;
            case WS.ChatCreated:
                if (payload) {
                    var created = payload;
                    var createdId = pick(created, "id", "Id");
                    if (createdId && !state.chats.some(function (c) { return c.id === createdId; })) {
                        state.chats.unshift(created);
                        state.lastPreviews[createdId] = "Start chat";
                        (created.participants || []).forEach(function (p) { state.peersById[p.id] = p; });
                        renderChatList($("chatSearch").value);
                    } else {
                        loadChats();
                    }
                }
                break;
            case WS.ChatUpdated:
                if (payload) {
                    var updatedChat = payload;
                    var updatedId = pick(updatedChat, "id", "Id");
                    if (updatedId) {
                        var updatedIdx = state.chats.findIndex(function (c) { return c.id === updatedId; });
                        if (updatedIdx >= 0) {
                            state.chats[updatedIdx] = updatedChat;
                            var avatarUrl = pick(updatedChat, "avatarUrl", "AvatarUrl");
                            if (avatarUrl) state.avatarBust[avatarUrl] = Date.now();
                            if (state.activeChatId === updatedId) openChat(updatedId);
                            else renderChatList($("chatSearch").value);
                        } else {
                            loadChats();
                        }
                    }
                }
                break;
            case WS.ChatDeleted:
                loadChats();
                break;
            case WS.Error:
                if (error) toast(error);
                break;
            default:
                break;
        }
    }

    function startHeartbeat() {
        clearInterval(state.heartbeat);
        state.heartbeat = setInterval(function () {
            if (state.socket && state.socket.readyState === WebSocket.OPEN) {
                state.socket.send(JSON.stringify({ type: WS.Heartbeat }));
            }
        }, 25000);
    }

    // ---- Notifications + sound ------------------------------------------------
    var audioCtx = null;
    var audioUnlocked = false;

    function unlockAudio() {
        try {
            audioCtx = audioCtx || new (window.AudioContext || window.webkitAudioContext)();
            if (audioCtx.state === "suspended") audioCtx.resume();
            // Play a near-silent blip so browsers mark the context as user-activated.
            var o = audioCtx.createOscillator(), g = audioCtx.createGain();
            o.connect(g); g.connect(audioCtx.destination);
            g.gain.value = 0.00001;
            o.start(); o.stop(audioCtx.currentTime + 0.01);
            audioUnlocked = true;
        } catch (e) { /* ignore */ }
    }

    function notifyIncoming(m) {
        if (state.prefs.soundEnabled !== false) playMessageSound();
        if (state.prefs.notificationsEnabled && document.hidden && "Notification" in window && Notification.permission === "granted") {
            try {
                new Notification(senderName(m), { body: (m.content || "New message").slice(0, 120), silent: true });
            } catch (e) { /* ignore */ }
        }
    }

    function playMessageSound() {
        // Prefer Web Audio (unlocked after first click/tap)
        try {
            audioCtx = audioCtx || new (window.AudioContext || window.webkitAudioContext)();
            if (audioCtx.state === "suspended") {
                audioCtx.resume().then(function () { beepNow(); }).catch(function () { beepFallback(); });
                return;
            }
            beepNow();
            return;
        } catch (e) { /* fall through */ }
        beepFallback();
    }

    function beepNow() {
        try {
            var t = audioCtx.currentTime;
            var o = audioCtx.createOscillator();
            var g = audioCtx.createGain();
            o.type = "sine";
            o.frequency.setValueAtTime(880, t);
            o.frequency.exponentialRampToValueAtTime(660, t + 0.12);
            g.gain.setValueAtTime(0.0001, t);
            g.gain.exponentialRampToValueAtTime(0.22, t + 0.02);
            g.gain.exponentialRampToValueAtTime(0.0001, t + 0.28);
            o.connect(g); g.connect(audioCtx.destination);
            o.start(t); o.stop(t + 0.3);
        } catch (e) {
            beepFallback();
        }
    }

    // HTMLAudioElement fallback (works even when AudioContext is blocked)
    function beepFallback() {
        try {
            var a = new Audio(
                "data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEAESsAACJWAAACABAAZGF0YQAAAAA="
            );
            // Short synthesized beep via oscillator-less tiny wav isn't audible;
            // use a slightly longer generated tone via Audio if available, else skip.
            a.volume = 0.5;
            var Ctx = window.AudioContext || window.webkitAudioContext;
            if (!Ctx) return;
            var ctx = new Ctx();
            var o = ctx.createOscillator();
            var g = ctx.createGain();
            o.connect(g); g.connect(ctx.destination);
            o.frequency.value = 880;
            g.gain.value = 0.2;
            o.start();
            setTimeout(function () {
                try { o.stop(); ctx.close(); } catch (e2) { /* ignore */ }
            }, 180);
        } catch (e) { /* ignore */ }
    }

    // ---- New chat / groups ----------------------------------------------------
    function openNewChat() {
        show($("newChatModal"));
        setChatMode("direct");
        $("userSearch").value = ""; $("groupName").value = "";
        state.selectedUsers = {};
        $("userList").innerHTML = '<p class="empty-hint">Loading people…</p>';
        api("GET", "/users").then(function (users) { state.users = users || []; renderUserList(); })
            .catch(function (e) { $("userList").innerHTML = '<p class="empty-hint">' + escapeHtml(e.message) + "</p>"; });
    }

    function setChatMode(mode) {
        state.chatMode = mode;
        $("segDirect").classList.toggle("active", mode === "direct");
        $("segGroup").classList.toggle("active", mode === "group");
        $("groupName").classList.toggle("hidden", mode !== "group");
        $("createGroupBtn").classList.toggle("hidden", mode !== "group");
        $("newChatTitle").textContent = mode === "group" ? "New group" : "New chat";
        renderUserList();
    }

    function renderUserList() {
        var list = $("userList"); var filter = ($("userSearch").value || "").toLowerCase();
        list.innerHTML = "";
        var rows = state.users.filter(function (u) {
            return !filter || (u.displayName + " " + u.username).toLowerCase().indexOf(filter) !== -1;
        });
        rows.forEach(function (u) {
            var li = document.createElement("li");
            li.className = "user-item" + (state.selectedUsers[u.id] ? " selected" : "");
            var img = document.createElement("img"); img.className = "avatar"; attachAvatar(img, u.id, u.displayName);
            var body = document.createElement("div"); body.className = "u-body";
            body.innerHTML = '<div class="u-name">' + escapeHtml(u.displayName) + '</div><div class="u-handle">@' + escapeHtml(u.username) + '</div>';
            li.appendChild(img); li.appendChild(body);
            if (state.chatMode === "group") {
                var chk = document.createElement("div"); chk.className = "u-check"; chk.textContent = state.selectedUsers[u.id] ? "✓" : "";
                li.appendChild(chk);
                li.addEventListener("click", function () {
                    if (state.selectedUsers[u.id]) delete state.selectedUsers[u.id]; else state.selectedUsers[u.id] = u;
                    renderUserList();
                });
            } else {
                li.addEventListener("click", function () { startDirectChat(u); });
            }
            list.appendChild(li);
        });
        if (!list.children.length) list.innerHTML = '<p class="empty-hint">No people found.</p>';
    }

    function startDirectChat(user) {
        var existing = state.chats.find(function (c) { return isDirect(c) && (c.participantIds || []).indexOf(user.id) !== -1; });
        hide($("newChatModal"));
        if (existing) { openChat(existing.id); return; }
        api("POST", "/chats", { name: user.displayName, type: 0, participantIds: [user.id] })
            .then(function (chat) {
                state.chats.push(chat);
                state.lastPreviews[chat.id] = "Start chat";
                (chat.participants || []).forEach(function (p) { state.peersById[p.id] = p; });
                renderChatList(); openChat(chat.id);
            }).catch(function (e) { toast(e.message); });
    }

    function createGroup() {
        var name = $("groupName").value.trim();
        var ids = Object.keys(state.selectedUsers);
        if (!name) { toast("Enter a group name"); return; }
        if (!ids.length) { toast("Select at least one member"); return; }
        // ChatType enum: DirectMessage=0, Group=1, Channel=2 (API expects numbers)
        api("POST", "/chats", { name: name, type: 1, participantIds: ids })
            .then(function (chat) {
                hide($("newChatModal"));
                state.chats.push(chat);
                state.lastPreviews[chat.id] = "Start chat";
                (chat.participants || []).forEach(function (p) { state.peersById[p.id] = p; });
                renderChatList(); openChat(chat.id);
            }).catch(function (e) { toast(e.message); });
    }

    // ---- Settings / preferences ----------------------------------------------
    function applyTheme(theme) {
        document.body.classList.toggle("theme-light", theme === "light");
        localStorage.setItem(THEME_KEY, theme);
        state.prefs.theme = theme;
    }

    function loadPreferences() {
        api("GET", "/preferences").then(function (p) {
            state.prefs.notificationsEnabled = !!p.notificationsEnabled;
            state.prefs.soundEnabled = !!p.soundEnabled;
            // Theme is intentionally NOT applied from the server here: the web
            // client defaults to the black & orange (dark) theme, and only the
            // user's explicit choice in Settings (persisted locally) changes it.
        }).catch(function () { /* preferences are optional */ });
    }

    function openSettings() {
        show($("settingsModal"));
        $("settingsDisplayName").value = state.me ? state.me.displayName : "";
        attachAvatar($("settingsAvatar"), state.me ? state.me.id : "default", state.me ? state.me.displayName : "");
        $("prefNotifications").checked = state.prefs.notificationsEnabled;
        $("prefSound").checked = state.prefs.soundEnabled;
        $("prefTheme").value = state.prefs.theme || "dark";
        loadDeviceSettings();
    }

    function renderDeviceSettings(status) {
        state.lockStatus = status;
        $("autoLockEnabled").checked = !!status.enabled;
        $("autoLockTimeout").value = String(status.timeoutSeconds || 60);
        $("autoLockControls").classList.toggle("hidden", !status.enabled);
    }

    function loadDeviceSettings() {
        api("GET", "/device-lock/status").then(renderDeviceSettings).catch(function (e) { toast(e.message); });
    }

    function openSecurityModal(mode) {
        state.securityMode = mode;
        $("securityError").textContent = "";
        $("securityPassword").value = ""; $("currentPin").value = "";
        $("newPin").value = ""; $("confirmPin").value = "";
        $("securityPasswordGroup").classList.toggle("hidden", mode !== "enable");
        $("currentPinGroup").classList.toggle("hidden", mode === "enable");
        $("newPinGroup").classList.toggle("hidden", mode === "timeout" || mode === "disable");
        $("securityModalTitle").textContent = mode === "enable" ? "Enable auto-lock" : mode === "change" ? "Change PIN" : mode === "disable" ? "Disable auto-lock" : "Update auto-lock";
        show($("securityModal"));
    }

    function saveSecurity() {
        var mode = state.securityMode;
        var wasPinReset = !!(state.lockStatus && state.lockStatus.requiresPinReset);
        var request;
        var operation;
        if (mode === "enable") {
            request = {
                accountPassword: $("securityPassword").value,
                pin: $("newPin").value,
                confirmPin: $("confirmPin").value,
                timeoutSeconds: Number($("autoLockTimeout").value || 60)
            };
            operation = api("POST", "/device-lock/enable", request);
        } else if (mode === "disable") {
            operation = api("POST", "/device-lock/disable", { pin: $("currentPin").value });
        } else {
            request = {
                currentPin: $("currentPin").value,
                timeoutSeconds: Number($("autoLockTimeout").value || 60),
                newPin: mode === "change" ? $("newPin").value : null,
                confirmNewPin: mode === "change" ? $("confirmPin").value : null
            };
            operation = api("PUT", "/device-lock", request);
        }

        operation.then(function () {
            hide($("securityModal"));
            if (wasPinReset) {
                state.locked = false;
                hide($("lockView"));
                enterApp();
            } else {
                loadDeviceSettings();
            }
            toast(mode === "disable" ? "Auto-lock disabled" : "Security settings saved");
        }).catch(function (e) { $("securityError").textContent = e.message; });
    }

    function savePreferences() {
        var theme = $("prefTheme").value;
        var dto = {
            userId: state.me ? state.me.id : "",
            notificationsEnabled: $("prefNotifications").checked,
            soundEnabled: $("prefSound").checked,
            theme: theme,
            language: "en",
            mutedChats: []
        };
        applyTheme(theme);
        if (dto.notificationsEnabled && "Notification" in window && Notification.permission === "default") {
            Notification.requestPermission();
        }
        api("PUT", "/preferences", dto).then(function (p) {
            state.prefs.notificationsEnabled = !!p.notificationsEnabled;
            state.prefs.soundEnabled = !!p.soundEnabled;
            toast("Preferences saved");
        }).catch(function (e) { toast(e.message); });
    }

    function saveProfile() {
        var name = $("settingsDisplayName").value.trim();
        if (!name) { toast("Display name cannot be empty"); return; }
        api("PUT", "/users/profile", { displayName: name }).then(function (u) {
            state.me = u; localStorage.setItem(USER_KEY, JSON.stringify(u));
            $("myName").textContent = u.displayName;
            toast("Profile updated");
        }).catch(function (e) { toast(e.message); });
    }

    function uploadAvatar(file) {
        var form = new FormData(); form.append("file", file, file.name);
        toast("Uploading photo…");
        apiForm("POST", "/users/avatar/upload", form).then(function (u) {
            state.me = u; localStorage.setItem(USER_KEY, JSON.stringify(u));
            state.avatarBust[u.id] = Date.now();
            attachAvatar($("settingsAvatar"), u.id, u.displayName);
            attachAvatar($("myAvatar"), u.id, u.displayName);
            renderChatList($("chatSearch").value);
            toast("Photo updated");
        }).catch(function (e) { toast(e.message); });
    }

    // ---- Image generation (Pollinations) -------------------------------------
    function openImageGen() { show($("imageGenModal")); }

    function generateImage() {
        var prompt = $("genPrompt").value.trim();
        if (!prompt) { toast("Enter a prompt"); return; }
        var model = $("genModel").value;
        var seed = Math.floor(Math.random() * 999999) + 1;
        var url = POLLINATIONS + encodeURIComponent(prompt) + "?model=" + model + "&width=1024&height=1024&seed=" + seed + "&nologo=true";
        var preview = $("genPreview");
        preview.innerHTML = '<div class="spinner"></div>';
        hide($("sendGenBtn"));
        var img = new Image();
        img.onload = function () { preview.innerHTML = ""; preview.appendChild(img); state.lastGenUrl = url; show($("sendGenBtn")); };
        img.onerror = function () { preview.textContent = "Generation failed. Try again."; state.lastGenUrl = null; };
        img.src = url;
    }

    function sendGeneratedImage() {
        if (!state.lastGenUrl) { toast("Generate an image first"); return; }
        if (!state.activeChatId) { toast("Open a chat first"); return; }
        var chatId = state.activeChatId;
        var url = state.lastGenUrl;
        var btn = $("sendGenBtn");
        btn.disabled = true;
        btn.textContent = "Sending…";
        postMessage(url, chatId).then(function (msg) {
            appendMessage(msg); scrollToBottom(); bumpChat(msg.chatId || chatId, msg);
            hide($("imageGenModal"));
            $("genPreview").innerHTML = "Your image will appear here";
            $("genPrompt").value = "";
            hide($("sendGenBtn"));
            state.lastGenUrl = null;
            toast("Image sent");
        }).catch(function (e) {
            toast(e.message);
        }).then(function () {
            btn.disabled = false;
            btn.textContent = "Send to chat";
        });
    }

    // ---- Emoji picker ---------------------------------------------------------
    function buildPickers() {
        var eg = $("emojiGrid"); eg.innerHTML = "";
        EMOJIS.forEach(function (e) {
            var b = document.createElement("button"); b.className = "emoji-cell"; b.type = "button"; b.textContent = e;
            b.addEventListener("click", function () {
                var inp = $("messageInput"); inp.value += e; inp.focus();
            });
            eg.appendChild(b);
        });
    }
    function togglePicker() { var p = $("pickerPanel"); if (p.classList.contains("hidden")) show(p); else hide(p); }
    function hidePicker() { hide($("pickerPanel")); }

    // ---- Voice messages -------------------------------------------------------
    function pickAudioMime() {
        var candidates = ["audio/webm;codecs=opus", "audio/webm", "audio/ogg;codecs=opus", "audio/ogg", "audio/mp4"];
        if (window.MediaRecorder && MediaRecorder.isTypeSupported) {
            for (var i = 0; i < candidates.length; i++) if (MediaRecorder.isTypeSupported(candidates[i])) return candidates[i];
        }
        return "";
    }

    function setRecordingUi(on) {
        state.recording = !!on;
        var voice = $("voiceBtn");
        var send = document.querySelector(".send-btn");
        if (on) {
            voice.classList.add("recording", "discard");
            voice.textContent = "🗑";
            voice.title = "Discard voice message";
            voice.setAttribute("aria-label", "Discard");
            if (send) { send.title = "Send voice message"; send.setAttribute("aria-label", "Send voice"); }
            showStrip("🎤", "Recording… tap ➤ to send, or 🗑 to discard", true);
            $("messageInput").disabled = true;
            $("messageInput").placeholder = "Recording…";
        } else {
            voice.classList.remove("recording", "discard");
            voice.textContent = "🎤";
            voice.title = "Record voice message";
            voice.setAttribute("aria-label", "Voice");
            if (send) { send.title = "Send"; send.setAttribute("aria-label", "Send"); }
            hideStrip();
            $("messageInput").disabled = false;
            $("messageInput").placeholder = "Message";
        }
    }

    function startRecording() {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            toast("Recording not supported on this device"); return;
        }
        if (!state.activeChatId) { toast("Open a chat first"); return; }
        navigator.mediaDevices.getUserMedia({ audio: true }).then(function (stream) {
            state.recStream = stream; state.recChunks = []; state.recDiscard = false;
            var mime = pickAudioMime();
            var rec = mime ? new MediaRecorder(stream, { mimeType: mime }) : new MediaRecorder(stream);
            state.recorder = rec;
            rec.ondataavailable = function (e) { if (e.data && e.data.size) state.recChunks.push(e.data); };
            rec.onstop = function () {
                stream.getTracks().forEach(function (t) { t.stop(); });
                var discard = state.recDiscard;
                state.recorder = null;
                state.recStream = null;
                setRecordingUi(false);
                if (discard) { state.recChunks = []; return; }
                var type = rec.mimeType || mime || "audio/webm";
                var ext = type.indexOf("ogg") !== -1 ? "ogg" : (type.indexOf("mp4") !== -1 ? "m4a" : "webm");
                var blob = new Blob(state.recChunks, { type: type });
                state.recChunks = [];
                if (blob.size > 0) {
                    var file = new File([blob], "voice-message." + ext, { type: type });
                    sendFile(file, "🎤 Voice message");
                }
            };
            rec.start();
            setRecordingUi(true);
        }).catch(function () { toast("Microphone access denied"); });
    }

    function finishRecordingAndSend() {
        if (!state.recorder || state.recorder.state === "inactive") return;
        state.recDiscard = false;
        try { state.recorder.stop(); } catch (e) { setRecordingUi(false); }
    }

    function cancelRecording() {
        if (!state.recorder && !state.recording) return;
        state.recDiscard = true;
        if (state.recorder && state.recorder.state !== "inactive") {
            try { state.recorder.stop(); } catch (e) { /* ignore */ }
        } else {
            if (state.recStream) state.recStream.getTracks().forEach(function (t) { t.stop(); });
            state.recorder = null; state.recChunks = [];
            setRecordingUi(false);
        }
    }

    function onVoiceBtnClick() {
        if (state.recording) cancelRecording();
        else startRecording();
    }

    // ---- Compose strip (preview) ---------------------------------------------
    function showStrip(icon, text, recording) {
        $("stripIcon").innerHTML = recording ? '<span class="rec-dot"></span>' : escapeHtml(icon);
        $("stripName").textContent = text;
        show($("composeStrip"));
    }
    function hideStrip() { hide($("composeStrip")); }

    // ---- Lightbox -------------------------------------------------------------
    function openLightbox(src) {
        if (!src) return;
        show($("lightbox"));
        $("lightboxImg").removeAttribute("src");
        loadAuthUrl(src).then(function (objUrl) {
            $("lightboxImg").src = objUrl;
        }).catch(function (err) {
            hide($("lightbox"));
            toast(err.message || "Failed to open image");
        });
    }

    // ---- Viewport height fix --------------------------------------------------
    function setAppHeight() { document.documentElement.style.setProperty("--app-height", window.innerHeight + "px"); }

    // ---- Init -----------------------------------------------------------------
    function init() {
        setAppHeight();
        window.addEventListener("resize", setAppHeight);
        window.addEventListener("orientationchange", setAppHeight);
        buildPickers();

        // Browsers block audio until a user gesture — unlock on first interaction.
        ["pointerdown", "keydown", "touchstart", "click"].forEach(function (evt) {
            document.addEventListener(evt, unlockAudio, { once: false, passive: true });
        });
        document.addEventListener("visibilitychange", function () {
            if (!document.hidden) unlockAudio();
            if (document.hidden) reportActivity();
            else checkLockStatus().then(function () { if (!state.locked) reportActivity().then(connectSocket); });
        });
        window.addEventListener("pagehide", function () {
            if (!state.token || !state.lockStatus || !state.lockStatus.enabled) return;
            fetch(API + "/device-lock/activity", {
                method: "POST", keepalive: true,
                headers: { "Content-Type": "application/json", Authorization: "Bearer " + state.token },
                body: JSON.stringify({ tabId: state.tabId, isVisible: false, hasActiveCall: false })
            }).catch(function () { });
        });

        // Auth
        $("showRegister").addEventListener("click", function () { hide($("loginForm")); show($("registerForm")); $("authError").textContent = ""; });
        $("showLogin").addEventListener("click", function () { hide($("registerForm")); show($("loginForm")); $("authError").textContent = ""; });

        $("loginForm").addEventListener("submit", function (e) {
            e.preventDefault(); $("authError").textContent = "";
            var btn = e.target.querySelector("button[type=submit]"); btn.disabled = true;
            login($("loginIdentifier").value, $("loginPassword").value)
                .catch(function (err) { $("authError").textContent = err.message; })
                .then(function () { btn.disabled = false; });
        });
        $("registerForm").addEventListener("submit", function (e) {
            e.preventDefault(); $("authError").textContent = "";
            var btn = e.target.querySelector("button[type=submit]"); btn.disabled = true;
            register($("regDisplayName").value, $("regUsername").value, $("regEmail").value, $("regPassword").value)
                .catch(function (err) { $("authError").textContent = err.message; })
                .then(function () { btn.disabled = false; });
        });

        // Sidebar
        $("logoutBtn").addEventListener("click", logout);
        $("newChatBtn").addEventListener("click", openNewChat);
        $("settingsBtn").addEventListener("click", openSettings);
        $("chatSearch").addEventListener("input", function (e) { renderChatList(e.target.value); });

        $("myStatusBadge").addEventListener("click", function (e) {
            e.preventDefault();
            e.stopPropagation();
            toggleStatusMenu(e);
        });
        Array.prototype.forEach.call(document.querySelectorAll(".status-option"), function (btn) {
            btn.addEventListener("click", function (e) {
                e.preventDefault();
                e.stopPropagation();
                setMyStatus(btn.getAttribute("data-status"));
            });
        });
        document.addEventListener("click", function (e) {
            if (!e.target.closest(".me-avatar")) {
                closeStatusMenu();
            }
        });

        document.querySelector('.folder-chip[data-folder-id="all"]').addEventListener("click", function () {
            state.activeFolderId = "all";
            renderFolderBar();
            renderChatList($("chatSearch").value);
        });
        $("newFolderBtn").addEventListener("click", function () { openFolderModal(null); });
        $("closeFolderModal").addEventListener("click", function () { hide($("folderModal")); });
        $("folderModal").addEventListener("click", function (e) { if (e.target === $("folderModal")) hide($("folderModal")); });
        $("saveFolderBtn").addEventListener("click", saveFolder);
        $("deleteFolderBtn").addEventListener("click", function () {
            if (!state.editingFolderId) return;
            var id = state.editingFolderId;
            openConfirm({
                title: "Delete folder?",
                text: "Remove this personal folder? Chats stay in All.",
                okText: "Delete",
                onConfirm: function () {
                    api("DELETE", "/folders/" + encodeURIComponent(id))
                        .then(function () {
                            hide($("folderModal"));
                            if (state.activeFolderId === id) state.activeFolderId = "all";
                            state.editingFolderId = null;
                            toast("Folder deleted");
                            return loadFolders();
                        })
                        .catch(function (err) { toast(err.message); });
                }
            });
        });

        $("chatMenuBtn").addEventListener("click", function () {
            if (state.activeChatId) openChatOptions(state.activeChatId);
        });
        $("closeChatOptions").addEventListener("click", function () { hide($("chatOptionsModal")); });
        $("chatOptionsModal").addEventListener("click", function (e) { if (e.target === $("chatOptionsModal")) hide($("chatOptionsModal")); });
        $("optEditGroup").addEventListener("click", function () {
            hide($("chatOptionsModal"));
            openEditGroup(state.optionsChatId);
        });
        $("optLeaveGroup").addEventListener("click", function () {
            hide($("chatOptionsModal"));
            leaveGroup(state.optionsChatId);
        });
        $("optAddToFolder").addEventListener("click", function () {
            hide($("chatOptionsModal"));
            openAddToFolder(state.optionsChatId);
        });
        $("optDeleteChat").addEventListener("click", function () {
            hide($("chatOptionsModal"));
            deleteChat(state.optionsChatId);
        });

        $("closeEditGroup").addEventListener("click", function () { hide($("editGroupModal")); });
        $("editGroupModal").addEventListener("click", function (e) { if (e.target === $("editGroupModal")) hide($("editGroupModal")); });
        $("editGroupAvatarBtn").addEventListener("click", function () { $("editGroupAvatarInput").click(); });
        $("editGroupAvatarInput").addEventListener("change", function (e) {
            var file = e.target.files && e.target.files[0];
            if (!file) return;
            state.editGroupAvatarFile = file;
            $("editGroupAvatar").src = URL.createObjectURL(file);
            e.target.value = "";
        });
        $("saveGroupBtn").addEventListener("click", saveGroupEdits);

        $("closeAddToFolder").addEventListener("click", function () { hide($("addToFolderModal")); });
        $("addToFolderModal").addEventListener("click", function (e) { if (e.target === $("addToFolderModal")) hide($("addToFolderModal")); });

        // New chat modal
        $("closeNewChat").addEventListener("click", function () { hide($("newChatModal")); });
        $("newChatModal").addEventListener("click", function (e) { if (e.target === $("newChatModal")) hide($("newChatModal")); });
        $("segDirect").addEventListener("click", function () { setChatMode("direct"); });
        $("segGroup").addEventListener("click", function () { setChatMode("group"); });
        $("userSearch").addEventListener("input", renderUserList);
        $("createGroupBtn").addEventListener("click", createGroup);

        // Settings modal
        $("closeSettings").addEventListener("click", function () { hide($("settingsModal")); });
        $("settingsModal").addEventListener("click", function (e) { if (e.target === $("settingsModal")) hide($("settingsModal")); });
        $("savePrefsBtn").addEventListener("click", savePreferences);
        $("saveProfileBtn").addEventListener("click", saveProfile);
        $("uploadAvatarBtn").addEventListener("click", function () { $("avatarInput").click(); });
        $("avatarInput").addEventListener("change", function (e) { if (e.target.files[0]) uploadAvatar(e.target.files[0]); e.target.value = ""; });
        $("autoLockEnabled").addEventListener("change", function (e) {
            if (e.target.checked && !(state.lockStatus && state.lockStatus.enabled)) {
                openSecurityModal("enable");
            } else if (!e.target.checked && state.lockStatus && state.lockStatus.enabled) {
                e.target.checked = true;
                openSecurityModal("disable");
            }
        });
        $("saveLockSettingsBtn").addEventListener("click", function () { openSecurityModal("timeout"); });
        $("changePinBtn").addEventListener("click", function () { openSecurityModal("change"); });
        $("lockNowBtn").addEventListener("click", function () {
            api("POST", "/device-lock/lock").then(showLocked).catch(function (e) { toast(e.message); });
        });
        $("closeSecurityModal").addEventListener("click", function () {
            if (state.lockStatus && state.lockStatus.requiresPinReset) return;
            hide($("securityModal")); renderDeviceSettings(state.lockStatus);
        });
        $("saveSecurityBtn").addEventListener("click", saveSecurity);

        $("unlockPin").addEventListener("input", function (e) {
            e.target.value = e.target.value.replace(/\D/g, "").slice(0, 4);
            if (e.target.value.length === 4) unlockDevice(e.target.value);
        });
        $("unlockForm").addEventListener("submit", function (e) {
            e.preventDefault();
            if ($("unlockPin").value.length === 4) unlockDevice($("unlockPin").value);
        });
        $("forgotPinBtn").addEventListener("click", function () {
            api("POST", "/device-lock/forgot").catch(function () { }).then(logout);
        });

        // Image gen modal
        $("imageGenBtn").addEventListener("click", openImageGen);
        $("closeImageGen").addEventListener("click", function () { hide($("imageGenModal")); });
        $("imageGenModal").addEventListener("click", function (e) { if (e.target === $("imageGenModal")) hide($("imageGenModal")); });
        $("genBtn").addEventListener("click", generateImage);
        $("sendGenBtn").addEventListener("click", sendGeneratedImage);

        // Conversation
        $("backBtn").addEventListener("click", function () {
            document.body.classList.remove("chat-open"); state.activeChatId = null; hidePicker();
            Array.prototype.forEach.call(document.querySelectorAll(".chat-item"), function (el) { el.classList.remove("active"); });
        });
        $("composer").addEventListener("submit", function (e) {
            e.preventDefault(); hidePicker();
            if (state.recording) { finishRecordingAndSend(); return; }
            if (state.editingMessageId) {
                submitEdit($("messageInput").value);
                return;
            }
            var input = $("messageInput"); sendText(input.value); input.value = ""; input.focus();
        });

        // Composer toolbar
        $("attachBtn").addEventListener("click", function () {
            if (state.editingMessageId) { toast("Finish or cancel editing first"); return; }
            $("fileInput").click();
        });
        $("fileInput").addEventListener("change", function (e) { if (e.target.files[0]) sendFile(e.target.files[0], ""); e.target.value = ""; });
        $("emojiBtn").addEventListener("click", togglePicker);
        $("voiceBtn").addEventListener("click", function () {
            if (state.editingMessageId) { toast("Finish or cancel editing first"); return; }
            onVoiceBtnClick();
        });

        // Delegated: message Edit/Delete + image lightbox + file preview
        $("messages").addEventListener("click", function (e) {
            var actionBtn = e.target.closest && e.target.closest(".msg-action");
            if (actionBtn) {
                e.preventDefault();
                e.stopPropagation();
                var row = actionBtn.closest(".msg-row");
                var id = row && row.dataset.msgId;
                if (!id) return;
                if (actionBtn.dataset.action === "edit") startEdit(id);
                else if (actionBtn.dataset.action === "delete") deleteOwnMessage(id);
                return;
            }
            var previewBtn = e.target.closest && e.target.closest(".att-preview-btn");
            if (previewBtn) {
                e.preventDefault();
                openFilePreview(
                    previewBtn.getAttribute("data-att-url"),
                    previewBtn.getAttribute("data-att-name"),
                    previewBtn.getAttribute("data-att-size")
                );
                return;
            }
            var downloadBtn = e.target.closest && e.target.closest(".att-download-btn");
            if (downloadBtn) {
                e.preventDefault();
                downloadAttachment(
                    downloadBtn.getAttribute("data-att-url"),
                    downloadBtn.getAttribute("data-att-name")
                );
                return;
            }
            var readMoreBtn = e.target.closest && e.target.closest(".read-more-btn");
            if (readMoreBtn) {
                e.preventDefault();
                toggleReadMore(readMoreBtn);
                return;
            }
            var t = e.target;
            if (t && t.classList && t.classList.contains("att-image")) {
                openLightbox(t.getAttribute("data-full") || t.src);
            }
        });
        $("lightbox").addEventListener("click", function () { hide($("lightbox")); $("lightboxImg").src = ""; });
        $("closeFilePreview").addEventListener("click", closeFilePreview);
        $("filePreviewModal").addEventListener("click", function (e) {
            if (e.target === $("filePreviewModal")) closeFilePreview();
        });
        $("filePreviewDownload").addEventListener("click", function () {
            if (state.previewDownloadUrl) {
                downloadAttachment(state.previewDownloadUrl, state.previewFileName);
            }
        });

        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && state.editingMessageId) cancelEdit();
            if (e.key === "Escape" && !$("filePreviewModal").classList.contains("hidden")) {
                closeFilePreview();
            }
        });
        $("stripCancel").addEventListener("click", function (e) {
            e.stopPropagation();
            if (state.recording) cancelRecording();
            else if (state.editingMessageId) cancelEdit();
            else hideStrip();
        });

        // Custom confirm dialog
        $("confirmCancel").addEventListener("click", closeConfirm);
        $("confirmClose").addEventListener("click", closeConfirm);
        $("confirmOk").addEventListener("click", acceptConfirm);
        $("confirmModal").addEventListener("click", function (e) {
            if (e.target === $("confirmModal")) closeConfirm();
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !$("confirmModal").classList.contains("hidden")) {
                closeConfirm();
            }
        });

        localStorage.removeItem("nexus_token");
        restoreSession();
    }

    document.addEventListener("DOMContentLoaded", init);
})();
