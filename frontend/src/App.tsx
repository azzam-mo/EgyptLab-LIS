import { useEffect, useMemo, useState } from 'react';
import './App.css';
import {
  defaultLocale,
  defaultView,
  dirFor,
  messages,
  type Locale,
  type ViewId,
} from './i18n/messages';

const VIEW_IDS: ViewId[] = [
  'dashboard',
  'reception',
  'catalogue',
  'samples',
  'results',
  'reports',
  'billing',
  'admin',
];

const STORAGE_KEY = 'egyptlab.locale';

function readStoredLocale(): Locale {
  if (typeof window === 'undefined') return defaultLocale;
  const stored = window.localStorage.getItem(STORAGE_KEY);
  return stored === 'en' || stored === 'ar' ? stored : defaultLocale;
}

function App() {
  const [locale, setLocale] = useState<Locale>(readStoredLocale);
  const [view, setView] = useState<ViewId>(defaultView);

  const t = useMemo(() => messages[locale], [locale]);

  useEffect(() => {
    const dir = dirFor(locale);
    document.documentElement.lang = locale;
    document.documentElement.dir = dir;
    try {
      window.localStorage.setItem(STORAGE_KEY, locale);
    } catch {
      // Ignore storage failures in restricted environments.
    }
  }, [locale]);

  const isDashboard = view === 'dashboard';

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">
        {t.nav.dashboard}
      </a>

      <header className="topbar">
        <div className="brand">
          <div className="brand-mark" aria-hidden="true">
            EL
          </div>
          <div className="brand-text">
            <div className="brand-title">{t.appTitle}</div>
            <div className="brand-subtitle">
              {t.common.singleLab} · {t.common.offlineReady}
            </div>
          </div>
        </div>

        <div className="lang-switch" role="group" aria-label={t.language}>
          <button
            type="button"
            className={locale === 'ar' ? 'lang-btn active' : 'lang-btn'}
            onClick={() => setLocale('ar')}
            aria-pressed={locale === 'ar'}
          >
            {t.arabic}
          </button>
          <button
            type="button"
            className={locale === 'en' ? 'lang-btn active' : 'lang-btn'}
            onClick={() => setLocale('en')}
            aria-pressed={locale === 'en'}
          >
            {t.english}
          </button>
        </div>
      </header>

      <div className="app-body">
        <nav className="sidebar" aria-label={t.navigation}>
          <ul className="nav-list">
            {VIEW_IDS.map((id) => (
              <li key={id}>
                <button
                  type="button"
                  className={view === id ? 'nav-item active' : 'nav-item'}
                  onClick={() => setView(id)}
                  aria-current={view === id ? 'page' : undefined}
                >
                  {t.nav[id]}
                </button>
              </li>
            ))}
          </ul>
        </nav>

        <main id="main-content" className="content" tabIndex={-1}>
          {isDashboard ? (
            <section className="page">
              <h1 className="page-title">{t.dashboard.title}</h1>
              <p className="page-lead">{t.dashboard.description}</p>

              <div className="cards">
                <article className="card status-card">
                  <h2>{t.dashboard.statusTitle}</h2>
                  <p>{t.dashboard.statusBody}</p>
                </article>

                <article className="card">
                  <h2>{t.nav.reception}</h2>
                  <p>{t.reception.description}</p>
                </article>

                <article className="card">
                  <h2>{t.nav.results}</h2>
                  <p>{t.results.description}</p>
                </article>

                <article className="card">
                  <h2>{t.nav.reports}</h2>
                  <p>{t.reports.description}</p>
                </article>
              </div>
            </section>
          ) : (
            <section className="page">
              <h1 className="page-title">{t[view].title}</h1>
              <p className="page-lead">{t[view].description}</p>
              <div className="placeholder-panel">
                <p>{t.common.placeholder}</p>
              </div>
            </section>
          )}
        </main>
      </div>
    </div>
  );
}

export default App;
