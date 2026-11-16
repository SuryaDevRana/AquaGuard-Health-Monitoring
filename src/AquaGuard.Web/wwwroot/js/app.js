document.addEventListener("DOMContentLoaded", () => {
  window.lucide?.createIcons();
  const shell = document.getElementById("appShell");
  document.querySelector("[data-sidebar-open]")?.addEventListener("click", () => shell?.classList.add("sidebar-open"));
  document.querySelector("[data-sidebar-close]")?.addEventListener("click", () => shell?.classList.remove("sidebar-open"));
  const current = window.location.pathname.toLowerCase();
  document.querySelectorAll(".side-link").forEach(link => {
    const target = link.getAttribute("href")?.toLowerCase().replace(/\/$/, "");
    if (target && (current === target || current.startsWith(`${target}/`))) {
      link.classList.add("active");
      link.setAttribute("aria-current", "page");
    }
  });
  document.querySelectorAll("[data-dismiss-after]").forEach(item => setTimeout(() => item.remove(), Number(item.dataset.dismissAfter) || 4500));
  document.querySelectorAll("[data-toast-close]").forEach(button => button.addEventListener("click", () => button.closest(".app-toast")?.remove()));
  document.querySelectorAll("form[data-auto-filter] select").forEach(select => {
    select.addEventListener("change", () => select.form?.requestSubmit());
  });
  document.querySelectorAll("form").forEach(form => {
    form.addEventListener("submit", () => {
      const button = form.querySelector("[data-loading-text]");
      if (!button) return;
      button.disabled = true;
      const label = button.querySelector("span");
      if (label) label.textContent = button.dataset.loadingText || "Working…";
    });
  });
});
