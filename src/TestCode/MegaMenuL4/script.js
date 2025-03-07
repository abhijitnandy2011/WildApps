document.addEventListener('DOMContentLoaded', () => {
    const menuButton = document.querySelector('.menu-button');
    const menuContainer = document.querySelector('.menu-button-container');
    
    // Toggle menu on button click
    menuButton.addEventListener('click', (e) => {
        e.stopPropagation();
        menuContainer.classList.toggle('active');
    });

    // Handle mobile menu interactions
    function handleMobileMenu() {
        const isMobile = window.innerWidth <= 576;
        
        if (isMobile) {
            // Remove hover-based display for mobile
            document.querySelectorAll('.mega-menu-column').forEach(column => {
                column.style.display = 'none';
            });
            
            // Show first level by default
            document.querySelector('.level-1').style.display = 'block';
            
            // Handle level 1 clicks
            document.querySelectorAll('.level-1-item').forEach(item => {
                item.addEventListener('click', (e) => {
                    e.preventDefault();
                    const category = item.getAttribute('data-category');
                    
                    // Hide all level 2, 3, and 4 columns
                    document.querySelectorAll('.level-2, .level-3, .level-4').forEach(col => {
                        col.style.display = 'none';
                    });
                    
                    // Show selected level 2 column
                    const targetLevel2 = document.querySelector(`.level-2[data-parent="${category}"]`);
                    if (targetLevel2) {
                        targetLevel2.style.display = 'block';
                    }
                });
            });
            
            // Handle level 2 clicks
            document.querySelectorAll('.level-2-item').forEach(item => {
                item.addEventListener('click', (e) => {
                    e.preventDefault();
                    const subcategory = item.getAttribute('data-subcategory');
                    
                    // Hide all level 3 and 4 columns
                    document.querySelectorAll('.level-3, .level-4').forEach(col => {
                        col.style.display = 'none';
                    });
                    
                    // Show selected level 3 column
                    const targetLevel3 = document.querySelector(`.level-3[data-parent="${subcategory}"]`);
                    if (targetLevel3) {
                        targetLevel3.style.display = 'block';
                    }
                });
            });

            // Handle level 3 clicks
            document.querySelectorAll('.level-3-item').forEach(item => {
                item.addEventListener('click', (e) => {
                    e.preventDefault();
                    const subcategory = item.getAttribute('data-subcategory');
                    
                    // Hide all level 4 columns
                    document.querySelectorAll('.level-4').forEach(col => {
                        col.style.display = 'none';
                    });
                    
                    // Show selected level 4 column
                    const targetLevel4 = document.querySelector(`.level-4[data-parent="${subcategory}"]`);
                    if (targetLevel4) {
                        targetLevel4.style.display = 'block';
                    }
                });
            });
        } else {
            // Reset styles for desktop
            document.querySelectorAll('.mega-menu-column').forEach(column => {
                if (column.classList.contains('level-1')) {
                    column.style.display = 'block';
                } else {
                    column.style.display = 'none';
                }
            });

            // Add hover functionality for desktop
            document.querySelectorAll('.level-1-item').forEach(item => {
                item.addEventListener('mouseenter', () => {
                    const category = item.getAttribute('data-category');
                    document.querySelectorAll('.level-2, .level-3, .level-4').forEach(col => col.style.display = 'none');
                    const targetLevel2 = document.querySelector(`.level-2[data-parent="${category}"]`);
                    if (targetLevel2) targetLevel2.style.display = 'block';
                });
            });

            document.querySelectorAll('.level-2-item').forEach(item => {
                item.addEventListener('mouseenter', () => {
                    const subcategory = item.getAttribute('data-subcategory');
                    document.querySelectorAll('.level-3, .level-4').forEach(col => col.style.display = 'none');
                    const targetLevel3 = document.querySelector(`.level-3[data-parent="${subcategory}"]`);
                    if (targetLevel3) targetLevel3.style.display = 'block';
                });
            });

            document.querySelectorAll('.level-3-item').forEach(item => {
                item.addEventListener('mouseenter', () => {
                    const subcategory = item.getAttribute('data-subcategory');
                    document.querySelectorAll('.level-4').forEach(col => col.style.display = 'none');
                    const targetLevel4 = document.querySelector(`.level-4[data-parent="${subcategory}"]`);
                    if (targetLevel4) targetLevel4.style.display = 'block';
                });
            });
        }
    }

    // Initialize mobile menu
    handleMobileMenu();
    
    // Update on window resize
    let resizeTimer;
    window.addEventListener('resize', () => {
        clearTimeout(resizeTimer);
        resizeTimer = setTimeout(handleMobileMenu, 250);
    });

    // Close mega menu when clicking outside
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.menu-button-container')) {
            menuContainer.classList.remove('active');
            if (window.innerWidth <= 576) {
                document.querySelectorAll('.mega-menu-column').forEach(column => {
                    if (!column.classList.contains('level-1')) {
                        column.style.display = 'none';
                    }
                });
                document.querySelector('.level-1').style.display = 'block';
            }
        }
    });

    // Prevent menu from closing when clicking inside
    document.querySelector('.mega-menu').addEventListener('click', (e) => {
        e.stopPropagation();
    });
}); 