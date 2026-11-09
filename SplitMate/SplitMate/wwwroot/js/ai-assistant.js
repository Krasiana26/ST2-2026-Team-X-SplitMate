(() => {
    const form = document.getElementById("ai-chat-form");
    const input = document.getElementById("ai-message");
    const sendButton = document.getElementById("ai-send-button");
    const messages = document.getElementById("ai-messages");

    if (!form || !input || !sendButton || !messages) {
        return;
    }

    const addMessage = (role, text, label, extraClass = "") => {
        const message = document.createElement("div");
        message.className = `ai-message ${role}${extraClass ? ` ${extraClass}` : ""}`;

        const messageLabel = document.createElement("span");
        messageLabel.className = "ai-message-label";
        messageLabel.textContent = label;

        const content = document.createElement("div");
        content.className = "ai-message-content";
        content.textContent = text;

        message.append(messageLabel, content);
        messages.append(message);
        message.scrollIntoView({ block: "nearest", behavior: "smooth" });
        return message;
    };

    form.addEventListener("submit", async (event) => {
        event.preventDefault();

        const message = input.value.trim();
        if (!message || message.length > 2000 || sendButton.disabled) {
            return;
        }

        addMessage("user", message, "You");
        input.value = "";
        input.disabled = true;
        sendButton.disabled = true;

        const loadingMessage = addMessage(
            "assistant",
            "Thinking",
            "SplitMate Assistant",
            "loading");

        try {
            const token = form.querySelector(
                'input[name="__RequestVerificationToken"]')?.value;
            const response = await fetch(form.action, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token ?? ""
                },
                body: JSON.stringify({ message })
            });

            const result = await response.json();
            loadingMessage.remove();

            if (!response.ok) {
                addMessage(
                    "error",
                    result.error || "The request could not be completed.",
                    "Assistant");
                return;
            }

            addMessage("assistant", result.reply, "SplitMate Assistant");
        } catch {
            loadingMessage.remove();
            addMessage(
                "error",
                "Could not reach the assistant. Check your connection and try again.",
                "Assistant");
        } finally {
            input.disabled = false;
            sendButton.disabled = false;
            input.focus();
        }
    });
})();
