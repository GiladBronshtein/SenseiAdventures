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
            scales: {
                y: {
                    beginAtZero: true,
                    max: yAxisMax
                }
            }
        }
    });
};
