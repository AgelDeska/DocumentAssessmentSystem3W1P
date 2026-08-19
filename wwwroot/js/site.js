document.addEventListener('DOMContentLoaded', () => {
	const form = document.querySelector('#upload-form');
	const fileInput = document.querySelector('#file-input');
	const dropZone = document.querySelector('#drop-zone');
	const filePreview = document.querySelector('#file-preview');
	const fileName = document.querySelector('#file-name');
	const fileSize = document.querySelector('#file-size');
	const removeFile = document.querySelector('#remove-file');
	const errorMessage = document.querySelector('#upload-error');
	const analyzeButton = document.querySelector('#analyze-button');
	const buttonLabel = document.querySelector('.button-label');
	const buttonLoading = document.querySelector('.button-loading');
	const loadingPanel = document.querySelector('#loading-panel');
	const maxFileSize = 20 * 1024 * 1024;

	if (!form || !fileInput || !dropZone) {
		return;
	}

	const formatFileSize = (bytes) => {
		if (bytes < 1024 * 1024) {
			return `${(bytes / 1024).toFixed(1)} KB`;
		}

		return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
	};

	const showError = (message) => {
		errorMessage.textContent = message;
		filePreview.hidden = true;
	};

	const clearFile = () => {
		fileInput.value = '';
		filePreview.hidden = true;
		errorMessage.textContent = '';
	};

	const displayFile = (file) => {
		const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();
		const isPdf = extension === '.pdf' && (!file.type || file.type === 'application/pdf');

		if (!isPdf) {
			clearFile();
			showError('File harus berformat PDF.');
			return false;
		}

		if (file.size === 0) {
			clearFile();
			showError('Silakan pilih file PDF terlebih dahulu.');
			return false;
		}

		if (file.size > maxFileSize) {
			clearFile();
			showError('Ukuran file melebihi batas yang diperbolehkan.');
			return false;
		}

		fileName.textContent = file.name;
		fileSize.textContent = formatFileSize(file.size);
		filePreview.hidden = false;
		errorMessage.textContent = '';
		return true;
	};

	fileInput.addEventListener('change', () => {
		if (fileInput.files.length > 0) {
			displayFile(fileInput.files[0]);
		}
	});

	['dragenter', 'dragover'].forEach((eventName) => {
		dropZone.addEventListener(eventName, (event) => {
			event.preventDefault();
			dropZone.classList.add('is-dragging');
		});
	});

	['dragleave', 'drop'].forEach((eventName) => {
		dropZone.addEventListener(eventName, (event) => {
			event.preventDefault();
			dropZone.classList.remove('is-dragging');
		});
	});

	dropZone.addEventListener('drop', (event) => {
		const files = event.dataTransfer.files;
		if (files.length === 0) {
			return;
		}

		fileInput.files = files;
		displayFile(files[0]);
	});

	dropZone.addEventListener('keydown', (event) => {
		if (event.key === 'Enter' || event.key === ' ') {
			event.preventDefault();
			fileInput.click();
		}
	});

	removeFile.addEventListener('click', clearFile);

	form.addEventListener('submit', (event) => {
		if (fileInput.files.length === 0 || !displayFile(fileInput.files[0])) {
			event.preventDefault();
			return;
		}

		analyzeButton.disabled = true;
		buttonLabel.hidden = true;
		buttonLoading.hidden = false;
		loadingPanel.hidden = false;
	});
	const accordions = document.querySelectorAll('.result-accordion');
	accordions.forEach((accordion) => {
		accordion.addEventListener('toggle', () => {
			accordion.classList.toggle('is-open', accordion.open);
		});
	});
});
