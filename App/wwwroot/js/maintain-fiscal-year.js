(() => {
    const form = document.querySelector("[data-fiscal-year-form]");
    if (!form) return;

    const yearInput = form.querySelector("#Year");
    const nameInput = form.querySelector("#Name");
    const period = form.querySelector("#FiscalYearPeriod");
    let defaultName = `${yearInput.value}年度`;

    yearInput.addEventListener("input", () => {
        const year = yearInput.valueAsNumber;
        if (!Number.isInteger(year) || year < 1 || year > 9998) {
            period.textContent = "有効な年度を入力すると期間が表示されます。";
            return;
        }

        const nextName = `${year}年度`;
        if (nameInput.value === defaultName || nameInput.value === "") {
            nameInput.value = nextName;
        }
        defaultName = nextName;
        const startsOn = `${String(year).padStart(4, "0")}/04/01`;
        const endsOn = `${String(year + 1).padStart(4, "0")}/03/31`;
        period.textContent = `${startsOn} ～ ${endsOn}`;
    });
})();
