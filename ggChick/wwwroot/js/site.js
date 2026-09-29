/**
 * GGCLIKS Main Site JavaScript
 */

// 1. Dark Mode Management
document.addEventListener("DOMContentLoaded", function () {
    const themeToggleBtn = document.getElementById("themeToggleBtn");
    const themeIcon = document.getElementById("themeIcon");
    const htmlElement = document.documentElement;

    const savedTheme = localStorage.getItem("ggcliks-theme") || "light";
    htmlElement.setAttribute("data-bs-theme", savedTheme);
    updateThemeIcon(savedTheme);

    themeToggleBtn?.addEventListener("click", function () {
        const currentTheme = htmlElement.getAttribute("data-bs-theme");
        const newTheme = currentTheme === "dark" ? "light" : "dark";
        htmlElement.setAttribute("data-bs-theme", newTheme);
        localStorage.setItem("ggcliks-theme", newTheme);
        updateThemeIcon(newTheme);
    });

    function updateThemeIcon(theme) {
        if (!themeIcon) return;
        if (theme === "dark") {
            themeIcon.className = "bi bi-sun-fill text-warning";
        } else {
            themeIcon.className = "bi bi-moon-stars-fill text-secondary";
        }
    }

    // 2. Interactive Star Rating selector for review submission
    const starContainer = document.querySelector(".interactive-stars");
    const ratingInput = document.getElementById("ratingInput");
    if (starContainer && ratingInput) {
        const stars = starContainer.querySelectorAll(".star");
        stars.forEach(star => {
            star.addEventListener("mouseover", function () {
                const val = parseInt(this.getAttribute("data-value"));
                highlightStars(val);
            });

            star.addEventListener("mouseout", function () {
                const selectedVal = parseInt(ratingInput.value) || 5;
                highlightStars(selectedVal);
            });

            star.addEventListener("click", function () {
                const val = parseInt(this.getAttribute("data-value"));
                ratingInput.value = val;
                highlightStars(val);
            });
        });

        function highlightStars(val) {
            stars.forEach(s => {
                const sVal = parseInt(s.getAttribute("data-value"));
                if (sVal <= val) {
                    s.classList.add("selected");
                    s.classList.replace("bi-star", "bi-star-fill");
                } else {
                    s.classList.remove("selected");
                    s.classList.replace("bi-star-fill", "bi-star");
                }
            });
        }
        highlightStars(parseInt(ratingInput.value) || 5);
    }

    // 3. Multi-image file preview with remove option in Product form
    const mainImageInput = document.getElementById("mainImageInput");
    const mainImagePreview = document.getElementById("mainImagePreview");
    if (mainImageInput && mainImagePreview) {
        mainImageInput.addEventListener("change", function () {
            const file = this.files[0];
            if (file) {
                if (file.size > 5 * 1024 * 1024) {
                    alert("File size exceeds 5MB limit.");
                    this.value = "";
                    return;
                }
                const reader = new FileReader();
                reader.onload = function (e) {
                    mainImagePreview.innerHTML = `
                        <div class="preview-thumb-box mt-2">
                            <img src="${e.target.result}" alt="Preview" />
                        </div>
                    `;
                };
                reader.readAsDataURL(file);
            }
        });
    }

    const additionalImagesInput = document.getElementById("additionalImagesInput");
    const additionalImagesPreview = document.getElementById("additionalImagesPreview");
    if (additionalImagesInput && additionalImagesPreview) {
        additionalImagesInput.addEventListener("change", function () {
            additionalImagesPreview.innerHTML = "";
            const files = Array.from(this.files);
            files.forEach((file, index) => {
                if (file.size > 5 * 1024 * 1024) return;
                const reader = new FileReader();
                reader.onload = function (e) {
                    const div = document.createElement("div");
                    div.className = "preview-thumb-box";
                    div.innerHTML = `<img src="${e.target.result}" alt="Preview ${index + 1}" />`;
                    additionalImagesPreview.appendChild(div);
                };
                reader.readAsDataURL(file);
            });
        });
    }
});

// 4. Global Toast Notification helper
function showToast(message, type = "success") {
    let toastContainer = document.getElementById("toastContainer");
    if (!toastContainer) {
        toastContainer = document.createElement("div");
        toastContainer.id = "toastContainer";
        toastContainer.className = "toast-container position-fixed bottom-0 start-0 p-3";
        toastContainer.style.zIndex = "11000";
        document.body.appendChild(toastContainer);
    }

    const toastEl = document.createElement("div");
    toastEl.className = `toast align-items-center text-bg-${type} border-0 shadow-lg`;
    toastEl.setAttribute("role", "alert");
    toastEl.setAttribute("aria-live", "assertive");
    toastEl.setAttribute("aria-atomic", "true");

    toastEl.innerHTML = `
        <div class="d-flex">
            <div class="toast-body fw-semibold">
                <i class="bi bi-info-circle-fill me-2"></i> ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;

    toastContainer.appendChild(toastEl);
    const toast = new bootstrap.Toast(toastEl, { delay: 3500 });
    toast.show();
    toastEl.addEventListener("hidden.bs.toast", () => toastEl.remove());
}

// 5. AJAX Add to Cart
window.addToCartAjax = async function (productId, quantity = 1) {
    try {
        const formData = new FormData();
        formData.append("productId", productId);
        formData.append("quantity", quantity);

        const response = await fetch("/Cart/AddToCart", {
            method: "POST",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            body: formData
        });

        const data = await response.json();
        if (data.success) {
            showToast(data.message, "success");
            const badge = document.getElementById("cartBadge");
            if (badge) {
                badge.textContent = data.cartCount;
                badge.classList.remove("d-none");
            }
        } else {
            showToast(data.message, "danger");
        }
    } catch (err) {
        console.error("Cart error:", err);
        showToast("Could not add to cart. Please try again.", "danger");
    }
};
