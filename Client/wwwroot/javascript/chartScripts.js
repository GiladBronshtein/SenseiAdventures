window.chartInstances = {};

window.renderBarChart = (canvasId, labels, data, backgroundColors, chartLabel, yAxisMax) => {
    if (window.chartInstances[canvasId]) {
        window.chartInstances[canvasId].destroy();
    }

    var ctx = document.getElementById(canvasId).getContext('2d');
    window.chartInstances[canvasId] = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: chartLabel,
                data: data,
                backgroundColor: backgroundColors,
                borderColor: backgroundColors.map(color => color.replace('0.2', '1')),
                borderWidth: 1
            }]
        },
        options: {
            plugins: {
                datalabels: {
                    color: 'black',
                    font: {
                        weight: 'normal',
                        size: 14
                    },
                    formatter: function (value, context) {
                        return value.toFixed(2); // Adjust the format as needed
                    }
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    max: yAxisMax
                }
            }
        },
        plugins: [ChartDataLabels]
    });
};

// Register ChartDataLabels globally
Chart.register(ChartDataLabels);
