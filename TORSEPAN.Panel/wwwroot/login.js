window.torsepanLogin = {
    readCredentials: (usernameInput, passwordInput) => ({
        username: usernameInput.value,
        password: passwordInput.value
    })
};

window.torsepanDesign = {
    createType: async () => {
        const nameInput = document.getElementById('new-design-name');
        const rateInput = document.getElementById('new-design-rate');
        const exportRateInput = document.getElementById('new-design-export-rate');
        const message = document.getElementById('design-create-message');
        const name = (nameInput?.value || '').trim();
        const rate = Number(rateInput?.value || 0);
        const exportRate = Number(exportRateInput?.value || 0);
        if (!name) {
            message.style.display = 'block'; message.className = 'app-message error';
            message.textContent = 'نام دیزاین جدید را وارد کنید.'; return;
        }
        message.style.display = 'block'; message.className = 'app-message';
        message.textContent = 'در حال ثبت دیزاین...';
        try {
            const response = await fetch('/api/internal/design-types', {
                method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${localStorage.getItem('access_token') || ''}` },
                body: JSON.stringify({ name, rate: Number.isFinite(rate) ? rate : 0, exportRate: Number.isFinite(exportRate) ? exportRate : 0 })
            });
            if (!response.ok) {
                const detail = await response.text();
                throw new Error(detail || `خطای ${response.status}`);
            }
            message.className = 'app-message success'; message.textContent = `دیزاین «${name}» با موفقیت ایجاد شد.`;
            setTimeout(() => window.location.assign('/designs'), 500);
        } catch (error) {
            message.className = 'app-message error';
            message.textContent = `ثبت دیزاین انجام نشد: ${error.message || 'خطای نامشخص'}`;
        }
    }
};
