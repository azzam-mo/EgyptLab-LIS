export const messages = {
  ar: {
    appTitle: 'EgyptLab LIS',
    language: 'اللغة',
    arabic: 'العربية',
    english: 'English',
    navigation: 'التنقل',
    nav: {
      dashboard: 'الرئيسية',
      reception: 'الاستقبال وتسجيل الحالات',
      catalogue: 'دليل التحاليل',
      samples: 'العينات والباركود',
      results: 'النتائج والاعتماد',
      reports: 'التقارير والطباعة',
      billing: 'الفواتير والمدفوعات',
      admin: 'الإدارة والصلاحيات',
    },
    dashboard: {
      title: 'لوحة البداية',
      description:
        'نسخة تجريبية لمعمل واحد تعمل محلياً (أوفلاين) على خادم المعمل. ابدأ بتسجيل المرضى والحالات، ثم إدخال النتائج واعتمادها، ثم طباعة التقارير.',
      statusTitle: 'حالة التنفيذ',
      statusBody:
        'تم تجهيز هيكل الواجهة والترجمة والتنقل (المرحلة 0.6). وحدات تسجيل الحالات وطباعة النتائج ستُستكمل في المراحل 1–3.',
    },
    reception: {
      title: 'الاستقبال وتسجيل الحالات',
      description:
        'تسجيل بيانات المريض، إنشاء الطلب/الحالة، واختيار التحاليل المطلوبة. (مخطط في المرحلة 1 — غير مُنفذ بعد)',
    },
    catalogue: {
      title: 'دليل التحاليل والأسعار',
      description:
        'إدارة التحاليل واللوحات والأسعار بالقروش. القيم السريرية (الوحدات والمديات المرجعية) لا تُضاف إلا بمعرفة مختصين معتمدين. (مخطط في المرحلة 1)',
    },
    samples: {
      title: 'العينات والباركود',
      description:
        'إنشاء الباركود وتتبع حالات العينة من السحب حتى الاستلام والتحليل. (مخطط في المرحلة 2)',
    },
    results: {
      title: 'إدخال النتائج والمراجعة والاعتماد',
      description:
        'إدخال النتائج يدوياً، تنبيه القيم الحرجة، واعتماد النتيجة على مرحلتين. النتيجة المعتمدة لا تُعدّل ويُنشأ إصدار جديد عند التصحيح. (مخطط في المرحلة 2)',
    },
    reports: {
      title: 'التقارير والطباعة',
      description:
        'طباعة تقرير النتائج PDF بهوية المعمل للنتائج المعتمدة فقط. (مخطط في المرحلة 3)',
    },
    billing: {
      title: 'الفواتير والمدفوعات',
      description:
        'إصدار الفواتير وتسجيل الدفعات الجزئية/الكاملة بالقروش، مع سجل غير قابل للحذف. (مخطط في المرحلة 1)',
    },
    admin: {
      title: 'الإدارة والصلاحيات',
      description:
        'إدارة المستخدمين والأدوار والصلاحيات داخل نطاق الفرع/المعمل. (أساس المصادقة موجود، وإدارة الحسابات تُستكمل لاحقاً)',
    },
    common: {
      singleLab: 'معمل واحد — محلي',
      offlineReady: 'يعمل على الخادم المحلي',
      placeholder: 'هذه الشاشة هيكل جاهز وسيتم ربطها بالوظائف في المراحل القادمة.',
    },
  },
  en: {
    appTitle: 'EgyptLab LIS',
    language: 'Language',
    arabic: 'العربية',
    english: 'English',
    navigation: 'Navigation',
    nav: {
      dashboard: 'Dashboard',
      reception: 'Reception & Case Registration',
      catalogue: 'Test Catalogue',
      samples: 'Samples & Barcodes',
      results: 'Results & Approval',
      reports: 'Reports & Printing',
      billing: 'Billing & Payments',
      admin: 'Administration & Permissions',
    },
    dashboard: {
      title: 'Getting started',
      description:
        'Single-lab trial version running locally (offline) on the lab server. Register patients and cases, enter and approve results, then print reports.',
      statusTitle: 'Implementation status',
      statusBody:
        'UI shell, translation, and navigation structure are in place (Phase 0.6). Case registration and result printing will be completed in Phases 1–3.',
    },
    reception: {
      title: 'Reception & Case Registration',
      description:
        'Register patient details, create an order/case, and select requested tests. (Planned for Phase 1 — not implemented yet)',
    },
    catalogue: {
      title: 'Test Catalogue & Pricing',
      description:
        'Manage tests, panels, and prices in piasters. Clinical values (units and reference ranges) must only be entered by qualified professionals. (Planned for Phase 1)',
    },
    samples: {
      title: 'Samples & Barcodes',
      description:
        'Generate barcodes and track sample states from collection to receipt and analysis. (Planned for Phase 2)',
    },
    results: {
      title: 'Result Entry, Review & Approval',
      description:
        'Manual result entry, critical-value alerts, and two-stage approval. Approved results are immutable; corrections create a new version. (Planned for Phase 2)',
    },
    reports: {
      title: 'Reports & Printing',
      description:
        'Print lab-branded PDF result reports for approved results only. (Planned for Phase 3)',
    },
    billing: {
      title: 'Billing & Payments',
      description:
        'Issue invoices and record partial/full payments in piasters, with a non-deletable record. (Planned for Phase 1)',
    },
    admin: {
      title: 'Administration & Permissions',
      description:
        'Manage users, roles, and permissions within the branch/lab scope. (Authentication foundation exists; account management will be completed later.)',
    },
    common: {
      singleLab: 'Single lab — local',
      offlineReady: 'Runs on the local server',
      placeholder:
        'This screen is a ready structure and will be connected to functionality in upcoming phases.',
    },
  },
} as const;

export type Locale = keyof typeof messages;

export type ViewId =
  | 'dashboard'
  | 'reception'
  | 'catalogue'
  | 'samples'
  | 'results'
  | 'reports'
  | 'billing'
  | 'admin';

export const defaultLocale: Locale = 'ar';

export const defaultView: ViewId = 'dashboard';

export function isRtl(locale: Locale): boolean {
  return locale === 'ar';
}

export function dirFor(locale: Locale): 'rtl' | 'ltr' {
  return isRtl(locale) ? 'rtl' : 'ltr';
}
