// Escape regular expression
function escapeRegExp(string) {
    return string.replace(/[.*+\-?^${}()|[\]\\]/g, '\\$&');
}

// History back
$('[data-back]').click(e => {
    document.referrer ? history.back() : location = '/';
});

// Initiate GET request (AJAX-supported)
$(document).on('click', '[data-get]', e => {
    e.preventDefault();
    const url = $(e.target).data('get');
    location = url || location;
});

// RESET form
$('[type=reset]').click(e => {
    e.preventDefault();
    location = location;
});

// Auto uppercase
$('[data-upper]').on('input', e => {
    const a = e.target.selectionStart;
    const b = e.target.selectionEnd;
    e.target.value = e.target.value.toUpperCase();
    e.target.setSelectionRange(a, b);
});

// Initiate POST request (AJAX-supported)
$(document).on('click', '[data-post]', e => {
    e.preventDefault();
    const url = $(e.target).data('post');
    const f = $('<form>').appendTo(document.body)[0];
    f.method = 'post';
    f.action = url || location;
    f.submit();
});

// Check all checkboxes
$('[data-check]').click(e => {
    e.preventDefault();
    const name = $(e.target).data('check');
    $(`[name=${name}]`).prop('checked', true);
});

// Uncheck all checkboxes
$('[data-uncheck]').click(e => {
    e.preventDefault();
    const name = $(e.target).data('uncheck');
    $(`[name=${name}]`).prop('checked', false);
});

// Row checkable
$('[data-checkable]').click(e => {
    if ($(e.target).is('input,button,a')) return;
    $(e.currentTarget)
        .find(':checkbox')
        .prop('checked', (i, v) => !v);
});

// Photo validation
function validatePhoto(f) {
    const reType = /^image\/(jpeg|png)$/i;
    const reName = /^.+\.(jpg|jpeg|png)$/i;

    return f &&
        f.size <= 1 * 1024 * 1024 &&
        reType.test(f.type) &&
        reName.test(f.name);
}

// Photo preview
$('.upload input').change(e => {
    const f = e.target.files[0];
    const img = $(e.target).siblings('img')[0];

    img.dataset.src ??= img.src;
    
    if (validatePhoto(f)) {
        img.onload = e => URL.revokeObjectURL(img.src);
        img.src = URL.createObjectURL(f);
    }
    else {
        img.src = img.dataset.src;
        e.target.value = '';
    }

    $(e.target).valid();
});
