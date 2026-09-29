/**
 * GGCLIKS Google Gemini AI Chatbot Client
 */
document.addEventListener("DOMContentLoaded", function () {
    const toggleBtn = document.getElementById("chatbotToggleBtn");
    const chatWindow = document.getElementById("chatbotWindow");
    const closeBtn = document.getElementById("chatbotCloseBtn");
    const chatForm = document.getElementById("chatbotForm");
    const chatInput = document.getElementById("chatbotInput");
    const messagesContainer = document.getElementById("chatbotMessages");

    if (!toggleBtn || !chatWindow) return;

    // Toggle Chat Window
    toggleBtn.addEventListener("click", () => {
        chatWindow.classList.toggle("open");
        if (chatWindow.classList.contains("open")) {
            chatInput?.focus();
        }
    });

    closeBtn?.addEventListener("click", () => {
        chatWindow.classList.remove("open");
    });

    // Quick Suggestions
    document.querySelectorAll(".suggestion-chip").forEach(chip => {
        chip.addEventListener("click", function () {
            const query = this.getAttribute("data-query");
            if (query && chatInput) {
                chatInput.value = query;
                sendMessage(query);
            }
        });
    });

    // Submit handler
    chatForm?.addEventListener("submit", function (e) {
        e.preventDefault();
        const text = chatInput.value.trim();
        if (!text) return;
        sendMessage(text);
    });

    async function sendMessage(userText) {
        // Append user bubble
        appendMessage(userText, "user");
        chatInput.value = "";

        // Show typing indicator
        const typingEl = document.createElement("div");
        typingEl.className = "typing-indicator";
        typingEl.innerHTML = '<div class="typing-dot"></div><div class="typing-dot"></div><div class="typing-dot"></div>';
        messagesContainer.appendChild(typingEl);
        scrollToBottom();

        try {
            const response = await fetch("/AI/Chat", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ message: userText })
            });

            typingEl.remove();

            if (!response.ok) {
                appendMessage("Sorry, I could not connect to the server. Please try again.", "bot");
                return;
            }

            const data = await response.json();
            appendMessage(data.reply || "I'm here to help with our GGCLIKS products!", "bot");
        } catch (error) {
            typingEl.remove();
            console.error("Chatbot fetch error:", error);
            appendMessage("Unable to reach the assistant right now. Please check your connection.", "bot");
        }
    }

    function appendMessage(text, role) {
        const bubble = document.createElement("div");
        bubble.className = `chat-bubble ${role}`;
        // Basic markdown formatting for bold and lists
        let formatted = escapeHtml(text)
            .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
            .replace(/\*(.*?)\*/g, '<em>$1</em>')
            .replace(/\n/g, '<br>');

        bubble.innerHTML = formatted;
        messagesContainer.appendChild(bubble);
        scrollToBottom();
    }

    function scrollToBottom() {
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    function escapeHtml(string) {
        const div = document.createElement('div');
        div.innerText = string;
        return div.innerHTML;
    }
});
