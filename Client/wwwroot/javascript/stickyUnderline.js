document.addEventListener('DOMContentLoaded', function () {
    setTimeout(function () {
        const setActiveLink = () => {
            const path = window.location.pathname.toLowerCase();
            console.log('Current Path:', path); // Debugging log

            document.querySelectorAll('.nav-link').forEach(link => {
                link.classList.remove('active');
                link.classList.remove('underline-active');
                const href = link.getAttribute('href').toLowerCase();
                if (path.includes(href)) {
                    link.classList.add('active');
                    link.classList.add('underline-active');
                }
            });
        };

        setActiveLink(); // Initial call on page load

        // Handle navigation changes (if your site uses history API for navigation)
        window.addEventListener('popstate', setActiveLink);
    }, 2000);
});
