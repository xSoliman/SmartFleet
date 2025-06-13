function previewImage(event) {
    var input = event.target;
    var preview = document.getElementById('imagePreview');
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            preview.src = e.target.result;
            preview.style.display = "block"; // Show the image
        };
        reader.readAsDataURL(input.files[0]);
    }
}