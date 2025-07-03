function previewImage(event) {
    var input = event.target;
    var preview = document.getElementById('imagePreview');
    var noImageText = document.getElementById('noImageText');

    if (input.files && input.files[0]) {
        var reader = new FileReader();

        reader.onload = function (e) {
            preview.src = e.target.result;
            preview.classList.add('show');
            if (noImageText) {
                noImageText.style.display = 'none';
            }
        };

        reader.readAsDataURL(input.files[0]);
    } else {
        preview.classList.remove('show');
        if (noImageText) {
            noImageText.style.display = 'block';
        }
    }
}