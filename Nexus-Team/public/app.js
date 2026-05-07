const chats = {
  anna: {
    name: "Anna",
    initial: "A",
    status: "Online",
    presence: "online",
    messages: [
      { type: "date", text: "Today" },
      { side: "incoming", author: "Anna", text: "Can you review the login and registration visuals today?", time: "10:31" },
      { side: "outgoing", text: "Yes. I matched the glass card style and the NexusTeam dark background.", time: "10:34", reactions: ["👍 2"] },
      { side: "incoming", author: "Anna", text: "Great. The social buttons and sign-in/sign-up switch should stay visible.", time: "10:40" },
      { side: "outgoing", text: "They are in the same positions as the desktop UI.", time: "10:42" }
    ]
  },
  team: {
    name: "Messenger Team",
    initial: "M",
    status: "Away",
    presence: "away",
    messages: [
      { type: "date", text: "Today" },
      { side: "incoming", author: "Diana", text: "The chat list needs realistic sample conversations.", time: "09:08" },
      { side: "incoming", author: "Matvei", text: "I added unread badges, status dots, and message timestamps.", time: "09:12" },
      { side: "outgoing", text: "Nice. The messenger window now has folders, chats, messages, search, and composer.", time: "09:15", reactions: ["🔥 3", "✅ 4"] }
    ]
  },
  diana: {
    name: "Diana",
    initial: "D",
    status: "Online",
    presence: "online",
    messages: [
      { type: "date", text: "Yesterday" },
      { side: "incoming", author: "Diana", text: "Let's keep the layout close to desktop.", time: "18:25" },
      { side: "outgoing", text: "Agreed. Three columns: folders, chat list, selected conversation.", time: "18:30" },
      { side: "incoming", author: "Diana", text: "And use the same warm dark background.", time: "18:32" }
    ]
  },
  matvei: {
    name: "Matvei",
    initial: "M",
    status: "Busy",
    presence: "busy",
    messages: [
      { type: "date", text: "Monday" },
      { side: "incoming", author: "Matvei", text: "I added fake data for the chat list.", time: "14:02" },
      { side: "outgoing", text: "Perfect. It makes the messenger screen look populated immediately.", time: "14:06" },
      { side: "incoming", author: "Matvei", text: "The current user messages are aligned to the right, like the original.", time: "14:09", reactions: ["👍 1"] }
    ]
  }
};

const screenButtons = document.querySelectorAll("[data-screen]");
const screens = document.querySelectorAll("[data-screen-panel]");
const chatButtons = document.querySelectorAll("[data-chat]");
const messagesContainer = document.querySelector("[data-messages]");
const selectedName = document.querySelector("[data-selected-name]");
const selectedStatus = document.querySelector("[data-selected-status]");
const selectedAvatar = document.querySelector("[data-selected-avatar]");

function showScreen(target) {
  screens.forEach((screen) => {
    screen.classList.toggle("is-active", screen.dataset.screenPanel === target);
  });

  document.querySelectorAll(".view-tab").forEach((button) => {
    button.classList.toggle("is-active", button.dataset.screen === target);
  });
}

function renderMessages(chatId) {
  const chat = chats[chatId];
  selectedName.textContent = chat.name;
  selectedStatus.textContent = chat.status;
  selectedAvatar.textContent = chat.initial;
  selectedAvatar.className = `avatar ${chat.presence}`;

  messagesContainer.innerHTML = chat.messages.map((message) => {
    if (message.type === "date") {
      return `<div class="date-chip">${message.text}</div>`;
    }

    const author = message.author ? `<span class="message-author">${message.author}</span>` : "";
    const reactions = message.reactions
      ? `<div class="reaction-row">${message.reactions.map((reaction) => `<span>${reaction}</span>`).join("")}</div>`
      : "";

    return `
      <article class="message ${message.side}">
        ${author}
        <div class="message-bubble">${message.text}</div>
        <span class="message-time">${message.time}</span>
        ${reactions}
      </article>
    `;
  }).join("");
}

screenButtons.forEach((button) => {
  button.addEventListener("click", () => showScreen(button.dataset.screen));
});

chatButtons.forEach((button) => {
  button.addEventListener("click", () => {
    chatButtons.forEach((chatButton) => {
      chatButton.classList.toggle("is-active", chatButton === button);
    });
    renderMessages(button.dataset.chat);
  });
});

renderMessages("anna");
