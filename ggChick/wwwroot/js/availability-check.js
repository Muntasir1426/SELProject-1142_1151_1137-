/**
 * GGCLIKS Real-Time Username & Email Availability Check with Debounce
 */
document.addEventListener("DOMContentLoaded", function () {
    const usernameInput = document.getElementById("usernameInput");
    const usernameFeedback = document.getElementById("usernameFeedback");

    const emailInput = document.getElementById("emailInput");
    const emailFeedback = document.getElementById("emailFeedback");

    function debounce(func, delay = 400) {
        let timer;
        return function (...args) {
            clearTimeout(timer);
            timer = setTimeout(() => func.apply(this, args), delay);
        };
    }

    // 1. Check Username Availability
    if (usernameInput && usernameFeedback) {
        const checkUsername = debounce(async function () {
            const username = usernameInput.value.trim();
            if (username.length < 3) {
                usernameFeedback.innerHTML = "";
                return;
            }

            usernameFeedback.innerHTML = '<span class="text-muted"><i class="bi bi-arrow-repeat spin"></i> Checking availability...</span>';

            try {
                const response = await fetch(`/Account/CheckUsername?username=${encodeURIComponent(username)}`);
                if (!response.ok) return;

                const data = await response.json();
                if (data.available) {
                    usernameFeedback.innerHTML = `<span class="text-success fw-semibold"><i class="bi bi-check-circle-fill"></i> ${data.message}</span>`;
                    usernameInput.classList.remove("is-invalid");
                    usernameInput.classList.add("is-valid");
                } else {
                    usernameFeedback.innerHTML = `<span class="text-danger fw-semibold"><i class="bi bi-x-circle-fill"></i> ${data.message}</span>`;
                    usernameInput.classList.remove("is-valid");
                    usernameInput.classList.add("is-invalid");
                }
            } catch (err) {
                console.error("Username check error:", err);
                usernameFeedback.innerHTML = "";
            }
        }, 350);

        usernameInput.addEventListener("input", checkUsername);
    }

    // 2. Check Email Availability
    if (emailInput && emailFeedback) {
        const checkEmail = debounce(async function () {
            const email = emailInput.value.trim();
            if (!email.includes("@") || email.length < 5) {
                emailFeedback.innerHTML = "";
                return;
            }

            emailFeedback.innerHTML = '<span class="text-muted"><i class="bi bi-arrow-repeat spin"></i> Checking email...</span>';

            try {
                const response = await fetch(`/Account/CheckEmail?email=${encodeURIComponent(email)}`);
                if (!response.ok) return;

                const data = await response.json();
                if (data.available) {
                    emailFeedback.innerHTML = `<span class="text-success fw-semibold"><i class="bi bi-check-circle-fill"></i> ${data.message}</span>`;
                    emailInput.classList.remove("is-invalid");
                    emailInput.classList.add("is-valid");
                } else {
                    emailFeedback.innerHTML = `<span class="text-danger fw-semibold"><i class="bi bi-x-circle-fill"></i> ${data.message}</span>`;
                    emailInput.classList.remove("is-valid");
                    emailInput.classList.add("is-invalid");
                }
            } catch (err) {
                console.error("Email check error:", err);
                emailFeedback.innerHTML = "";
            }
        }, 350);

        emailInput.addEventListener("input", checkEmail);
    }
});
