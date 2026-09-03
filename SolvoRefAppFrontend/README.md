# Solvo Referrals Frontend (Vite)

## Runtime baseline (security hardening)

- Use Node.js `20.x` (see `.nvmrc`).
- Before pushing, run:
  - `npm ci --legacy-peer-deps`
  - `npm run build`
  - `npm run audit:json`

> CI currently fails only when `npm audit` reports `critical > 0` and always publishes the audit artifact for review.

## Available Scripts

In the project directory, you can run:

### `npm start` / `npm run dev`

Runs the app in development mode with Vite.\
Open the URL printed in console (usually [http://localhost:5173](http://localhost:5173)).

The page will reload when you make changes.\
You may also see any lint errors in the console.

### `npm test`

Runs tests with Vitest.

### `npm run build`

Builds the app for production to the `dist` folder.\
It correctly bundles React in production mode and optimizes the build for the best performance.

The build is minified and the filenames include the hashes.\
Your app is ready to be deployed!

### `npm run preview`

Serves the production build locally for quick validation.

## Environment Variables

Current codebase still reads `process.env.REACT_APP_*`.
The Vite config maps both:

- `REACT_APP_*` (legacy compatibility)
- `VITE_*` (new standard)

Recommended: define both during transition, then move to `import.meta.env.VITE_*` progressively.

## Security Baseline

- Use Node.js `20.x` (see `.nvmrc`).
- Before pushing, run:
  - `npm ci --legacy-peer-deps`
  - `npm run build`
  - `npm run audit:json`

CI currently fails only when `npm audit` reports `critical > 0` and always publishes the audit artifact for review.
