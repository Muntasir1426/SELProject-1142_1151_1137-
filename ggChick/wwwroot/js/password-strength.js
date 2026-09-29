/**
 * GGCLIKS Live Password Strength Indicator
 */
document.addEventListener("DOMContentLoaded", function () {
    const passwordInput = document.getElementById("passwordInput");
    const strengthBar = document.getElementById("strengthBar");
    const strengthText = document.getElementById("strengthText");
    const strengthContainer = document.getElementById("strengthContainer");

    if (!passwordInput || !strengthBar || !strengthText) return;

    passwordInput.addEventListener("input", function () {
        const val = passwordInput.value;
        if (!val) {
            strengthBar.style.width = "0%";
            strengthText.textContent = "";
            if (strengthContainer) strengthContainer.classList.add("d-none");
            return;
        }

        if (strengthContainer) strengthContainer.classList.remove("d-none");

        let score = 0;

        // Criteria checks
        if (val.length >= 6) score += 1;
        if (val.length >= 8) score += 1;
        if (/[A-Z]/.test(val)) score += 1;
        if (/[a-z]/.test(val)) score += 1;
        if (/[0-9]/.test(val)) score += 1;
        if (/[^A-Za-z0-9]/.test(val)) score += 1;

        if (score <= 2) {
            strengthBar.style.width = "33%";
            strengthBar.className = "progress-bar bg-danger";
            strengthText.innerHTML = '<span class="text-danger fw-bold">Weak</span> - Try adding numbers, capitals, and symbols';
        } else if (score <= 4) {
            strengthBar.style.width = "66%";
            strengthBar.className = "progress-bar bg-warning";
            strengthText.innerHTML = '<span class="text-warning fw-bold">Medium</span> - Add special symbols or more length';
        } else {
            strengthBar.style.width = "100%";
            strengthBar.className = "progress-bar bg-success";
            strengthText.innerHTML = '<span class="text-success fw-bold">Strong</span> - Great password!';
        }
    });
});
