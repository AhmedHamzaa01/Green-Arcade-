import { HttpContextToken } from '@angular/common/http';

/** Don't attach the access token or try a refresh (used by the auth endpoints themselves). */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

/** Don't show the global error snackbar; the caller displays the error itself (e.g. under form fields). */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);
