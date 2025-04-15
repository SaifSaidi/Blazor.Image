document.addEventListener('DOMContentLoaded', () => {
    // Tab functionality
    const tabButtons = document.querySelectorAll('[role="tab"]');
    const tabPanels = document.querySelectorAll('[role="tabpanel"]');

    tabButtons.forEach(button => {
        button.addEventListener('click', () => {
            const tabId = button.getAttribute('aria-controls');

            // Update button states
            tabButtons.forEach(btn => {
                const isSelected = btn === button;
                btn.setAttribute('aria-selected', isSelected.toString());
                btn.classList.toggle('border-purple-500', isSelected);
                btn.classList.toggle('text-purple-600', isSelected);
                btn.classList.toggle('dark:text-purple-400', isSelected);
                btn.classList.toggle('border-transparent', !isSelected);
                btn.classList.toggle('text-gray-500', !isSelected);
                btn.classList.toggle('hover:text-gray-700', !isSelected);
                btn.classList.toggle('hover:border-gray-300', !isSelected);
            });

            // Show the selected panel
            tabPanels.forEach(panel => {
                panel.classList.toggle('hidden', panel.id !== tabId);
            });
        });
    });

    // Dropdown functionality
    const dropdownBtn = document.getElementById('dropdownActionButton');
    const dropdownMenu = document.getElementById('actionDropdown');
    if (dropdownBtn && dropdownMenu) {
        dropdownBtn.addEventListener('click', () => {
            dropdownMenu.classList.toggle('hidden');
        });

        document.addEventListener('click', (event) => {
            if (!dropdownBtn.contains(event.target) && !dropdownMenu.contains(event.target)) {
                dropdownMenu.classList.add('hidden');
            }
        });
    }

    // Table search functionality
    const searchInput = document.getElementById('imageSearch');
    const tableRows = document.querySelectorAll('#imageTable tbody tr');

    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const searchTerm = this.value.toLowerCase();

            tableRows.forEach(row => {
                const text = row.textContent.toLowerCase();
                const dataName = row.getAttribute('data-name')?.toLowerCase() || '';
                row.style.display = (text.includes(searchTerm) || dataName.includes(searchTerm)) ? '' : 'none';
            });
        });
    }

    // Pagination functionality
    const itemsPerPage = 10;
    let currentPage = 1;
    const totalItems = tableRows.length;
    const totalPages = Math.ceil(totalItems / itemsPerPage);

    const prevPageBtn = document.getElementById('prevPage');
    const nextPageBtn = document.getElementById('nextPage');
    const mobilePrevPageBtn = document.getElementById('mobilePrevPage');
    const mobileNextPageBtn = document.getElementById('mobileNextPage');
    const paginationNumbers = document.getElementById('paginationNumbers');
    const pageStartEl = document.getElementById('pageStart');
    const pageEndEl = document.getElementById('pageEnd');
    const totalItemsEl = document.getElementById('totalItems');

    function updatePagination() {
        if (pageStartEl && pageEndEl && totalItemsEl) {
            const start = (currentPage - 1) * itemsPerPage + 1;
            const end = Math.min(currentPage * itemsPerPage, totalItems);
            pageStartEl.textContent = start.toString();
            pageEndEl.textContent = end.toString();
            totalItemsEl.textContent = totalItems.toString();
        }

        if (prevPageBtn) prevPageBtn.disabled = currentPage === 1;
        if (nextPageBtn) nextPageBtn.disabled = currentPage === totalPages;
        if (mobilePrevPageBtn) mobilePrevPageBtn.disabled = currentPage === 1;
        if (mobileNextPageBtn) mobileNextPageBtn.disabled = currentPage === totalPages;

        if (paginationNumbers) {
            paginationNumbers.innerHTML = '';

            for (let i = 1; i <= totalPages; i++) {
                if (i === 1 || i === totalPages || (i >= currentPage - 1 && i <= currentPage + 1)) {
                    const pageButton = document.createElement('button');
                    pageButton.className = i === currentPage
                        ? 'relative inline-flex items-center px-4 py-2 border border-purple-500 bg-purple-50 dark:bg-purple-900/20 text-sm font-medium text-purple-600 dark:text-purple-400'
                        : 'relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700';
                    pageButton.textContent = i.toString();
                    pageButton.addEventListener('click', () => goToPage(i));
                    paginationNumbers.appendChild(pageButton);
                } else if ((i === 2 && currentPage > 3) || (i === totalPages - 1 && currentPage < totalPages - 2)) {
                    const ellipsis = document.createElement('span');
                    ellipsis.className = 'relative inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm font-medium text-gray-700 dark:text-gray-300';
                    ellipsis.textContent = '...';
                    paginationNumbers.appendChild(ellipsis);
                }
            }
        }

        const start = (currentPage - 1) * itemsPerPage;
        const end = currentPage * itemsPerPage;
        tableRows.forEach((row, index) => {
            row.style.display = index >= start && index < end ? '' : 'none';
        });
    }

    function goToPage(page) {
        currentPage = page;
        updatePagination();
    }

    updatePagination();

    [prevPageBtn, mobilePrevPageBtn].forEach(btn => {
        if (btn) {
            btn.addEventListener('click', () => {
                if (currentPage > 1) goToPage(currentPage - 1);
            });
        }
    });

    [nextPageBtn, mobileNextPageBtn].forEach(btn => {
        if (btn) {
            btn.addEventListener('click', () => {
                if (currentPage < totalPages) goToPage(currentPage + 1);
            });
        }
    });
});
