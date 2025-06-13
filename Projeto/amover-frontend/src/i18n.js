import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

// importa os ficheiros de tradução
import translationPT from './locales/pt/translation.json';
import translationEN from './locales/en/translation.json';

const resources = {
  pt: { translation: translationPT },
  en: { translation: translationEN },
};

i18n
  .use(initReactI18next)
  .init({
    resources,
    lng: 'pt', // idioma inicial (podes alterar isto para vir do localStorage depois)
    fallbackLng: 'pt', // se faltar uma tradução, usa o português
    interpolation: {
      escapeValue: false, // para React
    },
  });

export default i18n;
