import { writable } from 'svelte/store';

export type AuthModalTab = 'login' | 'register';

const _store = writable<{ open: boolean; tab: AuthModalTab; hint: string | null }>({ open: false, tab: 'login', hint: null });

export const authModal = {
    subscribe: _store.subscribe,
    openLogin:    (hint?: string) => _store.set({ open: true, tab: 'login',    hint: hint ?? null }),
    openRegister: (hint?: string) => _store.set({ open: true, tab: 'register', hint: hint ?? null }),
    close:        () => _store.update(s => ({ ...s, open: false, hint: null })),
    switchTab:    (tab: AuthModalTab) => _store.update(s => ({ ...s, tab })),
};
