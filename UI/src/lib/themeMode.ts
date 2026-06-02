import { browser } from '$app/environment';

const darkModeStorageKey = 'foliotrace.darkMode';

export function readInitialDarkMode() {
  if (!browser) {
    return false;
  }

  const storedDarkMode = localStorage.getItem(darkModeStorageKey);
  return storedDarkMode === null
    ? window.matchMedia('(prefers-color-scheme: dark)').matches
    : storedDarkMode === 'true';
}

export function applyDarkModePreference(darkMode: boolean) {
  if (!browser) {
    return;
  }

  document.documentElement.classList.toggle('dark', darkMode);
  document.documentElement.dataset.theme = darkMode ? 'dark' : 'light';
  localStorage.setItem(darkModeStorageKey, String(darkMode));
}
