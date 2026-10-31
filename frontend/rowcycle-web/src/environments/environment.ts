// Settings for production builds (`npm run build`). `environment.development.ts` replaces this file for `npm start`.
export const environment = {
  production: true,
  /**
   * Where the API lives. Relative, because the web app and the API are served from the same site
   * (DECISIONS 2026-10-10). Change it here if the API moves to its own address.
   */
  apiBaseUrl: '/api/v1',
};
