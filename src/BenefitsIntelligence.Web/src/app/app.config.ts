import { registerLocaleData } from '@angular/common';
import { provideHttpClient, withFetch } from '@angular/common/http';
import localeEnGb from '@angular/common/locales/en-GB';
import {
  ApplicationConfig,
  DEFAULT_CURRENCY_CODE,
  LOCALE_ID,
  provideBrowserGlobalErrorListeners,
} from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';

import { routes } from './app.routes';

registerLocaleData(localeEnGb);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withFetch()),
    { provide: LOCALE_ID, useValue: 'en-GB' },
    { provide: DEFAULT_CURRENCY_CODE, useValue: 'GBP' },
  ],
};
