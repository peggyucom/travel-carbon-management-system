window.confirmModal = (function () {
    const $modal = document.getElementById('globalConfirmModal');
    const body = document.getElementById('globalConfirmModalBody');
    const btnOk = document.getElementById('globalConfirmOk');
    const btnCancel = document.getElementById('globalConfirmCancel');

    function showConfirm(message) {
        return new Promise((resolve) => {
            if (body) body.textContent = message || '確定嗎？';
            if ($modal) {
                $("#globalConfirmModal").modal('show');
            }

            function clean() {
                btnOk.removeEventListener('click', onOk);
                btnCancel.removeEventListener('click', onCancel);
            }

            function onOk() { clean(); if ($modal) $("#globalConfirmModal").modal('hide'); resolve(true); }
            function onCancel() { clean(); if ($modal) $("#globalConfirmModal").modal('hide'); resolve(false); }

            btnOk.addEventListener('click', onOk);
            btnCancel.addEventListener('click', onCancel);
        });
    }

    function showAlert(message) {
        return new Promise((resolve) => {
            if (body) body.textContent = message || '';
            if ($modal) {
                // hide cancel button
                btnCancel.style.display = 'none';
                $("#globalConfirmModal").modal('show');
            }

            function clean() {
                btnOk.removeEventListener('click', onOk);
            }

            function onOk() { clean(); if ($modal) { $("#globalConfirmModal").modal('hide'); btnCancel.style.display = ''; } resolve(); }

            btnOk.addEventListener('click', onOk);
        });
    }

    function bindConfirmableForms() {
        document.querySelectorAll('form.confirmable-form').forEach(f => {
            if (f.__confirmBound) return;
            f.__confirmBound = true;
            f.addEventListener('submit', function (e) {
                e.preventDefault();
                let msg = f.dataset.confirm || f.dataset.confirmTemplate || '確定嗎？';
                if (f.dataset.confirmTemplate) {
                    msg = msg.replace('{rate}', (document.getElementById('rate')?.value || '')).replace('{value}', (document.getElementById('rate')?.value || ''));
                }
                showConfirm(msg).then(ok => { if (ok) f.submit(); });
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindConfirmableForms();
    });

    return { showConfirm, showAlert, bindConfirmableForms };
})();
