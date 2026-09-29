/**
 * GGCLIKS Admin Dashboard Chart.js Integration
 */
document.addEventListener("DOMContentLoaded", async function () {
    const salesCanvas = document.getElementById("salesChart");
    const statusCanvas = document.getElementById("statusChart");
    const topProductsCanvas = document.getElementById("topProductsChart");
    const categoryCanvas = document.getElementById("categoryChart");

    if (!salesCanvas || !statusCanvas) return;

    try {
        const response = await fetch("/Admin/Dashboard/GetChartData");
        if (!response.ok) return;

        const data = await response.json();

        // 1. Sales Over Time (Line Chart)
        new Chart(salesCanvas, {
            type: 'line',
            data: {
                labels: data.salesOverTime.labels,
                datasets: [{
                    label: 'Revenue (৳)',
                    data: data.salesOverTime.values,
                    borderColor: '#4f46e5',
                    backgroundColor: 'rgba(79, 70, 229, 0.1)',
                    tension: 0.35,
                    fill: true,
                    pointBackgroundColor: '#4f46e5',
                    pointRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) { return '৳' + value; }
                        }
                    }
                }
            }
        });

        // 2. Orders by Status (Doughnut Chart)
        new Chart(statusCanvas, {
            type: 'doughnut',
            data: {
                labels: data.ordersByStatus.labels,
                datasets: [{
                    data: data.ordersByStatus.values,
                    backgroundColor: [
                        '#f59e0b', // Pending (amber)
                        '#6366f1', // Processing (indigo)
                        '#0ea5e9', // Shipped (sky)
                        '#10b981', // Delivered (emerald)
                        '#ef4444'  // Cancelled (rose)
                    ],
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom' }
                }
            }
        });

        // 3. Top-selling Products (Bar Chart)
        if (topProductsCanvas) {
            new Chart(topProductsCanvas, {
                type: 'bar',
                data: {
                    labels: data.topProducts.labels,
                    datasets: [{
                        label: 'Units Sold',
                        data: data.topProducts.values,
                        backgroundColor: '#06b6d4',
                        borderRadius: 6
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: { precision: 0 }
                        }
                    }
                }
            });
        }

        // 4. Products by Category (Pie Chart)
        if (categoryCanvas) {
            new Chart(categoryCanvas, {
                type: 'pie',
                data: {
                    labels: data.productsByCategory.labels,
                    datasets: [{
                        data: data.productsByCategory.values,
                        backgroundColor: [
                            '#8b5cf6',
                            '#ec4899',
                            '#f97316',
                            '#14b8a6',
                            '#3b82f6'
                        ],
                        borderWidth: 2
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { position: 'bottom' }
                    }
                }
            });
        }
    } catch (err) {
        console.error("Failed to load admin dashboard charts:", err);
    }
});
