export const messages = {
  ar: {
    appTitle: 'EgyptLab LIS',
  },
  en: {
    appTitle: 'EgyptLab LIS',
  },
} as const;

export type Locale = keyof typeof messages;

export const defaultLocale: Locale = 'ar';
