import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '');
    const defineEnvCompat = {
        'process.env.NODE_ENV': JSON.stringify(mode),
    };

    for (const [key, value] of Object.entries(env)) {
        if (key.startsWith('REACT_APP_') || key.startsWith('VITE_')) {
            defineEnvCompat[`process.env.${key}`] = JSON.stringify(value);
        }
    }

    return {
        plugins: [
            react({
                include: /\.[jt]sx?$/,
            }),
        ],
        define: defineEnvCompat,
        esbuild: {
            loader: 'jsx',
            include: /src\/.*\.[jt]sx?$/,
            exclude: [],
        },
        optimizeDeps: {
            esbuildOptions: {
                loader: {
                    '.js': 'jsx',
                },
            },
        },
        build: {
            outDir: 'dist',
        },
        test: {
            environment: 'jsdom',
            setupFiles: './src/setupTests.js',
        },
    };
});
