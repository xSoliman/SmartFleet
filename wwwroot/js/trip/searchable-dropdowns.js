/**
 * Searchable Dropdowns for Trip Forms
 * Provides search functionality for vehicle and driver dropdowns
 */

class SearchableDropdown {
    constructor(selectElement, options = {}) {
        this.select = selectElement;
        this.options = {
            placeholder: 'Type to search...',
            minLength: 1,
            ...options
        };
        
        this.init();
    }
    
    init() {
        // Create wrapper
        this.wrapper = document.createElement('div');
        this.wrapper.className = 'searchable-dropdown-wrapper';
        
        // Create search input
        this.searchInput = document.createElement('input');
        this.searchInput.type = 'text';
        this.searchInput.className = 'searchable-dropdown-input';
        this.searchInput.placeholder = this.options.placeholder;
        
        // Create dropdown list
        this.dropdownList = document.createElement('div');
        this.dropdownList.className = 'searchable-dropdown-list';
        
        // Hide original select
        this.select.style.display = 'none';
        
        // Insert wrapper before select
        this.select.parentNode.insertBefore(this.wrapper, this.select);
        this.wrapper.appendChild(this.searchInput);
        this.wrapper.appendChild(this.dropdownList);
        this.wrapper.appendChild(this.select);
        
        // Store original options
        this.originalOptions = Array.from(this.select.options).map(option => ({
            value: option.value,
            text: option.text,
            selected: option.selected
        }));
        
        // Set initial value
        if (this.select.value) {
            const selectedOption = this.originalOptions.find(opt => opt.value === this.select.value);
            if (selectedOption) {
                this.searchInput.value = selectedOption.text;
            }
        }
        
        this.bindEvents();
        this.renderOptions(this.originalOptions);
    }
    
    bindEvents() {
        // Search input events
        this.searchInput.addEventListener('focus', () => {
            this.showDropdown();
        });
        
        this.searchInput.addEventListener('input', (e) => {
            this.filterOptions(e.target.value);
        });
        
        this.searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.hideDropdown();
                this.searchInput.blur();
            } else if (e.key === 'Enter') {
                e.preventDefault();
                const firstOption = this.dropdownList.querySelector('.searchable-dropdown-option');
                if (firstOption) {
                    this.selectOption(firstOption);
                }
            } else if (e.key === 'ArrowDown') {
                e.preventDefault();
                this.navigateOptions(1);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                this.navigateOptions(-1);
            }
        });
        
        // Click outside to close
        document.addEventListener('click', (e) => {
            if (!this.wrapper.contains(e.target)) {
                this.hideDropdown();
            }
        });
    }
    
    navigateOptions(direction) {
        const options = this.dropdownList.querySelectorAll('.searchable-dropdown-option');
        const currentIndex = Array.from(options).findIndex(option => 
            option.classList.contains('searchable-dropdown-option-selected')
        );
        
        let newIndex;
        if (currentIndex === -1) {
            newIndex = direction > 0 ? 0 : options.length - 1;
        } else {
            newIndex = currentIndex + direction;
            if (newIndex < 0) newIndex = options.length - 1;
            if (newIndex >= options.length) newIndex = 0;
        }
        
        // Remove previous selection
        options.forEach(option => option.classList.remove('searchable-dropdown-option-selected'));
        
        // Add selection to new option
        if (options[newIndex]) {
            options[newIndex].classList.add('searchable-dropdown-option-selected');
            options[newIndex].scrollIntoView({ block: 'nearest' });
        }
    }
    
    filterOptions(searchTerm) {
        if (searchTerm.length < this.options.minLength) {
            this.renderOptions(this.originalOptions);
            return;
        }
        
        const filteredOptions = this.originalOptions.filter(option => 
            option.text.toLowerCase().includes(searchTerm.toLowerCase())
        );
        
        this.renderOptions(filteredOptions);
    }
    
    renderOptions(options) {
        this.dropdownList.innerHTML = '';
        
        if (options.length === 0) {
            const noResults = document.createElement('div');
            noResults.className = 'searchable-dropdown-no-results';
            noResults.textContent = 'No results found';
            this.dropdownList.appendChild(noResults);
        } else {
            options.forEach(option => {
                const optionElement = document.createElement('div');
                optionElement.className = 'searchable-dropdown-option';
                optionElement.textContent = option.text;
                
                if (option.selected) {
                    optionElement.style.backgroundColor = '#e3f2fd';
                    optionElement.style.fontWeight = 'bold';
                }
                
                optionElement.addEventListener('click', () => {
                    this.selectOption(optionElement, option);
                });
                
                optionElement.addEventListener('mouseenter', () => {
                    // Remove selection from other options
                    this.dropdownList.querySelectorAll('.searchable-dropdown-option').forEach(opt => 
                        opt.classList.remove('searchable-dropdown-option-selected')
                    );
                    optionElement.classList.add('searchable-dropdown-option-selected');
                });
                
                this.dropdownList.appendChild(optionElement);
            });
        }
    }
    
    selectOption(optionElement, optionData = null) {
        if (!optionData) {
            const optionText = optionElement.textContent;
            optionData = this.originalOptions.find(opt => opt.text === optionText);
        }
        
        if (optionData) {
            this.searchInput.value = optionData.text;
            this.select.value = optionData.value;
            
            // Trigger change event on original select
            const event = new Event('change', { bubbles: true });
            this.select.dispatchEvent(event);
        }
        
        this.hideDropdown();
    }
    
    showDropdown() {
        this.dropdownList.style.display = 'block';
        this.searchInput.style.borderBottomLeftRadius = '0';
        this.searchInput.style.borderBottomRightRadius = '0';
        
        // Add animation class after a small delay
        setTimeout(() => {
            this.dropdownList.classList.add('show');
        }, 10);
    }
    
    hideDropdown() {
        this.dropdownList.classList.remove('show');
        
        // Wait for animation to complete before hiding
        setTimeout(() => {
            this.dropdownList.style.display = 'none';
            this.searchInput.style.borderBottomLeftRadius = '4px';
            this.searchInput.style.borderBottomRightRadius = '4px';
        }, 200);
    }
    
    // Public method to update options
    updateOptions(newOptions) {
        this.originalOptions = newOptions;
        this.renderOptions(this.originalOptions);
    }
}

// Initialize searchable dropdowns when DOM is ready
document.addEventListener('DOMContentLoaded', function() {
    // Initialize vehicle dropdown
    const vehicleSelect = document.querySelector('select[name="VehicleId"]');
    if (vehicleSelect) {
        new SearchableDropdown(vehicleSelect, {
            placeholder: 'Search vehicles...'
        });
    }
    
    // Initialize driver dropdown
    const driverSelect = document.querySelector('select[name="DriverId"]');
    if (driverSelect) {
        new SearchableDropdown(driverSelect, {
            placeholder: 'Search drivers...'
        });
    }
}); 